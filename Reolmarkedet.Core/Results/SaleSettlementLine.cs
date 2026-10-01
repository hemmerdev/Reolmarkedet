using Reolmarkedet.Core.Models;

namespace Reolmarkedet.Core.Results
{
    public class SaleSettlementLine(Sale sale, Item item, Rental rental, decimal commission)
    {
        public Sale Sale { get; } = sale;
        public Item Item { get; } = item;
        public Rental Rental { get; } = rental;
        public decimal Commission { get; } = commission;
    }
}
