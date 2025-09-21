using Microsoft.AspNetCore.Mvc;
using Menu.Models;
using Menu.Services;
using Stripe;
using Stripe.Checkout;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Menu.Controllers
{
    public class PaymentController : Controller
    {
        private readonly DB _context;
        private readonly IConfiguration _configuration;
        private readonly StripeClient _stripeClient;
        private readonly PaymentSecurityService _securityService;
        private readonly ILogger<PaymentController> _logger;

        public PaymentController(DB context, IConfiguration configuration, StripeClient stripeClient,
            PaymentSecurityService securityService, ILogger<PaymentController> logger)
        {
            _context = context;
            _configuration = configuration;
            _stripeClient = stripeClient;
            _securityService = securityService;
            _logger = logger;
        }

        // Enhanced Stripe Checkout Session with security validation
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCheckoutSession([FromBody] SecureCreateOrderRequest request)
        {
            var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var userId = HttpContext.Session.GetString("UserId");

            try
            {
                // Validate model
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                    _securityService.LogPaymentAttempt(clientIp, userId, request.TotalAmount, false, "Model validation failed");
                    return Json(new { success = false, error = "Invalid request data", details = errors });
                }

                // Security validation
                var validationResult = await _securityService.ValidatePaymentRequest(request, userId, clientIp);
                if (!validationResult.IsValid)
                {
                    _securityService.LogPaymentAttempt(clientIp, userId, request.TotalAmount, false, string.Join(", ", validationResult.Errors));
                    return Json(new { success = false, error = "Payment validation failed", details = validationResult.Errors });
                }

                User? user = null;
                string? customerEmail = null;

                if (!string.IsNullOrEmpty(userId))
                {
                    user = await _context.Users.FindAsync(userId);
                    if (user == null)
                    {
                        _securityService.LogPaymentAttempt(clientIp, userId, request.TotalAmount, false, "User not found");
                        return Json(new { success = false, error = "User not found" });
                    }
                    customerEmail = user.Email;
                }
                else
                {
                    customerEmail = request.Email;
                }

                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    var order = new Order
                    {
                        OrderId = Guid.NewGuid().ToString(),
                        UserId = userId,
                        User = user,
                        TotalAmount = validationResult.CalculatedTotal, // Use validated total
                        Status = "Pending Payment",
                        OrderDate = DateTime.Now,
                    };

                    _context.Orders.Add(order);

                    var lineItems = new List<SessionLineItemOptions>();

                    foreach (var item in request.Items)
                    {
                        var product = validationResult.ValidatedProducts.FirstOrDefault(p => p.Id == item.ProductId);
                        if (product != null)
                        {
                            var orderDetail = new OrderDetail
                            {
                                OrderDetailId = Guid.NewGuid().ToString(),
                                OrderId = order.OrderId,
                                Order = order,
                                ProductId = item.ProductId,
                                Product = product,
                                Quantity = item.Quantity,
                                UnitPrice = product.Price // Use validated price
                            };
                            _context.OrderDetails.Add(orderDetail);

                            var unitAmount = (long)Math.Round(Math.Abs(product.Price) * 100);
                            if (unitAmount <= 0)
                            {
                                throw new Exception($"Invalid price for product {product.Name}: {product.Price}");
                            }

                            lineItems.Add(new SessionLineItemOptions
                            {
                                PriceData = new SessionLineItemPriceDataOptions
                                {
                                    UnitAmount = unitAmount,
                                    Currency = "myr",
                                    ProductData = new SessionLineItemPriceDataProductDataOptions
                                    {
                                        Name = product.Name,
                                        Description = product.Description,
                                    },
                                },
                                Quantity = item.Quantity,
                            });
                        }
                    }

                    await _context.SaveChangesAsync();

                    var options = new SessionCreateOptions
                    {
                        PaymentMethodTypes = new List<string> { "card" },
                        LineItems = lineItems,
                        Mode = "payment",
                        SuccessUrl = $"{Request.Scheme}://{Request.Host}/Payment/Success?session_id={{CHECKOUT_SESSION_ID}}&order_id={order.OrderId}",
                        CancelUrl = $"{Request.Scheme}://{Request.Host}/Payment/Cancel?order_id={order.OrderId}",
                        CustomerEmail = customerEmail,
                        Metadata = new Dictionary<string, string>
                        {
                            { "order_id", order.OrderId },
                            { "user_id", userId ?? "guest" },
                            { "member_discount", validationResult.MemberDiscount.ToString() },
                            { "client_ip", clientIp }
                        }
                    };

                    if (validationResult.MemberDiscount > 0)
                    {
                        var couponService = new CouponService(_stripeClient);
                        var coupon = await couponService.CreateAsync(new CouponCreateOptions
                        {
                            PercentOff = 10,
                            Duration = "once",
                            Name = "Member Discount",
                            Id = $"member-discount-{order.OrderId}"
                        });

                        options.Discounts = new List<SessionDiscountOptions>
                        {
                            new SessionDiscountOptions
                            {
                                Coupon = coupon.Id
                            }
                        };
                    }

                    var service = new SessionService(_stripeClient);
                    Session session = await service.CreateAsync(options);

                    await transaction.CommitAsync();

                    _securityService.LogPaymentAttempt(clientIp, userId, validationResult.CalculatedTotal, true);
                    return Json(new { success = true, sessionId = session.Id, checkoutUrl = session.Url, memberDiscount = validationResult.MemberDiscount });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _securityService.LogPaymentAttempt(clientIp, userId, request.TotalAmount, false, ex.Message);
                    _logger.LogError(ex, "Error creating Stripe checkout session for IP: {ClientIp}", clientIp);
                    return Json(new { success = false, error = "Payment processing error. Please try again." });
                }
            }
            catch (Exception ex)
            {
                _securityService.LogPaymentAttempt(clientIp, userId, request.TotalAmount, false, ex.Message);
                _logger.LogError(ex, "Unexpected error in CreateCheckoutSession for IP: {ClientIp}", clientIp);
                return Json(new { success = false, error = "An unexpected error occurred. Please try again." });
            }
        }

        // Enhanced Counter Payment with security validation
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCounterPayment([FromBody] SecureCreateOrderRequest request)
        {
            var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var userId = HttpContext.Session.GetString("UserId");

            try
            {
                // Validate model
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                    _securityService.LogPaymentAttempt(clientIp, userId, request.TotalAmount, false, "Model validation failed");
                    return Json(new { success = false, error = "Invalid request data", details = errors });
                }

                // Security validation
                var validationResult = await _securityService.ValidatePaymentRequest(request, userId, clientIp);
                if (!validationResult.IsValid)
                {
                    _securityService.LogPaymentAttempt(clientIp, userId, request.TotalAmount, false, string.Join(", ", validationResult.Errors));
                    return Json(new { success = false, error = "Payment validation failed", details = validationResult.Errors });
                }

                User? user = null;
                if (!string.IsNullOrEmpty(userId))
                {
                    user = await _context.Users.FindAsync(userId);
                    if (user == null)
                    {
                        _securityService.LogPaymentAttempt(clientIp, userId, request.TotalAmount, false, "User not found");
                        return Json(new { success = false, error = "User not found" });
                    }
                }

                try
                {
                    var paymentNumber = new Random().Next(100000, 999999);

                    var order = new Order
                    {
                        OrderId = Guid.NewGuid().ToString(),
                        UserId = userId,
                        User = user,
                        TotalAmount = validationResult.CalculatedTotal, // Use validated total
                        Status = "Pending Payment",
                        OrderDate = DateTime.Now,
                    };

                    _context.Orders.Add(order);

                    foreach (var item in request.Items)
                    {
                        var product = validationResult.ValidatedProducts.FirstOrDefault(p => p.Id == item.ProductId);
                        if (product != null)
                        {
                            var orderDetail = new OrderDetail
                            {
                                OrderDetailId = Guid.NewGuid().ToString(),
                                OrderId = order.OrderId,
                                Order = order,
                                ProductId = item.ProductId,
                                Product = product,
                                Quantity = item.Quantity,
                                UnitPrice = product.Price // Use validated price
                            };
                            _context.OrderDetails.Add(orderDetail);
                        }
                    }

                    await _context.SaveChangesAsync();

                    _securityService.LogPaymentAttempt(clientIp, userId, validationResult.CalculatedTotal, true);
                    return Json(new
                    {
                        success = true,
                        orderId = order.OrderId,
                        paymentNumber,
                        memberDiscount = validationResult.MemberDiscount
                    });
                }
                catch (Exception ex)
                {
                    _securityService.LogPaymentAttempt(clientIp, userId, request.TotalAmount, false, ex.Message);
                    _logger.LogError(ex, "Error creating counter payment for IP: {ClientIp}", clientIp);
                    return Json(new { success = false, error = "Payment processing error. Please try again." });
                }
            }
            catch (Exception ex)
            {
                _securityService.LogPaymentAttempt(clientIp, userId, request.TotalAmount, false, ex.Message);
                _logger.LogError(ex, "Unexpected error in CreateCounterPayment for IP: {ClientIp}", clientIp);
                return Json(new { success = false, error = "An unexpected error occurred. Please try again." });
            }
        }

        // Generate secure request token
        [HttpGet]
        public IActionResult GetRequestToken()
        {
            var token = _securityService.GenerateRequestToken();
            return Json(new { success = true, token });
        }

        // Stripe Success
        [HttpGet]
        public async Task<IActionResult> Success(string session_id, string order_id)
        {
            try
            {
                var service = new SessionService(_stripeClient);
                Session session = service.Get(session_id);

                if (session.PaymentStatus == "paid")
                {
                    var order = await _context.Orders.FindAsync(order_id);
                    if (order != null)
                    {
                        order.Status = "Paid";
                        await _context.SaveChangesAsync();
                    }

                    ViewBag.OrderId = order_id;
                    ViewBag.SessionId = session_id;
                    ViewBag.PaymentIntentId = session.PaymentIntentId;
                    return View();
                }
                else
                {
                    return RedirectToAction("Cancel", new { order_id });
                }
            }
            catch (Exception ex)
            {
                ViewBag.Error = ex.Message;
                return View("Error");
            }
        }

        // Stripe Cancel
        [HttpGet]
        public async Task<IActionResult> Cancel(string order_id)
        {
            try
            {
                var order = await _context.Orders.FindAsync(order_id);
                if (order != null)
                {
                    order.Status = "Cancelled";
                    await _context.SaveChangesAsync();
                }

                ViewBag.OrderId = order_id;
                return View();
            }
            catch (Exception ex)
            {
                ViewBag.Error = ex.Message;
                return View("Error");
            }
        }

        // Checkout view
        public IActionResult Checkout()
        {
            ViewBag.PublishableKey = _configuration["Stripe:PublishableKey"];
            return View();
        }

        // Enhanced Invoice view with print options
        [HttpGet]
        public async Task<IActionResult> Invoice(string orderId, bool autoprint = false)
        {
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .Include(o => o.User)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null)
                return NotFound();

            ViewBag.AutoPrint = autoprint;
            return View(order);
        }

        // Generate printable receipt
        [HttpGet]
        public async Task<IActionResult> Receipt(string orderId)
        {
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .Include(o => o.User)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null)
                return NotFound();

            return View("Receipt", order);
        }
    }
}
