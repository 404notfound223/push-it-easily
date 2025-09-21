using Menu.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Menu.Controllers
{
    public class PMController : Controller
    {
        private readonly DB _context;

        public PMController(DB context)
        {
            _context = context;
        }

        // Product Management View
        public async Task<IActionResult> ProductManage()
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "admin" && userRole != "staff")
            {
                return RedirectToAction("Login", "Login");
            }

            var model = new StaffDashboardViewModel
            {
                UserRole = userRole,
                Products = await _context.Products.ToListAsync()
            };

            return View(model);
        }

        // Product Management API Methods
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
    }
}
