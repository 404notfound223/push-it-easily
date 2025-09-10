using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Menu.Models;

public static class IdGenerator
{
    // Generates a new OrderId like "ORD00000123"
    public static async Task<string> GenerateOrderId(DB context)
    {
        var lastOrder = await context.Orders
            .OrderByDescending(o => o.OrderId)
            .FirstOrDefaultAsync();

        int lastNumber = 0;
        if (lastOrder != null && lastOrder.OrderId.Length > 3)
        {
            int.TryParse(lastOrder.OrderId.Substring(3), out lastNumber);
        }
        return "ORD" + (lastNumber + 1).ToString("D8");
    }

    // Generates a new OrderDetailId like "ODT00000123"
    public static async Task<string> GenerateOrderDetailId(DB context)
    {
        var lastDetail = await context.OrderDetails
            .OrderByDescending(od => od.OrderDetailId)
            .FirstOrDefaultAsync();

        int lastNumber = 0;
        if (lastDetail != null && lastDetail.OrderDetailId.Length > 3)
        {
            int.TryParse(lastDetail.OrderDetailId.Substring(3), out lastNumber);
        }
        return "ODT" + (lastNumber + 1).ToString("D8");
    }
}