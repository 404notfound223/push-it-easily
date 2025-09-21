using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Menu.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Text;

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
        var order = await _context.Orders
            .Include(o => o.User)
            .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
            .FirstOrDefaultAsync(o => o.OrderId == orderId);

        if (order == null)
        {
            return Json(new { success = false, error = "Order not found" });
        }

        var orderDto = new
        {
            orderId = order.OrderId,
            orderDate = order.OrderDate,
            status = order.Status,
            totalAmount = order.TotalAmount,
            user = order.User == null ? null : new
            {
                name = order.User.Name,
                email = order.User.Email
            },
            orderItems = order.OrderDetails.Select(od => new
            {
                id = od.OrderDetailId,
                quantity = od.Quantity,
                unitPrice = od.UnitPrice,
                product = new
                {
                    name = od.Product.Name
                }
            })
        };

        return Json(new { success = true, order = orderDto });
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

            // Update existing order details
            foreach (var item in request.Items)
            {
                var orderDetail = order.OrderDetails
                    .FirstOrDefault(od => od.OrderDetailId.ToString() == item.ItemId);

                if (orderDetail != null)
                {
                    orderDetail.Quantity = item.Quantity;
                    if (item.UnitPrice > 0)
                    {
                        orderDetail.UnitPrice = item.UnitPrice;
                    }
                }
            }

            // Remove items missing or with 0 qty
            var itemsToRemove = order.OrderDetails
                .Where(od => !request.Items.Any(item => item.ItemId == od.OrderDetailId.ToString()) ||
                             request.Items.Any(item => item.ItemId == od.OrderDetailId.ToString() && item.Quantity <= 0))
                .ToList();

            foreach (var item in itemsToRemove)
            {
                _context.OrderDetails.Remove(item);
            }

            // Update total
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

            _context.OrderDetails.RemoveRange(order.OrderDetails);
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

            var hasOrders = await _context.Orders.AnyAsync(o => o.UserId == userId);
            if (hasOrders)
            {
                return Json(new { success = false, error = "Cannot delete user with existing orders. Consider disabling the user instead." });
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

    private string HashPassword(string password)
    {
        using (var sha256 = System.Security.Cryptography.SHA256.Create())
        {
            var bytes = Encoding.UTF8.GetBytes(password);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }
    }

    [HttpPost]
    public async Task<IActionResult> AddUser([FromBody] AddUserRequest request)
    {
        var userRole = HttpContext.Session.GetString("UserRole");
        if (userRole != "admin")
        {
            return Json(new { success = false, error = "Only admin can create users" });
        }

        try
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(request.UserId) ||
                string.IsNullOrWhiteSpace(request.Name) ||
                string.IsNullOrWhiteSpace(request.Email) ||
                string.IsNullOrWhiteSpace(request.Password) ||
                string.IsNullOrWhiteSpace(request.Role))
            {
                return Json(new { success = false, error = "All fields are required" });
            }

            // Check if user ID already exists
            var existingUser = await _context.Users.FindAsync(request.UserId);
            if (existingUser != null)
            {
                return Json(new { success = false, error = "User ID already exists" });
            }

            // Check if email already exists
            var existingEmail = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (existingEmail != null)
            {
                return Json(new { success = false, error = "Email already exists" });
            }

            // Validate role
            var validRoles = new[] { "admin", "staff", "member" };
            if (!validRoles.Contains(request.Role.ToLower()))
            {
                return Json(new { success = false, error = "Invalid role. Must be admin, staff, or member" });
            }

            var hashedPassword = HashPassword(request.Password);

            var newUser = new User
            {
                UserId = request.UserId,
                Name = request.Name,
                Email = request.Email,
                Password = hashedPassword,
                Role = request.Role.ToLower()
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                user = new
                {
                    userId = newUser.UserId,
                    name = newUser.Name,
                    email = newUser.Email,
                    role = newUser.Role
                }
            });
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
            if (string.IsNullOrWhiteSpace(product.Name))
            {
                return Json(new { success = false, error = "Product name is required" });
            }

            if (string.IsNullOrWhiteSpace(product.Category))
            {
                return Json(new { success = false, error = "Product category is required" });
            }

            if (product.Price <= 0)
            {
                return Json(new { success = false, error = "Product price must be greater than 0" });
            }

            string categoryPrefix = GetCategoryPrefix(product.Category);
            string newId = await GenerateNextProductId(categoryPrefix);

            product.Id = newId;
            product.ImagePath = product.ImagePath ?? "/images/default-product.jpg";
            product.Description = product.Description ?? "";
            product.Sold = 0;
            product.IsDisabled = false;

            _context.Products.Add(product);
            await _context.SaveChangesAsync();
            return Json(new { success = true, product });
        }
        catch (Exception ex)
        {
            var errorMessage = ex.Message;
            if (ex.InnerException != null)
            {
                errorMessage += " Inner Exception: " + ex.InnerException.Message;
            }

            // Log the full exception for debugging
            Console.WriteLine($"[v0] Product save error: {ex}");

            return Json(new { success = false, error = errorMessage });
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

            // Remove related OrderDetails first
            var relatedOrderDetails = await _context.OrderDetails
                .Where(od => od.ProductId == id)
                .ToListAsync();
            _context.OrderDetails.RemoveRange(relatedOrderDetails);

            // Remove the product
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

    [HttpPost]
    public async Task<IActionResult> AddProductWithImage()
    {
        var userRole = HttpContext.Session.GetString("UserRole");
        if (userRole != "admin" && userRole != "staff")
        {
            return Json(new { success = false, error = "Unauthorized" });
        }

        try
        {
            var name = Request.Form["name"].ToString();
            var priceStr = Request.Form["price"].ToString();
            var description = Request.Form["description"].ToString();
            var category = Request.Form["category"].ToString();
            var imageFile = Request.Form.Files["imageFile"];

            if (string.IsNullOrWhiteSpace(name))
            {
                return Json(new { success = false, error = "Product name is required" });
            }

            if (string.IsNullOrWhiteSpace(category))
            {
                return Json(new { success = false, error = "Product category is required" });
            }

            if (!decimal.TryParse(priceStr, out decimal price) || price <= 0)
            {
                return Json(new { success = false, error = "Valid price is required" });
            }

            // Get category prefix for product ID generation
            var categoryEntity = await _context.Categories.FirstOrDefaultAsync(c => c.Name == category && c.IsActive);
            string categoryPrefix = categoryEntity?.Prefix ?? GetCategoryPrefix(category);
            string newId = await GenerateNextProductId(categoryPrefix);

            // Handle image upload
            string imagePath = "/images/default-product.jpg";
            if (imageFile != null && imageFile.Length > 0)
            {
                // Create category-specific directory
                var categoryDir = Path.Combine("wwwroot", "images", "products", category.ToLower());
                if (!Directory.Exists(categoryDir))
                {
                    Directory.CreateDirectory(categoryDir);
                }

                // Generate unique filename
                var fileExtension = Path.GetExtension(imageFile.FileName);
                var fileName = $"{newId}_{DateTime.Now:yyyyMMddHHmmss}{fileExtension}";
                var filePath = Path.Combine(categoryDir, fileName);

                // Save file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(stream);
                }

                imagePath = $"/images/products/{category.ToLower()}/{fileName}";
            }

            var product = new Product
            {
                Id = newId,
                Name = name,
                Price = price,
                Description = description ?? "",
                Category = category,
                ImagePath = imagePath,
                Sold = 0,
                IsDisabled = false
            };

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
    public async Task<IActionResult> UpdateProductWithImage([FromForm] UpdateProductRequest request, IFormFile? imageFile)
    {
        var userRole = HttpContext.Session.GetString("UserRole");
        if (userRole != "admin" && userRole != "staff")
        {
            return Json(new { success = false, error = "Unauthorized" });
        }

        try
        {
            var existing = await _context.Products.FindAsync(request.Id);
            if (existing == null)
            {
                return Json(new { success = false, error = "Product not found" });
            }

            // Handle image upload if provided
            if (imageFile != null && imageFile.Length > 0)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                var fileExtension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();

                if (!allowedExtensions.Contains(fileExtension))
                {
                    return Json(new { success = false, error = "Invalid file type. Only JPG, PNG, GIF, and WebP files are allowed." });
                }

                if (imageFile.Length > 5 * 1024 * 1024) // 5MB limit
                {
                    return Json(new { success = false, error = "File size must be less than 5MB" });
                }

                // Delete old image if it exists and is not the default
                if (!string.IsNullOrEmpty(existing.ImagePath) && !existing.ImagePath.Contains("default-product"))
                {
                    var oldImagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", existing.ImagePath.TrimStart('/'));
                    if (System.IO.File.Exists(oldImagePath))
                    {
                        System.IO.File.Delete(oldImagePath);
                    }
                }

                var categoryFolder = request.Category.ToLower();
                var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "products", categoryFolder);
                if (!Directory.Exists(uploadsPath))
                {
                    Directory.CreateDirectory(uploadsPath);
                }

                // Generate unique filename
                var fileName = Guid.NewGuid().ToString() + fileExtension;
                var filePath = Path.Combine(uploadsPath, fileName);

                // Save file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(stream);
                }

                existing.ImagePath = $"/uploads/products/{categoryFolder}/{fileName}";
            }

            // Update other properties
            existing.Name = request.Name;
            existing.Price = request.Price;
            existing.Description = request.Description ?? "";
            existing.Category = request.Category;

            await _context.SaveChangesAsync();
            return Json(new { success = true, product = existing });
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
                var csv = new System.Text.StringBuilder();
                csv.AppendLine("Order ID,Order Date,Customer Name,Customer Email,Status,Total Amount,Items");

                foreach (var order in orders)
                {
                    var items = string.Join("; ", order.OrderDetails.Select(od => $"{od.Product.Name} x{od.Quantity}"));
                    csv.AppendLine($"{order.OrderId},{order.OrderDate:yyyy-MM-dd HH:mm},{order.User?.Name ?? "Guest"},{order.User?.Email ?? "N/A"},{order.Status},{order.TotalAmount:C},\"{items}\"");
                }

                var bytes = System.Text.Encoding.UTF8.GetBytes(csv.ToString());
                return File(bytes, "text/csv", $"orders_{DateTime.Now:yyyyMMdd}.csv");
            }

            return Json(new { success = false, error = "Unsupported format" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, error = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetUsersData(int page = 1, int pageSize = 20, string sortBy = "name", string sortOrder = "asc", string role = "")
    {
        var userRole = HttpContext.Session.GetString("UserRole");
        if (userRole != "admin" && userRole != "staff")
        {
            return Json(new { success = false, error = "Unauthorized" });
        }

        try
        {
            var query = _context.Users.AsQueryable();

            // Filter by role if specified
            if (!string.IsNullOrEmpty(role) && role != "all")
            {
                query = query.Where(u => u.Role.ToLower() == role.ToLower());
            }

            // Apply sorting
            query = sortBy.ToLower() switch
            {
                "name" => sortOrder == "desc" ? query.OrderByDescending(u => u.Name) : query.OrderBy(u => u.Name),
                "email" => sortOrder == "desc" ? query.OrderByDescending(u => u.Email) : query.OrderBy(u => u.Email),
                "role" => sortOrder == "desc" ? query.OrderByDescending(u => u.Role) : query.OrderBy(u => u.Role),
                _ => query.OrderBy(u => u.Name)
            };

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            var users = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Json(new
            {
                success = true,
                users = users,
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


    private string GetCategoryPrefix(string category)
    {
        var categoryEntity = _context.Categories.FirstOrDefault(c => c.Name == category && c.IsActive);
        if (categoryEntity != null)
        {
            return categoryEntity.Prefix;
        }

        // Fallback to hardcoded prefixes for existing categories
        return category.ToLower() switch
        {
            "pizza" => "PZ",
            "pasta" => "PA",
            "seafood" => "SF",
            "burgers" => "BG",
            "beverages" => "BV",
            "desserts" => "DS",
            "specials" => "SP",
            _ => "GN" // General
        };
    }

    // Helper method to get category prefix
    private string GetCategoryPrefix_Original(string category)
    {
        return category.ToLower() switch
        {
            "burgers" => "BG",
            "beverages" => "BV",
            "desserts" => "DS",
            "pasta" => "PA",
            "pizza" => "PZ",
            "seafood" => "SF",
            "specials" => "SP",
            _ => "GN" // General for unknown categories
        };
    }

    private async Task<string> GenerateNextProductId(string prefix)
    {
        try
        {
            // Find the highest existing ID with this prefix
            var existingIds = await _context.Products
                .Where(p => p.Id.StartsWith(prefix))
                .Select(p => p.Id)
                .ToListAsync();

            int maxNumber = 0;
            foreach (var id in existingIds)
            {
                if (id.Length == 5 && int.TryParse(id.Substring(2), out int number))
                {
                    maxNumber = Math.Max(maxNumber, number);
                }
            }

            // Generate next number with leading zeros
            int nextNumber = maxNumber + 1;
            string newId = $"{prefix}{nextNumber:D3}";

            while (await _context.Products.AnyAsync(p => p.Id == newId))
            {
                nextNumber++;
                newId = $"{prefix}{nextNumber:D3}";
            }

            Console.WriteLine($"[v0] Generated new product ID: {newId}");
            return newId;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[v0] Error generating product ID: {ex}");
            throw;
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetProductById(string id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null) return Json(new { success = false, error = "Product not found" });
        return Json(new { success = true, product });
    }
}
