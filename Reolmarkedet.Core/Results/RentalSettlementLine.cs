using Reolmarkedet.Core.Models;

namespace Reolmarkedet.Core.Results
{
    public class RentalSettlementLine(Rental rental, DateTime periodStart, DateTime periodEnd, decimal amount)
    {
        public Rental Rental { get; } = rental;
        public DateTime PeriodStart { get; } = periodStart;
        public DateTime PeriodEnd { get; } = periodEnd;
        public decimal Amount { get; } = amount;
    }
}
