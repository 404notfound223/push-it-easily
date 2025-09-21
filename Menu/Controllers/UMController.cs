using Menu.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace Menu.Controllers
{
    public class UMController : Controller
    {
        private readonly DB _context;

        public UMController(DB context)
        {
            _context = context;
        }

        // User Management View
        public async Task<IActionResult> UserManage()
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "admin")
            {
                return RedirectToAction("Login", "Login");
            }

            var model = new StaffDashboardViewModel
            {
                UserRole = userRole,
                Users = await _context.Users.ToListAsync()
            };

            return View(model);
        }

        // User Management API Methods
        [HttpGet]
        public async Task<IActionResult> GetUserById(string userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return Json(new { success = false, error = "User not found" });
            return Json(new { success = true, user });
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

        [HttpGet]
        public async Task<IActionResult> GetUsersData(int page = 1, int pageSize = 20, string sortBy = "name", string sortOrder = "asc", string role = "")
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "admin")
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

        private string HashPassword(string password)
        {
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(password);
                var hash = sha256.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }
    }
}
