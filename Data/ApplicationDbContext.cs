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
    public DbSet<ProductManufacturingModel> Product_Manufacturing { get; set; }

    public DbSet<ProductSalesModel> Product_Sales { get; set; }
    public DbSet<SalaryModel> Salary { get; set; }

    public DbSet<CreditModel> Credit { get; set; }
    public DbSet<PaymentsModel> Payment { get; set; }



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
.Property(r => r.Employees_id)
.HasColumnType("int");
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
        builder.Entity<RawMaterialPurchaseModel>()
  .HasOne(r => r.Employees)
  .WithMany(u => u.Raw_Materials_Purchase)
  .HasForeignKey(r => r.Employees_id)
  .OnDelete(DeleteBehavior.Restrict);
        // Budget

        builder.Entity<BudgetModel>()
           .Property(r => r.Amount)
           .HasColumnType("decimal(10,2)");
        builder.Entity<BudgetModel>().ToTable("Budget");

        //ProductManufacturingModel 

        builder.Entity<ProductManufacturingModel>()
.Property(r => r.Employees_id)
.HasColumnType("int");
        builder.Entity<ProductManufacturingModel>()
.Property(r => r.Product_id)
.HasColumnType("int");
        builder.Entity<ProductManufacturingModel>()
           .Property(r => r.Quantity)
           .HasColumnType("decimal(10,2)");
   
        builder.Entity<ProductManufacturingModel>()
        .Property(r => r.Date)
        .HasColumnType("datetime");

        builder.Entity<ProductManufacturingModel>().ToTable("Product_Manufacturing");

        builder.Entity<ProductManufacturingModel>()
         .HasOne(r => r.ProductModel)
         .WithMany(u => u.Product_Manufacturing)
         .HasForeignKey(r => r.Product_id)
         .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<ProductManufacturingModel>()
  .HasOne(r => r.Employees)
  .WithMany(u => u.Product_Manufacturing)
  .HasForeignKey(r => r.Employees_id)
  .OnDelete(DeleteBehavior.Restrict);




        //ProductSalesModel

        builder.Entity<ProductSalesModel>()
.Property(r => r.Product_id)
.HasColumnType("int");
        builder.Entity<ProductSalesModel>()
.Property(r => r.Employees_id)
.HasColumnType("int");
        builder.Entity<ProductSalesModel>()
           .Property(r => r.Quantity)
           .HasColumnType("decimal(10,2)");
        builder.Entity<ProductSalesModel>()
         .Property(r => r.Amount)
         .HasColumnType("decimal(10,2)");

        builder.Entity<ProductSalesModel>()
        .Property(r => r.Date)
        .HasColumnType("datetime");

        builder.Entity<ProductSalesModel>().ToTable("Product_Sales");

        builder.Entity<ProductSalesModel>()
         .HasOne(r => r.ProductModel)
         .WithMany(u => u.Product_Sales)
         .HasForeignKey(r => r.Product_id)
         .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<ProductSalesModel>()
    .HasOne(r => r.Employees)
    .WithMany(u => u.Product_Sales)
    .HasForeignKey(r => r.Employees_id)
    .OnDelete(DeleteBehavior.Restrict);


        //Employer

        builder.Entity<Employer>()
.Property(r => r.Position_id)
.HasColumnType("int");
      
        builder.Entity<Employer>()
           .Property(r => r.Full_Name)
           .HasColumnType("VARCHAR(255)");
        builder.Entity<Employer>()
           .Property(r => r.Address)
           .HasColumnType("VARCHAR(255)");

        builder.Entity<Employer>()
         .Property(r => r.Phone)
           .HasColumnType("VARCHAR(15)");
        builder.Entity<Employer>()
      .Property(r => r.Salary)
         .HasColumnType("decimal(10,2)");

        builder.Entity<Employer>().ToTable("Employees");

        builder.Entity<Employer>()
         .HasOne(r => r.Positions)
         .WithMany(u => u.Employees)
         .HasForeignKey(r => r.Position_id)
         .OnDelete(DeleteBehavior.Restrict);


        //SalaryModel
  
        builder.Entity<SalaryModel>()
