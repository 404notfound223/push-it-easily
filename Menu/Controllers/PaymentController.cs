using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Menu.Models;
using Stripe;
using Stripe.Checkout;

public class PaymentController : Controller
{
    private readonly DB _context;
    private readonly IConfiguration _configuration;

    public PaymentController(DB context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
        StripeConfiguration.ApiKey = _configuration["Stripe:SecretKey"];
    }

    [HttpPost]
    public async Task<IActionResult> CreateCheckoutSession([FromBody] CreateCheckoutRequest request)
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

            // Create order first
            var order = new Order
            {
                OrderId = Guid.NewGuid().ToString(),
                UserId = userId,
                User = user,
                TotalAmount = request.TotalAmount,
                Status = "Pending Payment",
                OrderDate = DateTime.Now
            };

            _context.Orders.Add(order);

            // Add order details
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

                    // Create Stripe line item
                    lineItems.Add(new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            UnitAmount = (long)(product.Price * 100), // Stripe uses cents
                            Currency = "rm",
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

            // Create Stripe checkout session
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

            var service = new SessionService();
            Session session = service.Create(options);

            return Json(new { success = true, sessionId = session.Id, checkoutUrl = session.Url });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, error = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> Success(string session_id, string order_id)
    {
        try
        {
            var service = new SessionService();
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
                return RedirectToAction("Cancel", new { order_id = order_id });
            }
        }
        catch (Exception ex)
        {
            ViewBag.Error = ex.Message;
            return View("Error");
        }
    }

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

            // Generate payment number
            var paymentNumber = new Random().Next(100000, 999999);

            var order = new Order
            {
                OrderId = Guid.NewGuid().ToString(),
                UserId = userId,
                User = user,
                TotalAmount = request.TotalAmount,
                Status = $"Pay at Counter - #{paymentNumber}",
                OrderDate = DateTime.Now
            };

            _context.Orders.Add(order);

            // Add order details
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

            return Json(new { 
                success = true, 
                orderId = order.OrderId, 
                paymentNumber = paymentNumber 
            });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, error = ex.Message });
        }
    }
}

public class CreateCheckoutRequest
{
    public decimal TotalAmount { get; set; }
    public List<OrderItemRequest> Items { get; set; } = new List<OrderItemRequest>();
}
