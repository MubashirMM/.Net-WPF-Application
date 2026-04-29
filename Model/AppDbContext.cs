using System.Data.Entity;
using WpfApp1.Model; 

namespace Project.Model
{
    public class AppDbContext : DbContext
    {
        // Pass the name "MyDbConnection" from App.config to the base constructor
        public AppDbContext() : base("name=MyDbConnection")
        {
             
        }

        public DbSet<Users> Users { get; set; }
        public DbSet<Products> Products { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
    }
}