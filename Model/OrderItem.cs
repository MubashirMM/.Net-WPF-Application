using WpfApp1.Model;

namespace Project.Model
{
    public class OrderItem
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public decimal Subtotal { get; set; }

        // Navigation properties
        public virtual Order Order { get; set; }
        public virtual Products Product { get; set; }
    }
}