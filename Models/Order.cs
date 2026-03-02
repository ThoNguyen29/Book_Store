using System;
using System.Collections.Generic;

namespace Book_Store.Models
{
    public class Order
    {
        public int OrderID { get; set; }

        public string CustomerName { get; set; } = string.Empty;

        public string ShippingAddress { get; set; } = string.Empty;

        public string PaymentMethod { get; set; } = string.Empty;

        public DateTime OrderDate { get; set; }

        public decimal TotalAmount { get; set; }

        public OrderStatus Status { get; set; }

        public List<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
    }
}