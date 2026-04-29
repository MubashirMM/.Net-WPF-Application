using System;
using System.Collections.Generic;

namespace Project.Model
{
    public class Order
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } // Paid, Pending, Cancelled
        public string BillNumber { get; set; }

        // Navigation property
        public virtual ICollection<OrderItem> OrderItems { get; set; }
    }
}