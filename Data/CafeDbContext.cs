using System.Data.Entity;
using CafeSelfService.Models;

namespace CafeSelfService.Data
{
    public class CafeDbContext : DbContext
    {
        static CafeDbContext()
        {
            Database.SetInitializer<CafeDbContext>(null);
        }

        public CafeDbContext() : base("name=CafeDb")
        {
        }

        public DbSet<Dish> Dishes { get; set; }
        protected override void OnModelCreating(DbModelBuilder builder)
        {
            var dish = builder.Entity<Dish>();
            dish.ToTable("tabledish", "public");
            dish.HasKey(d => d.Id);
            dish.Property(d => d.Id).HasColumnName("id");
            dish.Property(d => d.Name).HasColumnName("name").IsRequired().HasMaxLength(100);
            dish.Property(d => d.Price).HasColumnName("price").HasPrecision(10, 2);
            dish.Property(d => d.Description).HasColumnName("description");
            dish.Property(d => d.Category).HasColumnName("category").IsRequired().HasMaxLength(50);
        }
    }
}
