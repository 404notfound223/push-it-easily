using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Menu.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class StaffController : Controller
{
    private readonly DB _context;

    public StaffController(DB context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Dashboard()
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
                .ToListAsync(),

            Users = userRole == "admin" ? await _context.Users.ToListAsync() : new List<User>(),

            Products = await _context.Products.ToListAsync()
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> GetUserById(string userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return Json(new { success = false, error = "User not found" });
        return Json(new { success = true, user });
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

            return Json(new { success = true, order });
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
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null)
            {
                return Json(new { success = false, error = "Order not found" });
            }

            _context.Orders.Remove(order);
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, error = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> UpdateUser([FromBody] UpdateUserRequest request)
    {
        var userRole = HttpContext.Session.GetString("UserRole");
        if (userRole != "admin")
        {
            return Json(new { success = false, error = "Only admin can edit users" });
        }

        try
        {
            var user = await _context.Users.FindAsync(request.UserId);
            if (user == null)
            {
                return Json(new { success = false, error = "User not found" });
            }

            user.Name = request.Name;
            user.Email = request.Email;
            user.Role = request.Role;

            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, error = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> DeleteUser(string userId)
    {
        var userRole = HttpContext.Session.GetString("UserRole");
        if (userRole != "admin")
        {
            return Json(new { success = false, error = "Only admin can delete users" });
        }

        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return Json(new { success = false, error = "User not found" });
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, error = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetProductById(string id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null) return Json(new { success = false, error = "Product not found" });
        return Json(new { success = true, product });
    }

    [HttpPost]
    public async Task<IActionResult> AddProduct([FromBody] Product product)
    {
        var userRole = HttpContext.Session.GetString("UserRole");
        if (userRole != "admin" && userRole != "staff")
        {
            return Json(new { success = false, error = "Unauthorized" });
        }

        try
        {
            product.Id = Guid.NewGuid().ToString();
            _context.Products.Add(product);
            await _context.SaveChangesAsync();
            return Json(new { success = true, product });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, error = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> UpdateProduct([FromBody] Product product)
    {
        var userRole = HttpContext.Session.GetString("UserRole");
        if (userRole != "admin" && userRole != "staff")
        {
            return Json(new { success = false, error = "Unauthorized" });
        }

        try
        {
            var existing = await _context.Products.FindAsync(product.Id);
            if (existing == null)
            {
                return Json(new { success = false, error = "Product not found" });
            }

            existing.Name = product.Name;
            existing.Price = product.Price;
            existing.Description = product.Description;
            existing.Category = product.Category;
            existing.ImagePath = product.ImagePath;

            await _context.SaveChangesAsync();
            return Json(new { success = true, product = existing });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, error = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> DeleteProduct(string id)
    {
        var userRole = HttpContext.Session.GetString("UserRole");
        if (userRole != "admin" && userRole != "staff")
        {
            return Json(new { success = false, error = "Unauthorized" });
        }

        try
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return Json(new { success = false, error = "Product not found" });
            }

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, error = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> ToggleDisable(string id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null)
            return NotFound();

        product.IsDisabled = !product.IsDisabled;
        await _context.SaveChangesAsync();

        return RedirectToAction("Dashboard");
    }

    [HttpPost]
    public async Task<IActionResult> ToggleProductDisable([FromBody] ToggleDisableRequest request)
    {
        var userRole = HttpContext.Session.GetString("UserRole");
        if (userRole != "admin" && userRole != "staff")
        {
            return Json(new { success = false, error = "Unauthorized" });
        }

        try
        {
            var product = await _context.Products.FindAsync(request.ProductId);
            if (product == null)
            {
                return Json(new { success = false, error = "Product not found" });
            }

            product.IsDisabled = request.Disable;
            await _context.SaveChangesAsync();

            return Json(new { success = true, isDisabled = product.IsDisabled });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, error = ex.Message });
        }
    }

    [HttpPost]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction("Login", "Login");
    }

    [HttpGet]
    public async Task<IActionResult> GetProductsData(int page = 1, int pageSize = 20, string sortBy = "name", string sortOrder = "asc", string category = "")
    {
        var userRole = HttpContext.Session.GetString("UserRole");
        if (userRole != "admin" && userRole != "staff")
        {
            return Json(new { success = false, error = "Unauthorized" });
        }

        try
        {
            var query = _context.Products.AsQueryable();

            // Filter by category if specified
            if (!string.IsNullOrEmpty(category) && category != "all")
            {
                query = query.Where(p => p.Category.ToLower() == category.ToLower());
            }

            // Apply sorting
            query = sortBy.ToLower() switch
            {
                "name" => sortOrder == "desc" ? query.OrderByDescending(p => p.Name) : query.OrderBy(p => p.Name),
                "category" => sortOrder == "desc" ? query.OrderByDescending(p => p.Category) : query.OrderBy(p => p.Category),
                "price" => sortOrder == "desc" ? query.OrderByDescending(p => p.Price) : query.OrderBy(p => p.Price),
                _ => query.OrderBy(p => p.Name)
            };

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            var products = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Json(new
            {
                success = true,
                products = products,
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
}
