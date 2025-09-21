using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Menu.Models;
//using System.Linq.Dynamic.Core;

namespace Menu.Controllers
{
    public class HomeController : Controller
    {
        public readonly DB db;

        public HomeController(DB db)
        {
            this.db = db;
        }

        public IActionResult Index()
        {
            ViewBag.PageTitle = "The Secret Restaurant Menu";
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> LiveSearch(string term)
        {
            if (string.IsNullOrWhiteSpace(term))
            {
                return Json(new { success = false, results = new List<object>() });
            }

            try
            {
                var products = await db.Products
                    .Where(p => EF.Functions.Like(p.Name, $"%{term}%") ||
                               EF.Functions.Like(p.Description, $"%{term}%") ||
                               EF.Functions.Like(p.Category, $"%{term}%"))
                    .Select(p => new {
                        id = p.Id,
                        name = p.Name,
                        description = p.Description,
                        price = p.Price,
                        category = p.Category,
                        imageUrl = p.ImagePath
                    })
                    .Take(10) // Limit results to 10 items
                    .ToListAsync();

                return Json(new { success = true, results = products });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message, results = new List<object>() });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Search(string query, PagingRequest request)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return RedirectToAction("All", "Menu");
            }

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest" || Request.Query.ContainsKey("ajax"))
            {
                var pagedResult = await GetPagedSearchResults(query, request);
                return Json(new { success = true, result = pagedResult });
            }

            try
            {
                var products = await db.Products
                    .Where(p => EF.Functions.Like(p.Name, $"%{query}%") ||
                               EF.Functions.Like(p.Description, $"%{query}%") ||
                               EF.Functions.Like(p.Category, $"%{query}%"))
                    .ToListAsync();

                ViewBag.SearchQuery = query;
                ViewBag.ResultCount = products.Count;
                return View("SearchResult", products);
            }
            catch (Exception ex)
            {
                ViewBag.Error = ex.Message;
                ViewBag.SearchQuery = query;
                ViewBag.ResultCount = 0;
                return View("SearchResult", new List<Product>());
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetPagedSearchResults(PagingRequest request)
        {
            var query = Request.Query["query"].ToString();
            if (string.IsNullOrWhiteSpace(query))
            {
                return Json(new { success = false, error = "Search query is required" });
            }

            var pagedResult = await GetPagedSearchResults(query, request);
            return Json(new { success = true, result = pagedResult });
        }

        private async Task<PagedResult<object>> GetPagedSearchResults(string searchQuery, PagingRequest request)
        {
            var query = db.Products.AsQueryable();

            // Apply search filter
            if (!string.IsNullOrEmpty(searchQuery))
            {
                var searchTerm = searchQuery.ToLower();
                query = query.Where(p => EF.Functions.Like(p.Name.ToLower(), $"%{searchTerm}%") ||
                                        EF.Functions.Like(p.Description.ToLower(), $"%{searchTerm}%") ||
                                        EF.Functions.Like(p.Category.ToLower(), $"%{searchTerm}%"));
            }

            // Apply additional search term filter if provided
            if (!string.IsNullOrEmpty(request.SearchTerm))
            {
                var additionalTerm = request.SearchTerm.ToLower();
                query = query.Where(p => EF.Functions.Like(p.Name.ToLower(), $"%{additionalTerm}%") ||
                                        EF.Functions.Like(p.Description.ToLower(), $"%{additionalTerm}%") ||
                                        EF.Functions.Like(p.Category.ToLower(), $"%{additionalTerm}%"));
            }

            // Apply category filter
            if (!string.IsNullOrEmpty(request.Category))
            {
                query = query.Where(p => p.Category == request.Category);
            }

            // Apply sorting
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
            var products = await query
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(p => new {
                    id = p.Id,
                    name = p.Name,
                    description = p.Description,
                    price = p.Price,
                    category = p.Category,
                    imagePath = p.ImagePath,
                    isDisabled = p.IsDisabled
                })
                .ToListAsync();

            return new PagedResult<object>
            {
                Items = products.Cast<object>().ToList(),
                TotalCount = totalCount,
                PageNumber = request.Page,
                PageSize = request.PageSize
            };
        }
    }
}
