using System.Reflection.Emit;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WebServiceCosmetics.Controllers;
using WebServiceCosmetics.Models;


namespace WebServiceCosmetics.Data;

public class ApplicationDbContext : IdentityDbContext<CustomUser>
{
    public DbSet<Employer> Employees{ get; set; }
    public DbSet<Positions> Positions { get; set; }

    public DbSet<RawMaterialModel> Raw_Materials { get; set; }
    public DbSet<IngredientModel> Ingredient { get; set; }
    public DbSet<ProductModel> Products { get; set; }
    public DbSet<Unit> Units { get; set; }

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<RawMaterialModel>()
              .Property(r => r.Name)
              .HasColumnType("varchar(255)");

        builder.Entity<RawMaterialModel>()
            .Property(r => r.Unit_id)
            .HasColumnType("int");
        builder.Entity<RawMaterialModel>()
               .Property(r => r.Price)
               .HasColumnType("decimal(10,2)");

        builder.Entity<RawMaterialModel>()
            .Property(r => r.Quantity)
            .HasColumnType("decimal(10,2)");
        builder.Entity<RawMaterialModel>().ToTable("Raw_Materials");

        builder.Entity<RawMaterialModel>()
            .HasOne(r => r.Unit)
            .WithMany(u => u.RawMaterials)
            .HasForeignKey(r => r.Unit_id)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Entity<Employer>()
          .HasOne(r => r.Positions)
          .WithMany(u => u.Employer)
          .HasForeignKey(r => r.Position_id)
          .OnDelete(DeleteBehavior.Restrict);
        base.OnModelCreating(builder);
        builder.Entity<RawMaterialModel>().ToTable("Raw_Materials");
        builder.Entity<IngredientModel>().ToTable("Ingredient");
        builder.Entity<ProductModel>().ToTable("Product");
        
    }



}


