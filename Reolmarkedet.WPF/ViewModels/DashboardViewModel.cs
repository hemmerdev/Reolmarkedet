using Reolmarkedet.Core.Models;
using Reolmarkedet.Core.Models.enums;
using Reolmarkedet.Core.Services;
using System.Collections.ObjectModel;
using System.Windows.Threading;

namespace Reolmarkedet.WPF.ViewModels
{
    public class DashboardViewModel : ViewModelBase
    {
        private readonly ObservableCollection<Shelf> _shelves;
        private readonly ObservableCollection<Rental> _rentals;
        private readonly RentalService _rentalService;
        private readonly DispatcherTimer _clockTimer;
        public DateTime CurrentDateTime => DateTime.Now;
        public int AvailableShelfCount { get; private set; }
        public int RentedShelfCount { get; private set; }
        public int EndingShelfCount { get; private set; }
        public int TotalShelfCount => AvailableShelfCount + RentedShelfCount + EndingShelfCount;

        public DashboardViewModel(
            ObservableCollection<Shelf> shelves,
            ObservableCollection<Rental> rentals)
        {
            _shelves = shelves;
            _rentals = rentals;
            _clockTimer = new DispatcherTimer()
            {
                Interval = TimeSpan.FromSeconds(1),
            };
            // Subribe to the Tick event of the timer to update the CurrentDateTime property every second
            _clockTimer.Tick += (_, _) => OnPropertyChanged(nameof(CurrentDateTime));

            _rentalService = new RentalService();
            RefreshShelfCounts();
        }

        public void StartClock()
        {
            if (!_clockTimer.IsEnabled)
            {
                _clockTimer.Start();
                OnPropertyChanged(nameof(CurrentDateTime));
            }
        }

        public void StopClock()
        {
            if (_clockTimer.IsEnabled)
            {
                _clockTimer.Stop();
            }
        }

        public void RefreshShelfCounts()
        {
            int availableCount = 0;
            int rentedCount = 0;
            int endingCount = 0;
            DateTime today = DateTime.Today;
            foreach (var shelf in _shelves)
            {
                if (!shelf.IsActive)
                {
                    continue;
                }

                switch (_rentalService.GetShelfStatus(shelf, today, _rentals))
                {
                    case ShelfStatus.Available:
                        availableCount++;
                        break;
                    case ShelfStatus.Rented:
                        rentedCount++;
                        break;
                    case ShelfStatus.TerminationPending:
                        endingCount++;
                        break;
                }
            }

            AvailableShelfCount = availableCount;
            RentedShelfCount = rentedCount;
            EndingShelfCount = endingCount;

            OnPropertyChanged(nameof(AvailableShelfCount));
            OnPropertyChanged(nameof(RentedShelfCount));
            OnPropertyChanged(nameof(EndingShelfCount));
            OnPropertyChanged(nameof(TotalShelfCount));
        }
    }
}
