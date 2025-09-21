using Menu.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace Menu.Services
{
    public class PaymentSecurityService
    {
        private readonly DB _context;
        private readonly ILogger<PaymentSecurityService> _logger;
        private static readonly Dictionary<string, DateTime> _rateLimitTracker = new();
        private static readonly Dictionary<string, int> _attemptCounter = new();
        private const int MAX_ATTEMPTS_PER_MINUTE = 5;
        private const int MAX_ATTEMPTS_PER_HOUR = 20;

        public PaymentSecurityService(DB context, ILogger<PaymentSecurityService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<PaymentValidationResult> ValidatePaymentRequest(SecureCreateOrderRequest request, string? userId, string clientIp)
        {
            var result = new PaymentValidationResult();

            try
            {
                // Rate limiting check
                if (!CheckRateLimit(clientIp))
                {
                    result.Errors.Add("Too many payment attempts. Please try again later.");
                    _logger.LogWarning("Rate limit exceeded for IP: {ClientIp}", clientIp);
                    return result;
                }

                // Validate request token (basic CSRF protection)
                if (!ValidateRequestToken(request.RequestToken))
                {
                    result.Errors.Add("Invalid request token.");
                    _logger.LogWarning("Invalid request token from IP: {ClientIp}", clientIp);
                    return result;
                }

                // Validate products exist and are available
                var productValidation = await ValidateProducts(request.Items);
                if (!productValidation.IsValid)
                {
                    result.Errors.AddRange(productValidation.Errors);
                    return result;
                }

                result.ValidatedProducts = productValidation.ValidatedProducts;

                // Calculate and validate total amount
                var calculationResult = CalculateOrderTotal(request.Items, result.ValidatedProducts, userId);
                result.CalculatedTotal = calculationResult.Total;
                result.MemberDiscount = calculationResult.MemberDiscount;

                // Validate submitted total matches calculated total (within tolerance)
                const decimal tolerance = 0.02m; // 2 cent tolerance for rounding
                if (Math.Abs(request.TotalAmount - result.CalculatedTotal) > tolerance)
                {
                    result.Errors.Add($"Total amount mismatch. Expected: ${result.CalculatedTotal:F2}, Received: ${request.TotalAmount:F2}");
                    _logger.LogWarning("Price manipulation attempt detected. Expected: {Expected}, Received: {Received}, IP: {ClientIp}",
                        result.CalculatedTotal, request.TotalAmount, clientIp);
                    return result;
                }

                // Validate individual item prices
                foreach (var item in request.Items)
                {
                    var product = result.ValidatedProducts.FirstOrDefault(p => p.Id == item.ProductId);
                    if (product != null && Math.Abs(item.ExpectedPrice - product.Price) > tolerance)
                    {
                        result.Errors.Add($"Price mismatch for {product.Name}. Expected: ${product.Price:F2}, Received: ${item.ExpectedPrice:F2}");
                        _logger.LogWarning("Item price manipulation detected for product {ProductId}. Expected: {Expected}, Received: {Received}, IP: {ClientIp}",
                            product.Id, product.Price, item.ExpectedPrice, clientIp);
                        return result;
                    }
                }

                // Additional business rule validations
                if (request.Items.Sum(i => i.Quantity) > 50)
                {
                    result.Errors.Add("Order cannot contain more than 50 total items.");
                }

                if (result.CalculatedTotal > 10000m)
                {
                    result.Errors.Add("Order total cannot exceed $10,000.00.");
                }

                result.IsValid = result.Errors.Count == 0;

                if (result.IsValid)
                {
                    _logger.LogInformation("Payment validation successful for order total: {Total}, IP: {ClientIp}", result.CalculatedTotal, clientIp);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during payment validation for IP: {ClientIp}", clientIp);
                result.Errors.Add("Payment validation failed. Please try again.");
                return result;
            }
        }

        private bool CheckRateLimit(string clientIp)
        {
            var now = DateTime.UtcNow;
            var key = $"{clientIp}_{now:yyyyMMddHHmm}";
            var hourKey = $"{clientIp}_{now:yyyyMMddHH}";

            // Clean old entries
            var keysToRemove = _rateLimitTracker.Keys.Where(k => _rateLimitTracker[k] < now.AddHours(-1)).ToList();
            foreach (var oldKey in keysToRemove)
            {
                _rateLimitTracker.Remove(oldKey);
                _attemptCounter.Remove(oldKey);
            }

            // Check minute limit
            if (_attemptCounter.ContainsKey(key))
            {
                _attemptCounter[key]++;
                if (_attemptCounter[key] > MAX_ATTEMPTS_PER_MINUTE)
                {
                    return false;
                }
            }
            else
            {
                _attemptCounter[key] = 1;
                _rateLimitTracker[key] = now;
            }

            // Check hourly limit
            var hourlyAttempts = _attemptCounter.Keys
                .Where(k => k.StartsWith($"{clientIp}_") && k.Contains(now.ToString("yyyyMMddHH")))
                .Sum(k => _attemptCounter[k]);

            return hourlyAttempts <= MAX_ATTEMPTS_PER_HOUR;
        }

        private bool ValidateRequestToken(string token)
        {
            // Simple token validation - in production, use proper CSRF tokens
            if (string.IsNullOrEmpty(token) || token.Length < 10)
                return false;

            // Validate token format and timestamp (basic implementation)
            try
            {
                var parts = token.Split('_');
                if (parts.Length != 2) return false;

                var timestamp = long.Parse(parts[1]);
                var tokenTime = DateTimeOffset.FromUnixTimeSeconds(timestamp);

                // Token valid for 1 hour
                return DateTime.UtcNow - tokenTime.DateTime < TimeSpan.FromHours(1);
            }
            catch
            {
                return false;
            }
        }

        private async Task<PaymentValidationResult> ValidateProducts(List<SecureOrderItemRequest> items)
        {
            var result = new PaymentValidationResult();
            var productIds = items.Select(i => i.ProductId).Distinct().ToList();

            var products = await _context.Products
                .Where(p => productIds.Contains(p.Id))
                .ToListAsync();

            foreach (var item in items)
            {
                var product = products.FirstOrDefault(p => p.Id == item.ProductId);
                if (product == null)
                {
                    result.Errors.Add($"Product with ID {item.ProductId} not found.");
                    continue;
                }

                if (product.IsDisabled)
                {
                    result.Errors.Add($"Product {product.Name} is currently unavailable.");
                    continue;
                }

                if (item.Quantity <= 0 || item.Quantity > 99)
                {
                    result.Errors.Add($"Invalid quantity for {product.Name}. Must be between 1 and 99.");
                    continue;
                }

                result.ValidatedProducts.Add(product);
            }

            result.IsValid = result.Errors.Count == 0;
            return result;
        }

        private (decimal Total, decimal MemberDiscount) CalculateOrderTotal(List<SecureOrderItemRequest> items, List<Product> products, string? userId)
        {
            decimal subtotal = 0;

            foreach (var item in items)
            {
                var product = products.FirstOrDefault(p => p.Id == item.ProductId);
                if (product != null)
                {
                    subtotal += product.Price * item.Quantity;
                }
            }

            var tax = subtotal * 0.085m;
            var memberDiscount = 0m;

            // Check if user is a member (simplified - in production, query database)
            if (!string.IsNullOrEmpty(userId))
            {
                var user = _context.Users.FirstOrDefault(u => u.UserId == userId);
                if (user?.Role?.ToLower() == "member")
                {
                    memberDiscount = subtotal * 0.10m;
                }
            }

            var total = subtotal + tax - memberDiscount;
            return (total, memberDiscount);
        }

        public string GenerateRequestToken()
        {
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var randomBytes = new byte[16];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomBytes);
            }
            var randomString = Convert.ToBase64String(randomBytes).Replace("+", "").Replace("/", "").Replace("=", "");
            return $"{randomString}_{timestamp}";
        }

        public void LogPaymentAttempt(string clientIp, string? userId, decimal amount, bool success, string? error = null)
        {
            if (success)
            {
                _logger.LogInformation("Payment attempt successful - IP: {ClientIp}, User: {UserId}, Amount: {Amount}",
                    clientIp, userId ?? "guest", amount);
            }
            else
            {
                _logger.LogWarning("Payment attempt failed - IP: {ClientIp}, User: {UserId}, Amount: {Amount}, Error: {Error}",
                    clientIp, userId ?? "guest", amount, error ?? "Unknown");
            }
        }
    }
}
