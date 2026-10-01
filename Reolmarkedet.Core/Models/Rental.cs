using Reolmarkedet.Core.Models.enums;

namespace Reolmarkedet.Core.Models
{
    public class Rental
    {
        public int RentalId { get; set; }
        public Tenant Tenant { get; set; }
        public Shelf Shelf { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime? TerminationNoticeDate { get; set; }
        public decimal MonthlyRent { get; set; }
        public PaymentMethod? InitialPaymentMethod { get; set; }
        public bool IsCustomPrice { get; set; }

        public Rental(Tenant tenant, Shelf shelf)
        {
            Tenant = tenant;
            Shelf = shelf;
        }
    }
}
