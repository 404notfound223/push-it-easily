using Menu.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Menu.Controllers
{
    public class MenuController : Controller
    {
        private readonly DB _db;

        public MenuController(DB db)
        {
            _db = db;
        }

        public async Task<IActionResult> All(PagingRequest request)
        {
            var userRole = HttpContext.Session.GetString("UserRole");

            // For AJAX requests, return JSON
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest" || Request.Query.ContainsKey("ajax"))
            {
                var pagedResult = await GetPagedProducts(request, userRole);
                return Json(new { success = true, result = pagedResult });
            }

            // For regular requests, return view
            var products = await _db.Products.ToListAsync();

            // For members, filter out disabled products
            if (userRole != "admin" && userRole != "staff")
            {
                products = products.Where(p => !p.IsDisabled).ToList();
            }

            return View(products);
        }

        [HttpGet]
        public async Task<IActionResult> GetPagedProducts(PagingRequest request)
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            var pagedResult = await GetPagedProducts(request, userRole);
            return Json(new { success = true, result = pagedResult });
        }

        private async Task<PagedResult<Product>> GetPagedProducts(PagingRequest request, string? userRole)
        {
            var query = _db.Products.AsQueryable();

            // Filter by category if specified
            if (!string.IsNullOrEmpty(request.Category))
            {
                if (request.Category.ToLower() == "specials")
                {
                    query = query.Where(p => EF.Functions.Like(p.Category, "%special%"));
                }
                else
                {
                    query = query.Where(p => p.Category == request.Category ||
                                           (request.Category == "Burgers" && (p.Category == "Burger" || p.Category == "Burgers")));
                }
            }

            // Search functionality
            if (!string.IsNullOrEmpty(request.SearchTerm))
            {
                var searchTerm = request.SearchTerm.ToLower();
                query = query.Where(p => p.Name.ToLower().Contains(searchTerm) ||
                                       p.Description.ToLower().Contains(searchTerm) ||
                                       p.Category.ToLower().Contains(searchTerm));
            }

            // For members, filter out disabled products
            if (userRole != "admin" && userRole != "staff")
            {
                query = query.Where(p => !p.IsDisabled);
            }

            // Sorting
            if (!string.IsNullOrEmpty(request.SortBy))
            {
                switch (request.SortBy.ToLower())
                {
                    case "name":
                        query = request.SortDirection == "desc"
                            ? query.OrderByDescending(p => p.Name)
                            : query.OrderBy(p => p.Name);
                        break;
                    case "price":
                        query = request.SortDirection == "desc"
                            ? query.OrderByDescending(p => p.Price)
                            : query.OrderBy(p => p.Price);
                        break;
                    case "category":
                        query = request.SortDirection == "desc"
                            ? query.OrderByDescending(p => p.Category)
                            : query.OrderBy(p => p.Category);
                        break;
                    default:
                        query = query.OrderBy(p => p.Name);
                        break;
                }
            }
            else
            {
                query = query.OrderBy(p => p.Name);
            }

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            return new PagedResult<Product>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = request.Page,
                PageSize = request.PageSize
            };
        }

        public async Task<IActionResult> Specials(PagingRequest request)
        {
            request.Category = "Specials";
            return await HandleCategoryRequest(request, "Specials");
        }

        public async Task<IActionResult> Pizza(PagingRequest request)
        {
            request.Category = "Pizza";
            return await HandleCategoryRequest(request, "Pizza");
        }

        public async Task<IActionResult> Pasta(PagingRequest request)
        {
            request.Category = "Pasta";
            return await HandleCategoryRequest(request, "Pasta");
        }

        public async Task<IActionResult> Seafood(PagingRequest request)
        {
            request.Category = "Seafood";
            return await HandleCategoryRequest(request, "Seafood");
        }

        public async Task<IActionResult> Burgers(PagingRequest request)
        {
            request.Category = "Burgers";
            return await HandleCategoryRequest(request, "Burgers");
        }

        public async Task<IActionResult> Beverages(PagingRequest request)
        {
            request.Category = "Beverages";
            return await HandleCategoryRequest(request, "Beverages");
        }

        public async Task<IActionResult> Desserts(PagingRequest request)
        {
            request.Category = "Desserts";
            return await HandleCategoryRequest(request, "Desserts");
        }

        private async Task<IActionResult> HandleCategoryRequest(PagingRequest request, string category)
        {
            var userRole = HttpContext.Session.GetString("UserRole");

            // For AJAX requests, return JSON
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest" || Request.Query.ContainsKey("ajax"))
            {
                var pagedResult = await GetPagedProducts(request, userRole);
                return Json(new { success = true, result = pagedResult });
            }

            // For regular requests, return view with filtered products
            var query = _db.Products.AsQueryable();

            if (category == "Specials")
            {
                query = query.Where(p => EF.Functions.Like(p.Category, "%special%"));
            }
            else if (category == "Burgers")
            {
                query = query.Where(p => p.Category == "Burger" || p.Category == "Burgers");
            }
            else
            {
                query = query.Where(p => p.Category == category);
            }

            var products = await query.ToListAsync();

            if (userRole != "admin" && userRole != "staff")
            {
                products = products.Where(p => !p.IsDisabled).ToList();
            }

            return View(products);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus([FromBody] ToggleStatusRequest request)
        {
            var product = await _db.Products.FirstOrDefaultAsync(p => p.Id.ToString() == request.Id.ToString());
            if (product == null)
                return NotFound();

            product.IsDisabled = request.Disable;
            await _db.SaveChangesAsync();

            return Json(new { success = true, isDisabled = product.IsDisabled });
        }

    }

}
