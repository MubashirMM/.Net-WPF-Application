using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Project.Model
{
    public enum UserRole
    {
        SuperAdmin = 1,
        Admin = 2,
        User = 3
    }
   
        public class Users
        {
            [Key]
            [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // Database creates this automatically
            public int Id { get; set; }

            [Required]
            [StringLength(50)]
            public string Name { get; set; }

            [Required]
            [EmailAddress]
            [StringLength(100)]
            public string Email { get; set; }

            [Required]
            [StringLength(255)]
            public string Password { get; set; }

            [Required]
            public UserRole Role { get; set; } // The Enum we created
            public DateTime CreatedDate { get; set; }

            // Logic for your UI display
            [NotMapped]
            public string CreatedTimeDisplay => CreatedDate.ToString("hh:mm tt");

            [NotMapped]
            public string CreatedDateDisplay => CreatedDate.ToShortDateString();
        }
 }