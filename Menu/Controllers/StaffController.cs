using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Menu.Models;

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
            existing.Stock = product.Stock;

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
}

