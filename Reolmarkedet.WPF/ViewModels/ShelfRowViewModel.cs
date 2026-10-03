using Reolmarkedet.Core.Models;
using Reolmarkedet.Core.Models.enums;

namespace Reolmarkedet.WPF.ViewModels
{
    public class ShelfRowViewModel : ViewModelBase
    {
        public Shelf Shelf { get; }
        public Rental? CurrentRental { get; }
        public ShelfStatus ShelfStatus { get; }
        public DateTime? TerminationEndDate =>
            ShelfStatus == ShelfStatus.TerminationPending ? CurrentRental?.EndDate : null;
        public int ShelfNumber => Shelf.ShelfNumber;
        public string LocationText
        {
            get
            {
                if (Shelf.RowLabel is null || Shelf.PositionInRow is null)
                {
                    return "Ikke angivet";
                }
                else
                {
                    return $"{Shelf.RowLabel} · {Shelf.PositionInRow}";
                }
            }
        }
        public string CurrentTenantName => CurrentRental?.Tenant.Name ?? "-";

        public ShelfRowViewModel(Shelf shelf, Rental? currentRental, ShelfStatus shelfStatus)
        {
            Shelf = shelf;
            CurrentRental = currentRental;
            ShelfStatus = shelfStatus;
        }
    }
}
