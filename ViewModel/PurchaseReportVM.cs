using WebServiceCosmetics.Models;
using WebServiceCosmetics.ViewModel;

namespace WebServiceCosmetics.ViewModel
{
    public class PurchaseReportVM
    {
        public List<RawMaterialPurchaseModel>
            Purchases
        { get; set; }

        public ReportFilterVM Filter { get; set; }

        public decimal TotalAmount =>
            Purchases.Sum(x => x.Amount);
    }
}