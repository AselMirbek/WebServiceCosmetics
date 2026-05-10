using Microsoft.AspNetCore.Mvc.Rendering;
using WebServiceCosmetics.Models;
using WebServiceCosmetics.ViewModel;

namespace WebServiceCosmetics.ViewModel
{
    public class ManufacturingReportVM
    {
        public List<ProductManufacturingModel>
            Manufacturing
        { get; set; }

        public List<SelectListItem> Employees { get; set; }

        public ReportFilterVM Filter { get; set; }

        public decimal TotalQuantity =>
            Manufacturing.Sum(x => x.Quantity);
    }
}