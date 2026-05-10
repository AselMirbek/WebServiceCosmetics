// ViewModels/ReportDashboardVM.cs

using Microsoft.AspNetCore.Mvc.Rendering;
using WebServiceCosmetics.Models;

namespace WebServiceCosmetics.ViewModel
{
    // ViewModels/ReportDashboardVM.cs


        public class ReportFilterVM
    {
            public DateTime? DateFrom { get; set; }

            public DateTime? DateTo { get; set; }

            public int? Employees_id { get; set; }
        }

        // =========================================
        // DASHBOARD
        // =========================================

        public class ReportDashboardVM
        {
            public decimal TotalRevenue { get; set; }

            public int TotalSales { get; set; }

            public int TotalManufacturing { get; set; }

            public int TotalPurchases { get; set; }

            public decimal TotalSalary { get; set; }

            public decimal TotalCredits { get; set; }
        }
    }
   