using Reolmarkedet.Core.Models;
using Reolmarkedet.Core.Results;

//SettlementService
//- Find sales for a tenant/month
//- Calculate total sales
//- Calculate 10% commission
//- Calculate rental cost
//- Calculate final payout/debt


namespace Reolmarkedet.Core.Services
{
    public class SettlementService
    {
        public List<Sale> GetSalesForTenantAndMonth(
            Tenant tenant,
            int requestedYear,
            int requestedMonth,
            IEnumerable<Sale> sales,
            IEnumerable<Item> items,
            IEnumerable<Rental> rentals)
        {
            List<Sale> result = new List<Sale>();

            // Filter sales by requested year and month
            var monthSales = sales.Where(y => y.SaleDate.Year == requestedYear &&
                              y.SaleDate.Month == requestedMonth);

            foreach (var sale in monthSales)
            {
                // Find corresponding item for each sale
                var item = items.FirstOrDefault(i => i.ItemId == sale.ItemId)
                    ?? throw new InvalidOperationException(
                        $"Varenummer '{sale.ItemId}' blev ikke fundet.");

                // Find corresponding rental for each item
                var rental = rentals.FirstOrDefault(r => r.RentalId == item.RentalId)
                    ?? throw new InvalidOperationException(
                        $"Lejemål for varenummer '{item.ItemId}' blev ikke fundet.");

                if (rental.Tenant.TenantId == tenant.TenantId)
                {
                    result.Add(sale);
                }
            }

            return result;
        }

        public List<SaleSettlementLine> GetSaleLinesForTenantAndMonth(
            Tenant tenant,
            int requestedYear,
            int requestedMonth,
            IEnumerable<Sale> sales,
            IEnumerable<Item> items,
            IEnumerable<Rental> rentals)
        {
            List<SaleSettlementLine> result = new List<SaleSettlementLine>();
            var tenantSales = GetSalesForTenantAndMonth(tenant, requestedYear, requestedMonth, sales, items, rentals);
            foreach (var sale in tenantSales)
            {
                var item = items.FirstOrDefault(i => i.ItemId == sale.ItemId)
                    ?? throw new InvalidOperationException(
                        $"Varenummer '{sale.ItemId}' blev ikke fundet.");

                var rental = rentals.FirstOrDefault(r => r.RentalId == item.RentalId)
                    ?? throw new InvalidOperationException(
                        $"Lejemål for varenummer '{item.ItemId}' blev ikke fundet.");

                result.Add(new SaleSettlementLine(sale, item, rental, CalculateCommissionForSale(sale)));
            }
            return result;
        }

        public decimal CalculateCommission(IEnumerable<Sale> sales)
        {
            decimal commission =
                sales.Sum(s => CalculateCommissionForSale(s));

            return commission;
        }

        private decimal CalculateCommissionForSale(Sale sale)
        {
            return Math.Round(sale.SalePrice * 0.10m, 2, MidpointRounding.AwayFromZero);
        }

        public decimal CalculateRentalChargeForMonth(
            Rental rental,
            int year,
            int month,
            IEnumerable<Rental> rentals)
        {
            DateTime monthStart = new(year, month, 1);
            DateTime monthEnd = monthStart.AddMonths(1).AddDays(-1);

            if (year == rental.StartDate.Year &&
                month == rental.StartDate.Month)
            {
                return 0m; // First period, already charged
            }
            if (rental.StartDate.Date > monthEnd ||
                (rental.EndDate.HasValue && rental.EndDate.Value.Date < monthStart))
            {
                return 0m;
            }

            DateTime firstDay = rental.StartDate.Date > monthStart
                ? rental.StartDate.Date
                : monthStart;

            DateTime lastDay = rental.EndDate.HasValue &&
                               rental.EndDate.Value.Date < monthEnd
                ? rental.EndDate.Value.Date
                : monthEnd;

            int daysInMonth = DateTime.DaysInMonth(year, month);
            decimal total = 0m;
            RentalService rentalService = new();

            for (DateTime day = firstDay; day <= lastDay; day = day.AddDays(1))
            {
                decimal monthlyCharge =
                    rentalService.GetMonthlyRentForDate(rental, day, rentals);

                total += monthlyCharge / daysInMonth; // Daily charge
            }

            return Math.Round(total, 2, MidpointRounding.AwayFromZero);
        }

        public MonthlySettlementResult CalculateForTenantAndMonth(
            Tenant tenant,
            int year,
            int month,
            IEnumerable<Sale> sales,
            IEnumerable<Item> items,
            IEnumerable<Rental> rentals)
        {
            var saleLines =
                GetSaleLinesForTenantAndMonth(tenant, year, month, sales, items, rentals);

            decimal totalSales = saleLines.Sum(sl => sl.Sale.SalePrice);
            decimal commission = saleLines.Sum(sl => sl.Commission);

            // use next month rental charge for the current month settlement like explained in the case
            DateTime nextMonth = new DateTime(year, month, 1).AddMonths(1);
            var rentalCharge = rentals
                .Where(r => r.Tenant.TenantId == tenant.TenantId)
                .Sum(r => CalculateRentalChargeForMonth(r, nextMonth.Year, nextMonth.Month, rentals));

            return new MonthlySettlementResult(
                tenant, year, month, totalSales, commission, rentalCharge, saleLines);
        }

        public List<MonthlySettlementResult> CalculateForMonth(
            int year,
            int month,
            IEnumerable<Tenant> tenants,
            IEnumerable<Sale> sales,
            IEnumerable<Item> items,
            IEnumerable<Rental> rentals)
        {
            DateTime firstDay = new DateTime(year, month, 1);
            DateTime lastDay = firstDay.AddMonths(1).AddDays(-1);
            var results = new List<MonthlySettlementResult>();

            foreach (var tenant in tenants)
            {
                var tenantRentals =
                    rentals.Where(r => r.Tenant.TenantId == tenant.TenantId &&
                                 r.StartDate.Date <= lastDay && // Rental has started by the last day of the month
                                 (!r.EndDate.HasValue || r.EndDate.Value.Date >= firstDay)); // Rental has no end date or has not ended before the first day of the month

                bool hasRentalActivity = tenantRentals.Any();
                bool hasSales = GetSalesForTenantAndMonth(tenant, year, month, sales, items, rentals).Any();

                // Show tenants with either rental or sales activity in the month, or both. Exclude tenants with no activity.
                if (hasRentalActivity || hasSales)
                {
                    results.Add(
                        CalculateForTenantAndMonth(tenant, year, month, sales, items, rentals));
                }
            }

            return results;
        }
    }
}
