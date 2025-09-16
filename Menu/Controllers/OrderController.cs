using Menu.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

public class OrderController : Controller
{
    private readonly DB _context;

    public OrderController(DB context)
    {
        _context = context;
    }

    [HttpGet]
    public IActionResult ViewOrder()
    {
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> OrderHistory()
    {
        var userId = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToAction("Login", "Login");
        }

        try
        {
            var orders = await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return View(orders);
        }
        catch (Exception ex)
        {
            ViewBag.Error = ex.Message;
            return View(new List<Menu.Models.Order>());
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetUserOrders()
    {
        var userId = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userId))
        {
            return Json(new { success = false, error = "User not logged in" });
        }

        try
        {
            var orders = await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.OrderDate)
                .Select(o => new {
                    orderId = o.OrderId,
                    orderDate = o.OrderDate,
                    totalAmount = o.TotalAmount,
                    status = o.Status,
                    items = o.OrderDetails.Select(od => new {
                        productName = od.Product.Name,
                        quantity = od.Quantity,
                        unitPrice = od.UnitPrice
                    }).ToList()
                })
                .ToListAsync();

            return Json(new { success = true, orders });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, error = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> UpdateOrderStatus(string orderId, string status)
    {
        var userRole = HttpContext.Session.GetString("UserRole");
        if (userRole != "admin" && userRole != "staff")
        {
            return Json(new { success = false, error = "Unauthorized" });
        }

        try
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null)
            {
                return Json(new { success = false, error = "Order not found" });
            }

            order.Status = status;
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, error = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> OrderDetails(string orderId)
    {
        var userId = HttpContext.Session.GetString("UserId");
        var userRole = HttpContext.Session.GetString("UserRole");

        try
        {
            var order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null)
            {
                return NotFound();
            }

            // Check if user can view this order
            if (userRole != "admin" && userRole != "staff" && !string.IsNullOrEmpty(userId) && order.UserId != userId)
            {
                return Forbid();
            }

            return View(order);
        }
        catch (Exception ex)
        {
            ViewBag.Error = ex.Message;
            return View();
        }
    }

    [HttpGet]
    public async Task<IActionResult> MemberOrderView(string orderId)
    {
        var userId = HttpContext.Session.GetString("UserId");
        var userRole = HttpContext.Session.GetString("UserRole");

        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToAction("Login", "Login");
        }

        try
        {
            var order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null)
            {
                ViewBag.Error = "Order not found.";
                return View();
            }

            // Ensure member can only view their own orders
            if (userRole == "member" && order.UserId != userId)
            {
                ViewBag.Error = "You can only view your own orders.";
                return View();
            }

            // Calculate order summary
            var subtotal = order.OrderDetails.Sum(od => od.UnitPrice * od.Quantity);
            var tax = subtotal * 0.085m; // 8.5% tax
            var memberDiscount = 0m;

            if (order.User?.Role == "member")
            {
                memberDiscount = (subtotal + tax) * 0.10m; // 10% member discount
            }

            ViewBag.Subtotal = subtotal;
            ViewBag.Tax = tax;
            ViewBag.MemberDiscount = memberDiscount;
            ViewBag.FinalTotal = order.TotalAmount;

            return View(order);
        }
        catch (Exception ex)
        {
            ViewBag.Error = ex.Message;
            return View();
        }
    }

    [HttpGet]
    public async Task<IActionResult> TrackOrder(string orderId)
    {
        try
        {
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null)
            {
                ViewBag.Error = "Order not found.";
                return View();
            }

            return View(order);
        }
        catch (Exception ex)
        {
            ViewBag.Error = ex.Message;
            return View();
        }
    }
}
