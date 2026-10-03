using Reolmarkedet.Core.Interfaces;
using Reolmarkedet.Core.Models;
using Reolmarkedet.Core.Models.enums;
using Reolmarkedet.Core.Services;
using System.Collections.ObjectModel;
using System.Data.Common;
using System.Windows.Threading;

namespace Reolmarkedet.WPF.ViewModels
{
    public class DashboardViewModel : ViewModelBase
    {
        private readonly ObservableCollection<Shelf> _shelves;
        private readonly ObservableCollection<Rental> _rentals;
        private readonly RentalService _rentalService;
        private readonly DispatcherTimer _clockTimer;
        private DateTime _lastRefreshDate;
        public DateTime CurrentDateTime => DateTime.Now;
        private IRepository<Sale> _saleRepository;
        private string _monthlySalesMessage = string.Empty;

        public int AvailableShelfCount { get; private set; }
        public int RentedShelfCount { get; private set; }
        public int EndingShelfCount { get; private set; }
        public int TotalShelfCount => AvailableShelfCount + RentedShelfCount + EndingShelfCount;
        public int ActiveRentingTenantCount { get; private set; }
        public int MonthlySalesCount { get; private set; }
        public decimal MonthlySalesAmount { get; private set; }
        public ObservableCollection<Rental> UpcomingEndings { get; }

        public string MonthlySalesMessage
        {
            get => _monthlySalesMessage;
            private set
            {
                if (_monthlySalesMessage != value)
                {
                    _monthlySalesMessage = value;
                    OnPropertyChanged(nameof(MonthlySalesMessage));
                }
            }
        }

        public DashboardViewModel(
            ObservableCollection<Shelf> shelves,
            ObservableCollection<Rental> rentals,
            IRepository<Sale> saleRepository)
        {
            _shelves = shelves;
            _rentals = rentals;
            _saleRepository = saleRepository;
            UpcomingEndings = new ObservableCollection<Rental>();

            _clockTimer = new DispatcherTimer()
            {
                Interval = TimeSpan.FromSeconds(1),
            };
            // Subribe to the Tick event of the timer to update the CurrentDateTime property every second
            _clockTimer.Tick += (_, _) =>
            {
                DateTime today = DateTime.Today;
                if (today.Date != _lastRefreshDate.Date)
                {
                    Refresh();
                }
                OnPropertyChanged(nameof(CurrentDateTime));
            };

            _rentalService = new RentalService();
            Refresh();
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

        private void RefreshShelfCounts()
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

        private void RefreshUpcomingEndings()
        {
            UpcomingEndings.Clear();
            var today = DateTime.Today;
            var result = new List<Rental>();
            foreach (var shelf in _shelves)
            {
                if (!shelf.IsActive)
                {
                    continue;
                }

                Rental? rental = _rentalService.
                    GetCurrentRentalByShelfAndDate(shelf, today, _rentals);

                if (rental is not null && rental.EndDate.HasValue)
                {
                    result.Add(rental);
                }
            }

            foreach (Rental rental in result
                .OrderBy(r => r.EndDate)
                .ThenBy(r => r.Shelf.ShelfNumber))
            {
                UpcomingEndings.Add(rental);
            }
        }

        private void RefreshActiveRentingTenantCount()
        {
            var today = DateTime.Today;
            ActiveRentingTenantCount = _rentals
                .Where(r => r.Shelf.IsActive &&
                            r.StartDate.Date <= today &&
                            (!r.EndDate.HasValue || r.EndDate.Value.Date >= today))
                .Select(r => r.Tenant.TenantId)
                .Distinct()
                .Count();

            OnPropertyChanged(nameof(ActiveRentingTenantCount));
        }

        private void RefreshMonthlySales()
        {
            try
            {
                var sales = _saleRepository.GetAll();
                var salesInMonth = sales
                   .Where(s => s.SaleDate.Year == DateTime.Today.Year &&
                               s.SaleDate.Month == DateTime.Today.Month);

                MonthlySalesAmount = salesInMonth.Sum(s => s.SalePrice);
                MonthlySalesCount = salesInMonth.Count();
                MonthlySalesMessage = string.Empty;

            }
            catch (DbException)
            {
                MonthlySalesMessage = "Kunne ikke hente månedens salg fra databasen.";
                MonthlySalesCount = 0;
                MonthlySalesAmount = 0;
            }

            OnPropertyChanged(nameof(MonthlySalesCount));
            OnPropertyChanged(nameof(MonthlySalesAmount));
        }

        public void Refresh()
        {
            RefreshShelfCounts();
            RefreshUpcomingEndings();
            RefreshActiveRentingTenantCount();
            RefreshMonthlySales();
            _lastRefreshDate = DateTime.Today;
        }
    }
}
