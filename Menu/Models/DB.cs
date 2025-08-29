using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Menu.Models;

public class DB : DbContext
{
    public DB(DbContextOptions options) : base(options) { }
       public DbSet<Product> Products { get; set; }
    

    protected DB()
    {
    }
}


public class Product
{
    [Key]
    public required string Id { get; set; }
    public required string Name { get; set; }
    [Precision(4, 2)]  // set price upper limit: 99.99 --> lecture note slide 58
    public required decimal Price { get; set; }
    public required string? Description { get; set; }
    public required string Category { get; set; }
    public required string ImagePath { get; set; }
    public int Sold { get; set; } = 0;
    public int Stock { get; set; } = 0;
}
