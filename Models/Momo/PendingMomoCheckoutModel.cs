using Book_Store.Models;

namespace Book_Store.Models.Momo
{
    public class PendingMomoCheckoutModel
    {
        public string MomoOrderId { get; set; } = string.Empty;
        public int UserId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string ShippingAddress { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public List<CartItem> CartItems { get; set; } = new();
        public int? CreatedOrderId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
