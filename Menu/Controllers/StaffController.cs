using Menu.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Menu.Controllers
{
    public class StaffController : Controller
    {
        private readonly DB _context;

        public StaffController(DB context)
        {
            _context = context;
        }

        public async Task<IActionResult> Dashboard()
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "admin" && userRole != "staff")
            {
                return RedirectToAction("Login", "Login");
            }

            // Redirect to Order Management as the default landing page
            return RedirectToAction("OrderManage", "OM");
        }

        [HttpPost]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login", "Login");
        }

        [HttpPost]
        public async Task<IActionResult> ToggleDisable(string id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
                return NotFound();

            product.IsDisabled = !product.IsDisabled;
            await _context.SaveChangesAsync();

            return RedirectToAction("ProductManage", "PM");
        }

        [HttpGet]
        public async Task<IActionResult> GetCategoriesData(int page = 1, int pageSize = 20, string sortBy = "name", string sortOrder = "asc", string status = "")
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "admin" && userRole != "staff")
            {
                return Json(new { success = false, error = "Unauthorized" });
            }

            try
            {
                var query = _context.Categories.AsQueryable();

                // Filter by status if specified
                if (!string.IsNullOrEmpty(status) && status != "all")
                {
                    bool isActive = status == "active";
                    query = query.Where(c => c.IsActive == isActive);
                }

                // Apply sorting
                query = sortBy.ToLower() switch
                {
                    "name" => sortOrder == "desc" ? query.OrderByDescending(c => c.Name) : query.OrderBy(c => c.Name),
                    "prefix" => sortOrder == "desc" ? query.OrderByDescending(c => c.Prefix) : query.OrderBy(c => c.Prefix),
                    "created" => sortOrder == "desc" ? query.OrderByDescending(c => c.CreatedDate) : query.OrderBy(c => c.CreatedDate),
                    _ => query.OrderBy(c => c.Name)
                };

                var totalCount = await query.CountAsync();
                var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

                var categories = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                return Json(new
                {
                    success = true,
                    categories = categories,
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
}
