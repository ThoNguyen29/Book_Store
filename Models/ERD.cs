using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace Book_Store.Models
{
    // 1. Users
    [Table("Users", Schema = "dbo")]
    public class User
    {
        public User()
        {
            Orders = new HashSet<Order>();
        }

        [Key]
        public int ID { get; set; }

        [Required]
        [StringLength(255)]
        public string Email { get; set; } = string.Empty;

        // ĐÃ ĐỔI TÊN: Từ HashPassword thành PasswordHash để khớp với AccountController
        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        // THÊM DÒNG NÀY: Để hết lỗi IsActive trong AccountController
        public bool IsActive { get; set; } = true;

        [StringLength(255)]
        public string FullName { get; set; } = string.Empty;

        public string? Address { get; set; }

        [StringLength(50)]
        public string? Role { get; set; }

        public DateTime? CreatedAt { get; set; } = DateTime.Now;
        public string Username { get; set; } = string.Empty; 

        // Lưu ý: Password và ConfirmPassword thường dùng ở ViewModel, 
        // nhưng tôi vẫn giữ lại ở đây theo code của bạn để tránh lỗi logic khác.
        public string Password { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;

        // Navigation properties
        public virtual ICollection<Order> Orders { get; set; }
    }

    // 2. Categories
    [Table("Categories", Schema = "dbo")]
    public class Category
    {
        public Category()
        {
            Books = new HashSet<Book>();
        }

        [Key]
        public int CategoryID { get; set; }

        [Required]
        [StringLength(255)]
        public string Name { get; set; } = string.Empty;

        [StringLength(255)]
        public string? Slug { get; set; }

        // Navigation properties
        public virtual ICollection<Book> Books { get; set; }
    }

    // 3. Publishers
    [Table("Publishers", Schema = "dbo")]
    public class Publisher
    {
        public Publisher()
        {
            Books = new HashSet<Book>();
        }

        [Key]
        public int PublisherID { get; set; }

        [Required]
        [StringLength(255)]
        public string Name { get; set; } = string.Empty;

        // Navigation properties
        public virtual ICollection<Book> Books { get; set; }
    }

    // 4. Authors
    [Table("Authors", Schema = "dbo")] // Đã sửa tên bảng cho đúng
    public class Author
    {
        public Author()
        {
            BookAuthors = new HashSet<BookAuthor>();
        }

        [Key]
        public int AuthorID { get; set; }

        [Required]
        [StringLength(255)]
        public string Name { get; set; } = string.Empty;

        // Navigation properties
        public virtual ICollection<BookAuthor> BookAuthors { get; set; }
    }

    // 5. Books
    [Table("Books", Schema = "dbo")]
    public class Book
    {
        public Book()
        {
            OrderDetails = new HashSet<OrderDetail>();
            BookAuthors = new HashSet<BookAuthor>();
        }

        [Key]
        public int BookID { get; set; }

        [Required]
        [StringLength(255)]
        public string Title { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        public int Stock { get; set; }

        public string Description { get; set; } = string.Empty;

        public int? CategoryID { get; set; }
        [ForeignKey("CategoryID")]
        public virtual Category? Category { get; set; }

        public int? PublisherID { get; set; }
        [ForeignKey("PublisherID")]
        public virtual Publisher? Publisher { get; set; }

        public DateTime? CreatedAt { get; set; } = DateTime.Now;

        public virtual ICollection<OrderDetail> OrderDetails { get; set; }
        public virtual ICollection<BookAuthor> BookAuthors { get; set; }
    }

    // 6. BookAuthors (Bảng trung gian)
    [Table("BookAuthors", Schema = "dbo")] // Đã sửa lại tên bảng trung gian
    public class BookAuthor
    {
        public int BookID { get; set; }
        [ForeignKey("BookID")]
        public virtual Book Book { get; set; } = default!;

        public int AuthorID { get; set; }
        [ForeignKey("AuthorID")]
        public virtual Author Author { get; set; } = default!;
    }

    // 7. Orders
    [Table("Orders", Schema = "dbo")]
    public class Order
    {
        public Order()
        {
            OrderDetails = new HashSet<OrderDetail>();
            Payments = new HashSet<Payment>();
        }

        [Key]
        public int OrderID { get; set; }

        public int UserID { get; set; }
        [ForeignKey("UserID")]
        public virtual User User { get; set; } = default!;

        public DateTime OrderDate { get; set; } = DateTime.Now;

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        [StringLength(50)]
        public string Status { get; set; } = "Pending";

        public virtual ICollection<OrderDetail> OrderDetails { get; set; }
        public virtual ICollection<Payment> Payments { get; set; }
    }

    // 8. OrderDetails
    [Table("OrderDetails", Schema = "dbo")]
    public class OrderDetail
    {
        [Key]
        public int OrderDetailID { get; set; }

        public int OrderID { get; set; }
        [ForeignKey("OrderID")]
        public virtual Order Order { get; set; } = default!;

        public int BookID { get; set; }
        [ForeignKey("BookID")]
        public virtual Book Book { get; set; } = default!;

        public int Quantity { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }
    }

    // 9. Payments
    [Table("Payments", Schema = "dbo")]
    public class Payment
    {
        [Key]
        public int PaymentID { get; set; }

        public int OrderID { get; set; }
        [ForeignKey("OrderID")]
        public virtual Order Order { get; set; } = default!;

        [StringLength(50)]
        public string Method { get; set; } = string.Empty;

        [StringLength(50)]
        public string Status { get; set; } = "Unpaid";

        public DateTime? PaidAt { get; set; }
    }
}