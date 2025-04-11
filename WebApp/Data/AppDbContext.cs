using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WebApp.Models;

namespace WebApp.Data
{
    public class AppDbContext : IdentityDbContext<ApplicationUser>
    {


        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        // DbSet'ler
        public DbSet<Idea> Ideas { get; set; }
        public DbSet<Vote> Votes { get; set; }


        // Lazy Loading Proxies etkinleştirildi
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseLazyLoadingProxies();
            }
        }

    }
}
