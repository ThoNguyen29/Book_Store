using Book_Store.Models;

namespace Book_Store.ViewModel.Products
{
    public class BookCardViewModel
    {
        public Book Book { get; set; } = null!;
        public bool ShowCategory { get; set; } = true;
        public bool ShowStockBadge { get; set; } = true;
        public bool AllowAddToCart { get; set; } = true;
    }
}
