using Menu.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace Menu.Controllers
{
    public class OMController : Controller
    {
        private readonly DB _context;

        public OMController(DB context)
        {
            _context = context;
        }

        // Order Management View
        public async Task<IActionResult> OrderManage()
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "admin" && userRole != "staff")
            {
                return RedirectToAction("Login", "Login");
            }

            var model = new StaffDashboardViewModel
            {
                UserRole = userRole,
                Orders = await _context.Orders
                    .Include(o => o.User)
                    .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                    .OrderByDescending(o => o.OrderDate)
                    .ToListAsync()
            };

            return View(model);
        }

        // Order Management API Methods
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
        public async Task<IActionResult> GetOrderDetails(string orderId)
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "admin" && userRole != "staff")
            {
                return Json(new { success = false, error = "Unauthorized" });
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
                    return Json(new { success = false, error = "Order not found" });
                }

                var result = new
                {
                    orderId = order.OrderId,
                    orderDate = order.OrderDate,
                    status = order.Status,
                    totalAmount = order.TotalAmount,
                    user = order.User != null ? new
                    {
                        userId = order.User.UserId,
                        name = order.User.Name,
                        email = order.User.Email,
                        role = order.User.Role
                    } : null,
                    orderDetails = order.OrderDetails.Select(od => new
                    {
                        id = od.OrderDetailId,
                        orderDetailId = od.OrderDetailId,
                        productId = od.ProductId,
                        product = new
                        {
                            name = od.Product.Name
                        },
                        quantity = od.Quantity,
                        unitPrice = od.UnitPrice
                    }).ToList()
                };

                return Json(new { success = true, order = result });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateOrder([FromBody] UpdateOrderRequest request)
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "admin" && userRole != "staff")
            {
                return Json(new { success = false, error = "Unauthorized" });
            }

            try
            {
                var order = await _context.Orders
                    .Include(o => o.OrderDetails)
                    .FirstOrDefaultAsync(o => o.OrderId == request.OrderId);

                if (order == null)
                {
                    return Json(new { success = false, error = "Order not found" });
                }

                // Update order details
                foreach (var item in request.Items)
                {
                    var orderDetail = order.OrderDetails.FirstOrDefault(od => od.OrderDetailId.ToString() == item.ItemId);
                    if (orderDetail != null)
                    {
                        orderDetail.Quantity = item.Quantity;
                        // Unit price remains the same unless specifically changed
                        if (item.UnitPrice > 0)
                        {
                            orderDetail.UnitPrice = item.UnitPrice;
                        }
                    }
                }

                // Remove items that are no longer in the request (quantity = 0 or removed)
                var itemsToRemove = order.OrderDetails
                    .Where(od => !request.Items.Any(item => item.ItemId == od.OrderDetailId.ToString()) ||
                                request.Items.Any(item => item.ItemId == od.OrderDetailId.ToString() && item.Quantity <= 0))
                    .ToList();

                foreach (var item in itemsToRemove)
                {
                    _context.OrderDetails.Remove(item);
                }

                // Recalculate total amount
                order.TotalAmount = request.TotalAmount;

                await _context.SaveChangesAsync();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteOrder([FromForm] string orderId)
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "admin" && userRole != "staff")
            {
                return Json(new { success = false, error = "Unauthorized" });
            }

            try
            {
                var order = await _context.Orders
                    .Include(o => o.OrderDetails)
                    .FirstOrDefaultAsync(o => o.OrderId == orderId);

                if (order == null)
                {
                    return Json(new { success = false, error = "Order not found" });
                }

                // Remove related OrderDetails first
                _context.OrderDetails.RemoveRange(order.OrderDetails);

                // Remove the order
                _context.Orders.Remove(order);

                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetOrdersData(int page = 1, int pageSize = 20, string sortBy = "orderDate", string sortOrder = "desc", string status = "", string dateFrom = "", string dateTo = "")
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "admin" && userRole != "staff")
            {
                return Json(new { success = false, error = "Unauthorized" });
            }

            try
            {
                var query = _context.Orders
                    .Include(o => o.User)
                    .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                    .AsQueryable();

                // Filter by status if specified
                if (!string.IsNullOrEmpty(status) && status != "all")
                {
                    query = query.Where(o => o.Status.ToLower() == status.ToLower());
                }

                // Filter by date range if specified
                if (!string.IsNullOrEmpty(dateFrom) && DateTime.TryParse(dateFrom, out var fromDate))
                {
                    query = query.Where(o => o.OrderDate >= fromDate);
                }

                if (!string.IsNullOrEmpty(dateTo) && DateTime.TryParse(dateTo, out var toDate))
                {
                    query = query.Where(o => o.OrderDate <= toDate.AddDays(1)); // Include the entire day
                }

                // Apply sorting
                query = sortBy.ToLower() switch
                {
                    "orderdate" => sortOrder == "desc" ? query.OrderByDescending(o => o.OrderDate) : query.OrderBy(o => o.OrderDate),
                    "totalamount" => sortOrder == "desc" ? query.OrderByDescending(o => o.TotalAmount) : query.OrderBy(o => o.TotalAmount),
                    "status" => sortOrder == "desc" ? query.OrderByDescending(o => o.Status) : query.OrderBy(o => o.Status),
                    "customer" => sortOrder == "desc" ? query.OrderByDescending(o => o.User != null ? o.User.Name : "Guest") : query.OrderBy(o => o.User != null ? o.User.Name : "Guest"),
                    _ => query.OrderByDescending(o => o.OrderDate)
                };

                var totalCount = await query.CountAsync();
                var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

                var orders = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(o => new
                    {
                        o.OrderId,
                        o.OrderDate,
                        o.TotalAmount,
                        o.Status,
                        CustomerName = o.User != null ? o.User.Name : "Guest",
                        CustomerEmail = o.User != null ? o.User.Email : "N/A",
                        ItemCount = o.OrderDetails.Sum(od => od.Quantity),
                        OrderDetails = o.OrderDetails.Select(od => new
                        {
                            od.OrderDetailId,
                            od.ProductId,
                            ProductName = od.Product.Name,
                            od.Quantity,
                            od.UnitPrice,
                            Total = od.Quantity * od.UnitPrice
                        }).ToList()
                    })
                    .ToListAsync();

                return Json(new
                {
                    success = true,
                    orders = orders,
                    currentPage = page,
                    totalPages = totalPages,
                    totalCount = totalCount,
                    pageSize = pageSize
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetOrderStatistics()
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "admin" && userRole != "staff")
            {
                return Json(new { success = false, error = "Unauthorized" });
            }

            try
            {
                var today = DateTime.Today;
                var thisWeek = today.AddDays(-(int)today.DayOfWeek);
                var thisMonth = new DateTime(today.Year, today.Month, 1);

                var stats = new
                {
                    TotalOrders = await _context.Orders.CountAsync(),
                    TodayOrders = await _context.Orders.CountAsync(o => o.OrderDate >= today),
                    WeekOrders = await _context.Orders.CountAsync(o => o.OrderDate >= thisWeek),
                    MonthOrders = await _context.Orders.CountAsync(o => o.OrderDate >= thisMonth),

                    TotalRevenue = await _context.Orders.SumAsync(o => o.TotalAmount),
                    TodayRevenue = await _context.Orders.Where(o => o.OrderDate >= today).SumAsync(o => o.TotalAmount),
                    WeekRevenue = await _context.Orders.Where(o => o.OrderDate >= thisWeek).SumAsync(o => o.TotalAmount),
                    MonthRevenue = await _context.Orders.Where(o => o.OrderDate >= thisMonth).SumAsync(o => o.TotalAmount),

                    PendingOrders = await _context.Orders.CountAsync(o => o.Status == "Pending"),
                    ProcessingOrders = await _context.Orders.CountAsync(o => o.Status == "Processing"),
                    CompletedOrders = await _context.Orders.CountAsync(o => o.Status == "Completed"),
                    CancelledOrders = await _context.Orders.CountAsync(o => o.Status == "Cancelled"),

                    TopProducts = await _context.Products
                        .OrderByDescending(p => p.Sold)
                        .Take(5)
                        .Select(p => new { p.Name, p.Sold, p.Price })
                        .ToListAsync()
                };

                return Json(new { success = true, stats });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> BulkUpdateOrderStatus([FromBody] BulkOrderStatusRequest request)
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "admin" && userRole != "staff")
            {
                return Json(new { success = false, error = "Unauthorized" });
            }

            try
            {
                var orders = await _context.Orders
                    .Where(o => request.OrderIds.Contains(o.OrderId))
                    .ToListAsync();

                if (orders.Count == 0)
                {
                    return Json(new { success = false, error = "No orders found" });
                }

                foreach (var order in orders)
                {
                    order.Status = request.NewStatus;
                }

                await _context.SaveChangesAsync();

                return Json(new { success = true, updatedCount = orders.Count });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> BulkDeleteOrders([FromBody] BulkDeleteOrdersRequest request)
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "admin")
            {
                return Json(new { success = false, error = "Only admin can bulk delete orders" });
            }

            try
            {
                var orders = await _context.Orders
                    .Where(o => request.OrderIds.Contains(o.OrderId))
                    .ToListAsync();

                if (orders.Count == 0)
                {
                    return Json(new { success = false, error = "No orders found" });
                }

                _context.Orders.RemoveRange(orders);
                await _context.SaveChangesAsync();

                return Json(new { success = true, deletedCount = orders.Count });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> ExportOrders(string format = "csv", string status = "", string dateFrom = "", string dateTo = "")
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "admin" && userRole != "staff")
            {
                return Json(new { success = false, error = "Unauthorized" });
            }

            try
            {
                var query = _context.Orders
                    .Include(o => o.User)
                    .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                    .AsQueryable();

                // Apply filters
                if (!string.IsNullOrEmpty(status) && status != "all")
                {
                    query = query.Where(o => o.Status.ToLower() == status.ToLower());
                }

                if (!string.IsNullOrEmpty(dateFrom) && DateTime.TryParse(dateFrom, out var fromDate))
                {
                    query = query.Where(o => o.OrderDate >= fromDate);
                }

                if (!string.IsNullOrEmpty(dateTo) && DateTime.TryParse(dateTo, out var toDate))
                {
                    query = query.Where(o => o.OrderDate <= toDate.AddDays(1));
                }

                var orders = await query.OrderByDescending(o => o.OrderDate).ToListAsync();

                if (format.ToLower() == "csv")
                {
                    var csv = new StringBuilder();
                    csv.AppendLine("Order ID,Order Date,Customer Name,Customer Email,Status,Total Amount,Items");

                    foreach (var order in orders)
                    {
                        var items = string.Join("; ", order.OrderDetails.Select(od => $"{od.Product.Name} x{od.Quantity}"));
                        csv.AppendLine($"{order.OrderId},{order.OrderDate:yyyy-MM-dd HH:mm},{order.User?.Name ?? "Guest"},{order.User?.Email ?? "N/A"},{order.Status},{order.TotalAmount:C},\"{items}\"");
                    }

                    var bytes = Encoding.UTF8.GetBytes(csv.ToString());
                    return File(bytes, "text/csv", $"orders_{DateTime.Now:yyyyMMdd}.csv");
                }

                return Json(new { success = false, error = "Unsupported format" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }
    }
}
