using System.ComponentModel.DataAnnotations;

namespace Menu.Models
{
    public class SecureCreateOrderRequest
    {
        [Required]
        [Range(0.01, 10000.00, ErrorMessage = "Total amount must be between $0.01 and $10,000.00")]
        public decimal TotalAmount { get; set; }

        [Required]
        [MinLength(1, ErrorMessage = "Order must contain at least one item")]
        [MaxLength(50, ErrorMessage = "Order cannot contain more than 50 items")]
        public List<SecureOrderItemRequest> Items { get; set; } = new List<SecureOrderItemRequest>();

        [EmailAddress(ErrorMessage = "Invalid email format")]
        [StringLength(255, ErrorMessage = "Email cannot exceed 255 characters")]
        public string? Email { get; set; }

        [Required]
        public string RequestToken { get; set; } = string.Empty; // CSRF protection
    }

    public class SecureOrderItemRequest
    {
        [Required]
        [StringLength(50, ErrorMessage = "Product ID cannot exceed 50 characters")]
        public string ProductId { get; set; } = string.Empty;

        [Required]
        [Range(1, 99, ErrorMessage = "Quantity must be between 1 and 99")]
        public int Quantity { get; set; }

        [Range(0.01, 1000.00, ErrorMessage = "Price must be between $0.01 and $1,000.00")]
        public decimal ExpectedPrice { get; set; } // Price validation
    }

    public class PaymentValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public decimal CalculatedTotal { get; set; }
        public decimal MemberDiscount { get; set; }
        public List<Product> ValidatedProducts { get; set; } = new List<Product>();
    }
}
