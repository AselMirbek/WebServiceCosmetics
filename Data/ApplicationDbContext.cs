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
    public DbSet<ProductModel> Product { get; set; }
    public DbSet<Unit> Units { get; set; }
    public DbSet<BudgetModel> Budget { get; set; }
    public DbSet<RawMaterialPurchaseModel> Raw_Materials_Purchase { get; set; }

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
        //ProductModel
        builder.Entity<ProductModel>()
            .Property(r => r.Name)
            .HasColumnType("varchar(255)");

        builder.Entity<ProductModel>()
            .Property(r => r.Unit_id)
            .HasColumnType("int");
        builder.Entity<ProductModel>()
               .Property(r => r.Price)
               .HasColumnType("decimal(10,2)");

        builder.Entity<ProductModel>()
            .Property(r => r.Quantity)
            .HasColumnType("decimal(10,2)");
        builder.Entity<ProductModel>().ToTable("Product");

        builder.Entity<ProductModel>()
            .HasOne(r => r.Unit)
            .WithMany(u => u.Product)
            .HasForeignKey(r => r.Unit_id)
            .OnDelete(DeleteBehavior.Restrict);

        //IngredientModel
        builder.Entity<IngredientModel>()
       .Property(r => r.Product_id)
       .HasColumnType("int");
        builder.Entity<IngredientModel>()
          .Property(r => r.Raw_Material_id)
          .HasColumnType("int");

        builder.Entity<IngredientModel>()
            .Property(r => r.Quantity)
            .HasColumnType("decimal(10,2)");
        builder.Entity<IngredientModel>().ToTable("Ingredient");


        builder.Entity<IngredientModel>()
            .HasOne(r => r.RawMaterialModel)
            .WithMany(u => u.ingredient)
            .HasForeignKey(r => r.Raw_Material_id)
            .OnDelete(DeleteBehavior.Restrict);




        // RawMaterialPurchaseModel

        builder.Entity<RawMaterialPurchaseModel>()
         .Property(r => r.Raw_Material_id)
         .HasColumnType("int");
        builder.Entity<RawMaterialPurchaseModel>()
           .Property(r => r.Quantity)
           .HasColumnType("decimal(10,2)");
        builder.Entity<RawMaterialPurchaseModel>()
          .Property(r => r.Amount)
          .HasColumnType("decimal(10,2)");
        builder.Entity<RawMaterialPurchaseModel>()
        .Property(r => r.Date)
        .HasColumnType("datetime");

        builder.Entity<RawMaterialPurchaseModel>().ToTable("Raw_Materials_Purchase");
        builder.Entity<RawMaterialPurchaseModel>()
            .HasOne(r => r.RawMaterialModel)
            .WithMany(u => u.Raw_Materials_Purchase)
            .HasForeignKey(r => r.Raw_Material_id)
            .OnDelete(DeleteBehavior.Restrict);
        // Budget

           builder.Entity<BudgetModel>()
           .Property(r => r.Amount)
           .HasColumnType("decimal(10,2)");
        builder.Entity<BudgetModel>().ToTable("Budget");



        builder.Entity<Employer>()
          .HasOne(r => r.Positions)
          .WithMany(u => u.Employer)
          .HasForeignKey(r => r.Position_id)
          .OnDelete(DeleteBehavior.Restrict);
        base.OnModelCreating(builder);
        builder.Entity<RawMaterialModel>().ToTable("Raw_Materials");
        builder.Entity<IngredientModel>().ToTable("Ingredient");
        builder.Entity<ProductModel>().ToTable("Product");
        builder.Entity<RawMaterialPurchaseModel>().ToTable("Raw_Materials_Purchase");
        builder.Entity<BudgetModel>().ToTable("Budget");


    }



}


