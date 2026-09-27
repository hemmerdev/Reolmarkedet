using Reolmarkedet.Core.Models;
using Reolmarkedet.WPF.ViewModels.enums;

namespace Reolmarkedet.WPF.ViewModels
{
    public class RentalRowViewModel : ViewModelBase
    {
        public Rental Rental { get; }
        public int RentalId => Rental.RentalId;
        public string TenantName => Rental.Tenant.Name;
        public int ShelfNumber => Rental.Shelf.ShelfNumber;
        public DateTime StartDate => Rental.StartDate;
        public DateTime? EndDate => Rental.EndDate;
        public decimal MonthlyRent { get; }
        public RentalStatus Status
        {
            get
            {
                if (Rental.EndDate.HasValue &&
                    Rental.EndDate.Value.Date < DateTime.Today.Date)
                {
                    return RentalStatus.Historical;

                }
                else if (Rental.StartDate.Date > DateTime.Today.Date)
                {
                    return RentalStatus.Upcoming;
                }
                else
                {
                    return RentalStatus.Active;
                }
            }
        }

        public RentalRowViewModel(Rental rental, decimal monthlyRent)
        {
            Rental = rental;
            MonthlyRent = monthlyRent;
        }
    }
}
