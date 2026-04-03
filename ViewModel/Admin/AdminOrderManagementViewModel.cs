using System;
using System.Collections.Generic;
using Book_Store.Models;

namespace Book_Store.ViewModel.Admin
{
    public class AdminUserManagementViewModel
    {
        public List<AdminUserItemViewModel> Users { get; set; } = new();
    }

    public class AdminUserItemViewModel
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public int OrderCount { get; set; }
        public DateTime? LastOrderDate { get; set; }
    }

    public class AdminUserOrdersViewModel
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public List<AdminOrderSummaryItemViewModel> Orders { get; set; } = new();
    }

    public class AdminOrderSummaryItemViewModel
    {
        public int OrderId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public OrderStatus Status { get; set; }
        public int ItemCount { get; set; }
    }

    public class AdminOrderDetailsViewModel
    {
        public int? UserId { get; set; }
        public int OrderId { get; set; }
        public DateTime OrderDate { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public string ShippingAddress { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = string.Empty;
        public OrderStatus Status { get; set; }
        public OrderStatus? NextStatus { get; set; }
        public bool CanCancel { get; set; }
        public decimal TotalAmount { get; set; }
        public List<AdminOrderProductItemViewModel> Items { get; set; } = new();
    }

    public class AdminOrderProductItemViewModel
    {
        public int BookId { get; set; }
        public string BookTitle { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LineTotal { get; set; }
    }
}
