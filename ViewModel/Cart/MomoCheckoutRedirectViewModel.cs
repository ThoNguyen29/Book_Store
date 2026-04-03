namespace Book_Store.ViewModel.Cart
{
    public class MomoCheckoutRedirectViewModel
    {
        public string Address { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string OrderInfo { get; set; } = "Thanh toán đơn hàng";
    }
}
