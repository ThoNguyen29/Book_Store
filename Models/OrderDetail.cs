using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Book_Store.Models
{
    public class OrderDetail
    {
        [Key]
        public int OrderDetailID { get; set; }

        public int OrderID { get; set; }

        public int BookID { get; set; }

        public int Quantity { get; set; }

        public decimal Price { get; set; }

        [ForeignKey("OrderID")]
        public Order? Order { get; set; }

        [ForeignKey("BookID")]
        public Book? Book { get; set; }
    }
}