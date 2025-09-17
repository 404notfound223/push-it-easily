using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Threading.Tasks;

namespace Menu.Controllers
{
    public class FileUploadController : Controller
    {
        private readonly IWebHostEnvironment _environment;

        public FileUploadController(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        [HttpPost]
        public async Task<IActionResult> UploadImage(IFormFile file)
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "admin" && userRole != "staff")
            {
                return Json(new { success = false, error = "Unauthorized" });
            }

            if (file == null || file.Length == 0)
            {
                return Json(new { success = false, error = "No file selected" });
            }

            // Validate file type
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(fileExtension))
            {
                return Json(new { success = false, error = "Invalid file type. Only JPG, PNG, GIF, and WebP files are allowed." });
            }

            // Validate file size (max 5MB)
            if (file.Length > 5 * 1024 * 1024)
            {
                return Json(new { success = false, error = "File size must be less than 5MB" });
            }

            try
            {
                // Create uploads directory if it doesn't exist
                var uploadsPath = Path.Combine(_environment.WebRootPath, "uploads", "products");
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
                    await file.CopyToAsync(stream);
                }

                // Return relative path for database storage
                var relativePath = $"/uploads/products/{fileName}";

                return Json(new { success = true, imagePath = relativePath, fileName = fileName });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = $"Upload failed: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UploadMultipleImages(List<IFormFile> files)
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "admin" && userRole != "staff")
            {
                return Json(new { success = false, error = "Unauthorized" });
            }

            if (files == null || files.Count == 0)
            {
                return Json(new { success = false, error = "No files selected" });
            }

            var uploadedFiles = new List<object>();
            var errors = new List<string>();

            foreach (var file in files)
            {
                if (file.Length == 0) continue;

                // Validate file type
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();

                if (!allowedExtensions.Contains(fileExtension))
                {
                    errors.Add($"{file.FileName}: Invalid file type");
                    continue;
                }

                // Validate file size (max 5MB)
                if (file.Length > 5 * 1024 * 1024)
                {
                    errors.Add($"{file.FileName}: File size must be less than 5MB");
                    continue;
                }

                try
                {
                    // Create uploads directory if it doesn't exist
                    var uploadsPath = Path.Combine(_environment.WebRootPath, "uploads", "products");
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
                        await file.CopyToAsync(stream);
                    }

                    // Return relative path for database storage
                    var relativePath = $"/uploads/products/{fileName}";

                    uploadedFiles.Add(new
                    {
                        originalName = file.FileName,
                        fileName = fileName,
                        imagePath = relativePath,
                        size = file.Length
                    });
                }
                catch (Exception ex)
                {
                    errors.Add($"{file.FileName}: Upload failed - {ex.Message}");
                }
            }

            return Json(new
            {
                success = uploadedFiles.Count > 0,
                uploadedFiles = uploadedFiles,
                errors = errors,
                totalUploaded = uploadedFiles.Count,
                totalErrors = errors.Count
            });
        }

        [HttpPost]
        public IActionResult DeleteImage(string imagePath)
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "admin" && userRole != "staff")
            {
                return Json(new { success = false, error = "Unauthorized" });
            }

            try
            {
                if (string.IsNullOrEmpty(imagePath))
                {
                    return Json(new { success = false, error = "No image path provided" });
                }

                // Don't delete default images
                if (imagePath.Contains("default-product") || imagePath.Contains("/images/"))
                {
                    return Json(new { success = false, error = "Cannot delete default images" });
                }

                // Convert relative path to absolute path
                var fullPath = Path.Combine(_environment.WebRootPath, imagePath.TrimStart('/'));

                if (System.IO.File.Exists(fullPath))
                {
                    System.IO.File.Delete(fullPath);
                    return Json(new { success = true });
                }
                else
                {
                    return Json(new { success = false, error = "File not found" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = $"Delete failed: {ex.Message}" });
            }
        }

        [HttpGet]
        public IActionResult GetUploadedImages(int page = 1, int pageSize = 20)
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "admin" && userRole != "staff")
            {
                return Json(new { success = false, error = "Unauthorized" });
            }

            try
            {
                var uploadsPath = Path.Combine(_environment.WebRootPath, "uploads", "products");

                if (!Directory.Exists(uploadsPath))
                {
                    return Json(new
                    {
                        success = true,
                        images = new List<object>(),
                        currentPage = page,
                        totalPages = 0,
                        totalCount = 0
                    });
                }

                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                var files = Directory.GetFiles(uploadsPath)
                    .Where(f => allowedExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
                    .OrderByDescending(f => new FileInfo(f).CreationTime)
                    .ToList();

                var totalCount = files.Count;
                var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

                var pagedFiles = files
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(f => {
                        var fileInfo = new FileInfo(f);
                        var fileName = Path.GetFileName(f);
                        return new
                        {
                            fileName = fileName,
                            imagePath = $"/uploads/products/{fileName}",
                            size = fileInfo.Length,
                            createdDate = fileInfo.CreationTime,
                            sizeFormatted = FormatFileSize(fileInfo.Length)
                        };
                    })
                    .ToList();

                return Json(new
                {
                    success = true,
                    images = pagedFiles,
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
        public IActionResult DeleteMultipleImages([FromBody] DeleteMultipleImagesRequest request)
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "admin" && userRole != "staff")
            {
                return Json(new { success = false, error = "Unauthorized" });
            }

            try
            {
                var deletedCount = 0;
                var errors = new List<string>();

                foreach (var imagePath in request.ImagePaths)
                {
                    try
                    {
                        // Don't delete default images
                        if (imagePath.Contains("default-product") || imagePath.Contains("/images/"))
                        {
                            errors.Add($"{imagePath}: Cannot delete default images");
                            continue;
                        }

                        // Convert relative path to absolute path
                        var fullPath = Path.Combine(_environment.WebRootPath, imagePath.TrimStart('/'));

                        if (System.IO.File.Exists(fullPath))
                        {
                            System.IO.File.Delete(fullPath);
                            deletedCount++;
                        }
                        else
                        {
                            errors.Add($"{imagePath}: File not found");
                        }
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"{imagePath}: {ex.Message}");
                    }
                }

                return Json(new
                {
                    success = deletedCount > 0,
                    deletedCount = deletedCount,
                    errors = errors,
                    totalRequested = request.ImagePaths.Count
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpGet]
        public IActionResult GetImageInfo(string imagePath)
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "admin" && userRole != "staff")
            {
                return Json(new { success = false, error = "Unauthorized" });
            }

            try
            {
                if (string.IsNullOrEmpty(imagePath))
                {
                    return Json(new { success = false, error = "No image path provided" });
                }

                var fullPath = Path.Combine(_environment.WebRootPath, imagePath.TrimStart('/'));

                if (!System.IO.File.Exists(fullPath))
                {
                    return Json(new { success = false, error = "File not found" });
                }

                var fileInfo = new FileInfo(fullPath);

                return Json(new
                {
                    success = true,
                    fileName = fileInfo.Name,
                    size = fileInfo.Length,
                    sizeFormatted = FormatFileSize(fileInfo.Length),
                    createdDate = fileInfo.CreationTime,
                    modifiedDate = fileInfo.LastWriteTime,
                    extension = fileInfo.Extension,
                    imagePath = imagePath
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        private static string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }
    }

    public class DeleteMultipleImagesRequest
    {
        public List<string> ImagePaths { get; set; } = new List<string>();
    }
}
