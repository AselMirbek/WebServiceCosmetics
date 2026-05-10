using Microsoft.AspNetCore.Mvc.Rendering;
using WebServiceCosmetics.Models;
using WebServiceCosmetics.ViewModel;

namespace WebServiceCosmetics.ViewModel
{
    public class SalesReportVM
    {
        public List<ProductSalesModel> Sales { get; set; }

        public List<SelectListItem> Employees { get; set; }

        public ReportFilterVM Filter { get; set; }

        public decimal TotalAmount =>
            Sales.Sum(x => x.Amount);
    }
}