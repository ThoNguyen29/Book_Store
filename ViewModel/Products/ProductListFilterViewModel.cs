using Book_Store.Models;

namespace Book_Store.ViewModel.Products
{
    public class ProductListFilterViewModel
    {
        public List<Category> Categories { get; set; } = new();
        public List<Publisher> Publishers { get; set; } = new();
        public List<Author> Authors { get; set; } = new();

        public string CurrentQ { get; set; } = "";
        public int? CurrentCategoryId { get; set; }
        public decimal? CurrentMinPrice { get; set; }
        public decimal? CurrentMaxPrice { get; set; }
        public int? CurrentPublisherId { get; set; }
        public int? CurrentAuthorId { get; set; }
        public string CurrentSort { get; set; } = "newest";
        public bool HasFilter { get; set; }
    }
}
