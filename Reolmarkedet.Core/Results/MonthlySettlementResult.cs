using Reolmarkedet.Core.Models;

namespace Reolmarkedet.Core.Results
{
    public class MonthlySettlementResult(
        Tenant tenant, int year, int month, decimal totalSales, decimal commission, decimal rent,
        IReadOnlyList<SaleSettlementLine> saleLines,
        IReadOnlyList<RentalSettlementLine> rentalLines)
    {
        public Tenant Tenant { get; } = tenant;
        public int Year { get; } = year;
        public int Month { get; } = month;
        public decimal TotalSales { get; } = totalSales;
        public decimal Commission { get; } = commission;
        public decimal Rent { get; } = rent;
        public IReadOnlyList<SaleSettlementLine> SaleLines { get; } = saleLines;
        public IReadOnlyList<RentalSettlementLine> RentalLines { get; } = rentalLines;
        public decimal Balance => TotalSales - Commission - Rent;
    }
}
