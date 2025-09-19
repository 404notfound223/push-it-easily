using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Menu.Models;

public class DB : DbContext
{
    public DB(DbContextOptions options) : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderDetail> OrderDetails { get; set; }
    public DbSet<Category> Categories { get; set; }

    protected DB()
    {
    }
}

public class Product
{
    [Key]
    public required string Id { get; set; }
    public required string Name { get; set; }
    [Precision(4, 2)]
    public required decimal Price { get; set; }
    public string Description { get; set; } // Fixed contradictory required nullable field - made it optional
    public required string Category { get; set; }
    public required string ImagePath { get; set; }
    public int Sold { get; set; } = 0;
    public bool IsDisabled { get; set; } = false;
}

public class Order
{
    [Key]
    public string OrderId { get; set; } = null!;
    public DateTime OrderDate { get; set; } = DateTime.Now;

    [Precision(10, 2)]
    public decimal TotalAmount { get; set; }
    public required string Status { get; set; }

    // Foreign key
    public string? UserId { get; set; }
    public User? User { get; set; }

    // Relationships
    public ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
}

public class OrderDetail
{
    [Key]
    public required string OrderDetailId { get; set; }

    // Foreign keys
    public required string OrderId { get; set; }
    public required Order Order { get; set; }

    public required string ProductId { get; set; }
    public required Product Product { get; set; }

    public required int Quantity { get; set; }

    [Precision(10, 2)]
    public decimal UnitPrice { get; set; }
}

[Table("User")]
public class User
{
    [Key]
    [MaxLength(50)]
    public required string UserId { get; set; }

    [MaxLength(50)]
    public required string Name { get; set; }

    [MaxLength(255)]
    public required string Password { get; set; }

    [MaxLength(255)]
    public required string Email { get; set; }

    [MaxLength(20)]
    public required string Role { get; set; }

    public ICollection<Order> Orders { get; set; } = new List<Order>();
}

public class Category
{
    [Key]
    public required string Id { get; set; }

    [MaxLength(100)]
    public required string Name { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    [MaxLength(2)]
    public required string Prefix { get; set; } // 2-character prefix for product IDs

    public bool IsActive { get; set; } = true;

    public DateTime CreatedDate { get; set; } = DateTime.Now;
}

// need to move all the method below to separate files later
public class VerificationRequest
{
    public required string Email { get; set; }
}

public class CreateOrderRequest
{
    public decimal TotalAmount { get; set; }
    public List<OrderItemRequest> Items { get; set; } = new List<OrderItemRequest>();
    public string? Email { get; set; } // Add this for guest checkout
}

public class OrderItemRequest
{
    public string ProductId { get; set; } = string.Empty;
    public int Quantity { get; set; }
}

public class StaffDashboardViewModel
{
    public string UserRole { get; set; } = string.Empty;
    public List<Order> Orders { get; set; } = new List<Order>();
    public List<User> Users { get; set; } = new List<User>();
    public List<Product> Products { get; set; } = new List<Product>();
}

public class UpdateUserRequest
{
    public string UserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

public class ToggleStatusRequest
{
    public int Id { get; set; }
    public bool Disable { get; set; }
}

public class ToggleDisableRequest
{
    public string ProductId { get; set; } = string.Empty;
    public bool Disable { get; set; }
}

public class AddUserRequest
{
    public required string UserId { get; set; }
    public required string Name { get; set; }
    public required string Email { get; set; }
    public required string Password { get; set; }
    public required string Role { get; set; }
}

public class AddProductRequest
{
    public required string Name { get; set; }
    public required decimal Price { get; set; }
    public string? Description { get; set; }
    public required string Category { get; set; }
}

public class UpdateProductRequest
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required decimal Price { get; set; }
    public string? Description { get; set; }
    public required string Category { get; set; }
}

public class UpdateOrderRequest
{
    public required string OrderId { get; set; }
    public List<UpdateOrderItemRequest> Items { get; set; } = new List<UpdateOrderItemRequest>();
    public decimal TotalAmount { get; set; }
}

public class UpdateOrderItemRequest
{
    public required string ItemId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}

public class BulkOrderStatusRequest
{
    public List<string> OrderIds { get; set; } = new List<string>();
    public required string NewStatus { get; set; }
}

public class BulkDeleteOrdersRequest
{
    public List<string> OrderIds { get; set; } = new List<string>();
}

public class AddCategoryRequest
{
    public required string Name { get; set; }
    public string? Description { get; set; }
    public required string Prefix { get; set; }
}

public class UpdateCategoryRequest
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public required string Prefix { get; set; }
    public bool IsActive { get; set; }
}
