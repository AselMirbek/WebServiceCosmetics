// ViewModel/CreditReportVM.cs

using WebServiceCosmetics.Models;

namespace WebServiceCosmetics.ViewModel
{
    public class CreditReportVM
    {
        // =====================================================
        // CREDIT LIST
        // =====================================================

        public List<CreditModel> Credits { get; set; }
            = new();

        // =====================================================
        // SELECTED CREDIT DETAILS
        // =====================================================

        public CreditModel? SelectedCredit { get; set; }

        // =====================================================
        // PAYMENT SCHEDULE
        // =====================================================

        public List<PaymentsModel> Payments { get; set; }
            = new();

        // =====================================================
        // FILTER
        // =====================================================

        public int? CreditId { get; set; }

        // =====================================================
        // KPI
        // =====================================================

        // Общая сумма кредитов

        public decimal TotalCredits =>
            Credits.Sum(x => x.Amount);

        // Остаток долга

        public decimal TotalRemainingAmount =>
            Credits.Sum(x => x.RemainingAmount ?? 0);

        // Общие штрафы

        public decimal TotalPenalties =>
            Credits.Sum(x => x.Penalties ?? 0);

        // Общая сумма процентов

        public decimal TotalInterest =>
            Payments.Sum(x => x.Interest);

        // Общая сумма платежей

        public decimal TotalPayments =>
            Payments.Sum(x => x.PaymentAmount);

        // Общая финальная сумма

        public decimal TotalFinalAmount =>
            Payments.Sum(x => x.FinalAmount);

        // Количество кредитов

        public int TotalCreditsCount =>
            Credits.Count;

        // Количество просрочек

        public int TotalOverdues =>
            Payments.Count(x =>
                (x.OverdueDays ?? 0) > 0);

        // Количество оплаченных платежей

        public int PaidPayments =>
            Payments.Count(x => x.IsPaid);

        // Количество неоплаченных платежей

        public int UnpaidPayments =>
            Payments.Count(x => !x.IsPaid);

        // =====================================================
        // ANALYTICS
        // =====================================================

        // Самый большой кредит

        public decimal MaxCreditAmount =>
            Credits.Any()
                ? Credits.Max(x => x.Amount)
                : 0;

        // Средний размер кредита

        public decimal AverageCreditAmount =>
            Credits.Any()
                ? Credits.Average(x => x.Amount)
                : 0;

        // Средний процент

        public decimal AverageInterestRate =>
            Credits.Any()
                ? Credits.Average(x =>
                    x.AnnualInterestRate)
                : 0;
    }
}