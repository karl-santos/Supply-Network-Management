using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace SupplyNetworkManagement.Data
{
    /*public class AppDbContext
    {
    }*/

    public class Vendor
    {
        [Key]
        public int VendorId { get; set; }
        public string VendorName { get; set; } = string.Empty;

        public string PhoneNumber { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

    }
        public class MyDbContext(DbContextOptions<MyDbContext> options) : DbContext(options)
        {
            public DbSet<Vendor> Vendors => Set<Vendor>();
        }
    }

