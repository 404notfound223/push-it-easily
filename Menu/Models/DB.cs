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
    public required string? Description { get; set; }
    public required string Category { get; set; }
    public required string ImagePath { get; set; }
    public int Sold { get; set; } = 0;
    public int Stock { get; set; } = 0;
}

public class Order
{
    [Key]
    public required string OrderId { get; set; }
    public DateTime OrderDate { get; set; } = DateTime.Now;

    [Precision(10, 2)]
    public decimal TotalAmount { get; set; }
    public required string Status { get; set; }

    // Foreign key
    public required string UserId { get; set; }
    public required User User { get; set; }

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


public class VerificationRequest
{
    public required string Email { get; set; }
}

public class CreateOrderRequest
{
    public decimal TotalAmount { get; set; }
    public List<OrderItemRequest> Items { get; set; } = new List<OrderItemRequest>();
}

public class OrderItemRequest
{
    public string ProductId { get; set; } = string.Empty;
    public int Quantity { get; set; }
}

//public class CreateCheckoutRequest
//{
//    public decimal TotalAmount { get; set; }
//    public List<CartItem> Items { get; set; } = new();
//}

//public class CartItem
//{
//    public string ProductId { get; set; }
//    public int Quantity { get; set; }
//}