.Property(r => r.Employees_id)
.HasColumnType("int");
        builder.Entity<SalaryModel>()
           .Property(r => r.Year)
           .HasColumnType("int");
        builder.Entity<SalaryModel>()
        .Property(r => r.Month)
        .HasColumnType("int");
        builder.Entity<SalaryModel>()
      .Property(r => r.NumberOfPurchases)
      .HasColumnType("int");
        builder.Entity<SalaryModel>()
      .Property(r => r.NumberOfProductions)
      .HasColumnType("int");
        builder.Entity<SalaryModel>()
      .Property(r => r.NumberOfSales)
      .HasColumnType("int");
        builder.Entity<SalaryModel>()
.Property(r => r.Common)
.HasColumnType("int");

        builder.Entity<SalaryModel>()
         .Property(r => r.SalaryAmount)
         .HasColumnType("decimal(18,2)");
        builder.Entity<SalaryModel>()
       .Property(r => r.Bonus)
       .HasColumnType("decimal(18,2)");
        builder.Entity<SalaryModel>()
       .Property(r => r.General)
       .HasColumnType("decimal(18,2)");
        builder.Entity<SalaryModel>()
        .Property(r => r.Issued)
        .HasColumnType("bit");

        builder.Entity<SalaryModel>().ToTable("Salary");

      
        builder.Entity<SalaryModel>()
    .HasOne(r => r.Employees)
    .WithMany(u => u.Salaries)
    .HasForeignKey(r => r.Employees_id)
    .OnDelete(DeleteBehavior.Restrict);
        //Payment

        builder.Entity<PaymentsModel>()
.Property(r => r.Credit_id)
.HasColumnType("int");
        builder.Entity<PaymentsModel>()
           .Property(r => r.PaymentDate)
           .HasColumnType("datetime");
        builder.Entity<PaymentsModel>()
        .Property(r => r.PaymentAmount)
        .HasColumnType("decimal(18,2)");
        builder.Entity<PaymentsModel>()
     .Property(r => r.Interest)
     .HasColumnType("decimal(18,2)");
        builder.Entity<PaymentsModel>()
     .Property(r => r.TotalAmount)
     .HasColumnType("decimal(18,2)");
        builder.Entity<PaymentsModel>()
     .Property(r => r.RemainingAmount)
     .HasColumnType("decimal(18,2)");


        builder.Entity<PaymentsModel>()
   .Property(r => r.OverdueDays)
   .HasColumnType("int");
        builder.Entity<PaymentsModel>()
  .Property(r => r.Penalty)
  .HasColumnType("decimal(18,2)");
        builder.Entity<PaymentsModel>().ToTable("Payments");

        builder.Entity<PaymentsModel>()
         .HasOne(r => r.Credit)
         .WithMany(u => u.Payment)
         .HasForeignKey(r => r.Credit_id)
         .OnDelete(DeleteBehavior.Restrict);


        //Credit



        builder.Entity<CreditModel>()
           .Property(r => r.StartDate)
           .HasColumnType("datetime");
        builder.Entity<CreditModel>()
        .Property(r => r.Amount)
        .HasColumnType("decimal(18,2)");
        builder.Entity<CreditModel>()
     .Property(r => r.Years)
     .HasColumnType("int");
        builder.Entity<CreditModel>()
     .Property(r => r.AnnualInterestRate)
     .HasColumnType("decimal(18,2)");

        builder.Entity<CreditModel>()
  .Property(r => r.Penalties)
  .HasColumnType("decimal(18,2)");


        builder.Entity<CreditModel>().ToTable("Credit");





        base.OnModelCreating(builder);
        builder.Entity<RawMaterialModel>().ToTable("Raw_Materials");
        builder.Entity<IngredientModel>().ToTable("Ingredient");
        builder.Entity<ProductModel>().ToTable("Product");
        builder.Entity<RawMaterialPurchaseModel>().ToTable("Raw_Materials_Purchase");
        builder.Entity<BudgetModel>().ToTable("Budget");
        builder.Entity<ProductManufacturingModel>().ToTable("Product_Manufacturing");
        builder.Entity<ProductSalesModel>().ToTable("Product_Sales");
        builder.Entity<Employer>().ToTable("Employees");
        builder.Entity<SalaryModel>().ToTable("Salary");




    }



}


