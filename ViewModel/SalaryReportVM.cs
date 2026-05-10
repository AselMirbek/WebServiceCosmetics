using Microsoft.AspNetCore.Mvc.Rendering;
using WebServiceCosmetics.Models;

namespace WebServiceCosmetics.ViewModel
{
   
        public class SalaryReportVM
        {
            public List<SalaryModel> Salaries
            { get; set; } = new();

            public SalaryFilterVM Filter
            { get; set; } = new();

            // TOTALS

            public decimal TotalSalary =>
                Salaries.Sum(x =>
                    x.SalaryAmount);

            public decimal TotalBonus =>
                Salaries.Sum(x =>
                    x.Bonus ?? 0);

            public decimal TotalGeneral =>
                Salaries.Sum(x =>
                    x.General ?? 0);

            public decimal TotalIssued =>
                Salaries
                    .Where(x => x.Issued)
                    .Sum(x => x.General ?? 0);

            public decimal TotalNotIssued =>
                Salaries
                    .Where(x => !x.Issued)
                    .Sum(x => x.General ?? 0);

            // OPERATIONS

            public int TotalPurchases =>
                Salaries.Sum(x =>
                    x.NumberOfPurchases ?? 0);

            public int TotalManufacturing =>
                Salaries.Sum(x =>
                    x.NumberOfProductions ?? 0);

            public int TotalSales =>
                Salaries.Sum(x =>
                    x.NumberOfSales ?? 0);

            public int TotalOperations =>
                Salaries.Sum(x =>
                    (x.NumberOfPurchases ?? 0)
                    + (x.NumberOfProductions ?? 0)
                    + (x.NumberOfSales ?? 0));
        public int? Employees_id
        { get; set; }
        public List<SelectListItem> Employees
        { get; set; } = new();
    

}
}

public class SalaryFilterVM
{
    public int? YearFrom
    { get; set; }

    public int? MonthFrom
    { get; set; }

    public int? YearTo
    { get; set; }

    public int? MonthTo
    { get; set; }

    public int? Employees_id
    { get; set; }

    public bool? Issued
    { get; set; }

    public List<SelectListItem> Employees
    { get; set; } = new();
}