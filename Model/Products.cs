using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.Security.Policy;
using Project.Model;
using System.ComponentModel.DataAnnotations.Schema;

namespace WpfApp1.Model
{
    public class Products
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public decimal ProductPrice { get; set; }
        public decimal ProductStock { get; set; }
        public int UserId { get; set; }
        [ForeignKey("UserId")]
        public Users Users { get; set; }



    }
}
