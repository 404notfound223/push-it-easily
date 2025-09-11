using Menu.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stripe.Climate;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

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

    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
    {
        var userId = HttpContext.Session.GetString("UserId");
        var isGuest = string.IsNullOrEmpty(userId);

        try
        {
            User? user = null;
            if (!isGuest)
            {
                user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == userId);
            }

            var order = new Menu.Models.Order
            {
                OrderId = await IdGenerator.GenerateOrderId(_context),
                UserId = isGuest ? null : userId,
                User = user,
                TotalAmount = request.TotalAmount,
                Status = "Pending",
                OrderDate = DateTime.Now
            };

            _context.Orders.Add(order);

            // Get the last OrderDetailId number once
            var lastDetail = await _context.OrderDetails
                .OrderByDescending(od => od.OrderDetailId)
                .FirstOrDefaultAsync();

            int lastNumber = 0;
            if (lastDetail != null && lastDetail.OrderDetailId.Length > 3)
            {
                int.TryParse(lastDetail.OrderDetailId.Substring(3), out lastNumber);
            }

            // Add order details
            foreach (var item in request.Items)
            {
                var product = await _context.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == item.ProductId);
                if (product != null)
                {
                    var orderDetail = new OrderDetail
                    {
                        OrderDetailId = await IdGenerator.GenerateOrderDetailId(_context),
                        OrderId = order.OrderId,
                        Order = order,
                        ProductId = item.ProductId,
                        Product = product,
                        Quantity = item.Quantity,
                        UnitPrice = product.Price
                    };
                    _context.OrderDetails.Add(orderDetail);

                    var trackedProduct = await _context.Products.FirstOrDefaultAsync(p => p.Id == item.ProductId);
                    if (trackedProduct != null)
                    {
                        trackedProduct.Sold += item.Quantity;
                    }
                }
            }

            await _context.SaveChangesAsync();

            return Json(new { success = true, orderId = order.OrderId, isGuest = isGuest });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, error = ex.Message });
        }
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
}

