using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Menu.Models;
using Stripe;
using Stripe.Checkout;

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
            {
                return Json(new { success = false, error = "Invalid or missing request body." });
            }

            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, error = "User not logged in" });
            }

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    return Json(new { success = false, error = "User not found" });
                }

                // Create order
                var order = new Order
                {
                    OrderId = await IdGenerator.GenerateOrderId(_context),
                    UserId = userId,
                    User = user,
                    TotalAmount = request.TotalAmount,
                    Status = "Pending Payment",
                    OrderDate = DateTime.Now
                };
                _context.Orders.Add(order);

                // Build line items for Stripe
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

                        lineItems.Add(new SessionLineItemOptions
                        {
                            PriceData = new SessionLineItemPriceDataOptions
                            {
                                UnitAmount = (long)(product.Price * 100), // Stripe uses cents
                                Currency = "MYR",
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

                // Create Stripe session
                var options = new SessionCreateOptions
                {
                    PaymentMethodTypes = new List<string> { "card" },
                    LineItems = lineItems,
                    Mode = "payment",
                    SuccessUrl = $"{Request.Scheme}://{Request.Host}/Payment/Success?session_id={{CHECKOUT_SESSION_ID}}&order_id={order.OrderId}",
                    CancelUrl = $"{Request.Scheme}://{Request.Host}/Payment/Cancel?order_id={order.OrderId}",
                    CustomerEmail = user.Email,
                    Metadata = new Dictionary<string, string>
                    {
                        { "order_id", order.OrderId },
                        { "user_id", userId }
                    }
                };

                var service = new SessionService(_stripeClient);
                Session session = await service.CreateAsync(options);

                await transaction.CommitAsync();

                return Json(new { success = true, sessionId = session.Id, checkoutUrl = session.Url });
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

        // Counter (manual) payment
        [HttpPost]
        public async Task<IActionResult> CreateCounterPayment([FromBody] CreateOrderRequest request)
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, error = "User not logged in" });
            }

            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    return Json(new { success = false, error = "User not found" });
                }

                var paymentNumber = new Random().Next(100000, 999999);

                var order = new Order
                {
                    OrderId = await IdGenerator.GenerateOrderId(_context),
                    UserId = userId,
                    User = user,
                    TotalAmount = request.TotalAmount,
                    Status = $"Pay at Counter - #{paymentNumber}",
                    OrderDate = DateTime.Now
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
                    paymentNumber
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
    }
}
