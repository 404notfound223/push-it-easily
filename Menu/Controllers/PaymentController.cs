using Microsoft.AspNetCore.Mvc;
using Menu.Models;
using Stripe;
using Stripe.Checkout;
using Microsoft.EntityFrameworkCore;

namespace Menu.Controllers
{
    public class PaymentController : Controller
    {
        private readonly DB _context;
        private readonly IConfiguration _configuration;
        private readonly StripeClient _stripeClient;

        // Inject StripeClient from Program.cs
        public PaymentController(DB context, IConfiguration configuration, StripeClient stripeClient)
        {
            _context = context;
            _configuration = configuration;
            _stripeClient = stripeClient;
        }

        // Stripe Checkout Session
        [HttpPost]
        public async Task<IActionResult> CreateCheckoutSession([FromBody] CreateOrderRequest request)
        {
            if (request == null)
                return Json(new { success = false, error = "Invalid or missing request body." });

            var userId = HttpContext.Session.GetString("UserId");
            User? user = null;
            string? customerEmail = null;
            decimal memberDiscount = 0;
            bool isMember = false;

            if (!string.IsNullOrEmpty(userId))
            {
                user = await _context.Users.FindAsync(userId);
                if (user == null)
                    return Json(new { success = false, error = "User not found" });
                customerEmail = user.Email;

                if (user.Role.ToLower() == "member")
                {
                    isMember = true;
                    var subtotal = request.TotalAmount;
                    memberDiscount = subtotal * 0.10m; // 10% discount
                }
            }
            else
            {
                // Optionally, get guest email from request (add Email property to CreateOrderRequest if needed)
                customerEmail = request.Email; // You must add Email to your CreateOrderRequest DTO for this to work
            }

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var order = new Order
                {
                    OrderId = Guid.NewGuid().ToString(),
                    UserId = userId,
                    User = user,
                    TotalAmount = request.TotalAmount,
                    Status = "Pending Payment",
                    OrderDate = DateTime.Now,
                };

                _context.Orders.Add(order);

                var lineItems = new List<SessionLineItemOptions>();

                foreach (var item in request.Items)
                {
                    var product = await _context.Products.FindAsync(item.ProductId);
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
                            UnitPrice = product.Price
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
                        { "member_discount", memberDiscount.ToString() }
                    }
                };

                if (isMember && memberDiscount > 0)
                {
                    var couponService = new CouponService(_stripeClient);
                    var coupon = await couponService.CreateAsync(new CouponCreateOptions
                    {
                        PercentOff = 10,
                        Duration = "once",
                        Name = "Member Discount",
                        Id = $"member-discount-{order.OrderId}" // Unique ID for this order
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

                return Json(new { success = true, sessionId = session.Id, checkoutUrl = session.Url, memberDiscount = memberDiscount });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return Json(new { success = false, error = ex.Message });
            }
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
                    var order = await _context.Orders
                        .Include(o => o.OrderDetails)
                        .ThenInclude(od => od.Product)
                        .Include(o => o.User)
                        .FirstOrDefaultAsync(o => o.OrderId == order_id);

                    if (order != null)
                    {
                        order.Status = "Paid";
                        await _context.SaveChangesAsync();
                    }

                    decimal memberDiscount = 0;
                    if (session.Metadata != null && session.Metadata.ContainsKey("member_discount"))
                    {
                        decimal.TryParse(session.Metadata["member_discount"], out memberDiscount);
                    }

                    ViewBag.OrderId = order_id;
                    ViewBag.SessionId = session_id;
                    ViewBag.PaymentIntentId = session.PaymentIntentId;
                    ViewBag.Order = order;
                    ViewBag.MemberDiscount = memberDiscount;
                    ViewBag.AmountPaid = session.AmountTotal / 100.0m; // Convert from cents
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

        // Counter (manual) payment
        [HttpPost]
        public async Task<IActionResult> CreateCounterPayment([FromBody] CreateOrderRequest request)
        {
            var userId = HttpContext.Session.GetString("UserId");
            User? user = null;
            decimal memberDiscount = 0;

            if (!string.IsNullOrEmpty(userId))
            {
                user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    return Json(new { success = false, error = "User not found" });
                }

                if (user.Role.ToLower() == "member")
                {
                    var subtotal = request.TotalAmount;
                    memberDiscount = subtotal * 0.10m; // 10% discount
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
                    TotalAmount = request.TotalAmount,
                    Status = "Pending Payment",
                    OrderDate = DateTime.Now,
                };

                _context.Orders.Add(order);

                foreach (var item in request.Items)
                {
                    var product = await _context.Products.FindAsync(item.ProductId);
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
                            UnitPrice = product.Price
                        };
                        _context.OrderDetails.Add(orderDetail);
                    }
                }

                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    orderId = order.OrderId,
                    paymentNumber,
                    memberDiscount = memberDiscount
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }


        // Checkout view
        public IActionResult Checkout()
        {
            ViewBag.PublishableKey = _configuration["Stripe:PublishableKey"];
            return View();
        }

        // Invoice view
        [HttpGet]
        public async Task<IActionResult> Invoice(string orderId)
        {
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .Include(o => o.User)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null)
                return NotFound();

            return View(order);
        }
    }
}
