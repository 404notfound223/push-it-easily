using Menu.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
//using System.Linq.Dynamic.Core;

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
    public async Task<IActionResult> OrderHistory(PagingRequest request)
    {
        var userId = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToAction("Login", "Login");
        }

        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest" || Request.Query.ContainsKey("ajax"))
        {
            var pagedResult = await GetPagedOrderHistory(userId, request);
            return Json(new { success = true, result = pagedResult });
        }

        try
        {
            var orders = await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .Include(o => o.User)
                .Where(o => o.UserId != null && o.UserId == userId)
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
    public async Task<IActionResult> GetPagedOrderHistory(PagingRequest request)
    {
        var userId = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userId))
        {
            return Json(new { success = false, error = "User not logged in" });
        }

        var pagedResult = await GetPagedOrderHistory(userId, request);
        return Json(new { success = true, result = pagedResult });
    }

    private async Task<PagedResult<object>> GetPagedOrderHistory(string userId, PagingRequest request)
    {
        var query = _context.Orders
            .Include(o => o.OrderDetails)
            .ThenInclude(od => od.Product)
            .Include(o => o.User)
            .Where(o => o.UserId != null && o.UserId == userId)
            .AsQueryable();

        if (!string.IsNullOrEmpty(request.SearchTerm))
        {
            var searchTerm = request.SearchTerm.ToLower();
            query = query.Where(o => o.OrderId.ToLower().Contains(searchTerm) ||
                                   o.Status.ToLower().Contains(searchTerm) ||
                                   o.OrderDetails.Any(od => od.Product.Name.ToLower().Contains(searchTerm)));
        }

        if (!string.IsNullOrEmpty(request.Category)) // Using Category field for status filter
        {
            query = query.Where(o => o.Status == request.Category);
        }

        if (!string.IsNullOrEmpty(request.SortBy))
        {
            switch (request.SortBy.ToLower())
            {
                case "date":
                    query = request.SortDirection == "desc"
                        ? query.OrderByDescending(o => o.OrderDate)
                        : query.OrderBy(o => o.OrderDate);
                    break;
                case "total":
                    query = request.SortDirection == "desc"
                        ? query.OrderByDescending(o => o.TotalAmount)
                        : query.OrderBy(o => o.TotalAmount);
                    break;
                case "status":
                    query = request.SortDirection == "desc"
                        ? query.OrderByDescending(o => o.Status)
                        : query.OrderBy(o => o.Status);
                    break;
                default:
                    query = query.OrderByDescending(o => o.OrderDate);
                    break;
            }
        }
        else
        {
            query = query.OrderByDescending(o => o.OrderDate);
        }

        var totalCount = await query.CountAsync();
        var orders = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(o => new {
                orderId = o.OrderId,
                orderDate = o.OrderDate,
                totalAmount = o.TotalAmount,
                status = o.Status,
                customer = o.User != null ? o.User.Name : "Guest",
                itemCount = o.OrderDetails.Count(),
                items = o.OrderDetails.Take(3).Select(od => new {
                    productName = od.Product.Name,
                    quantity = od.Quantity,
                    unitPrice = od.UnitPrice
                }).ToList(),
                hasMoreItems = o.OrderDetails.Count() > 3
            })
            .ToListAsync();

        return new PagedResult<object>
        {
            Items = orders.Cast<object>().ToList(),
            TotalCount = totalCount,
            PageNumber = request.Page,
            PageSize = request.PageSize
        };
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
                .Include(o => o.User) 
                .Where(o => o.UserId != null && o.UserId == userId) 
                .OrderByDescending(o => o.OrderDate)
                .Select(o => new {
                    orderId = o.OrderId,
                    orderDate = o.OrderDate,
                    totalAmount = o.TotalAmount,
                    status = o.Status,
                    customer = o.User != null ? o.User.Name : "Guest",
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
    public async Task<IActionResult> MemberOrderView(string Id)
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
                .FirstOrDefaultAsync(o => o.OrderId == Id);

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
                memberDiscount = subtotal * 0.10m; // 10% member discount on subtotal only
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
