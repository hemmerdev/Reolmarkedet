using Reolmarkedet.Core.Interfaces;
using Reolmarkedet.Core.Models;
using Reolmarkedet.WPF.Commands;
using System.Collections.ObjectModel;

namespace Reolmarkedet.WPF.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private ViewModelBase? _currentViewModel;
        public ViewModelBase? CurrentViewModel
        {
            get { return _currentViewModel; }
            private set
            {
                if (_currentViewModel != value)
                {
                    _currentViewModel = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(ActivePage));
                }
            }
        }

        // This property is used to determine which navigation button should be highlighted in the UI.
        public NavigationPage? ActivePage => CurrentViewModel switch
        {
            DashboardViewModel => NavigationPage.Dashboard,
            TenantViewModel => NavigationPage.Tenants,
            ShelfViewModel => NavigationPage.Shelves,
            RentalViewModel => NavigationPage.Rentals,
            _ => null
        };

        public ObservableCollection<Rental> Rentals { get; } = new();

        public DashboardViewModel Dashboard { get; }
        public TenantViewModel TenantManagement { get; }
        public ShelfViewModel ShelfManagement { get; }
        public RentalViewModel RentalManagement { get; }


        public RelayCommand ShowDashboardCommand { get; }
        public RelayCommand ShowTenantsCommand { get; }
        public RelayCommand ShowShelfManagementCommand { get; }
        public RelayCommand ShowRentalManagementCommand { get; }

        public MainViewModel(IRepository<Tenant> tenantRepository)
        {
            Dashboard = new DashboardViewModel();
            TenantManagement = new TenantViewModel(Rentals, tenantRepository);
            ShelfManagement = new ShelfViewModel(Rentals);
            RentalManagement = new RentalViewModel(
                TenantManagement.Tenants,
                ShelfManagement.Shelves,
                Rentals);

            CurrentViewModel = Dashboard;

            // Development sample: create a tenant through the existing flow,
            // so its ID counter and visible tenant list stay consistent.
            TenantManagement.Name = "Testlejer";
            TenantManagement.AddTenantCommand.Execute(null);

            Tenant sampleTenant = TenantManagement.Tenants[0];
            Shelf sampleShelf = ShelfManagement.Shelves[0];

            Rental sampleRental = new(sampleTenant, sampleShelf)
            {
                RentalId = 1,
                StartDate = DateTime.Today.AddMonths(-1),
                EndDate = DateTime.Today.AddDays(-1),
                MonthlyRent = 850m
            };
            Rental sampleRental2 = new(sampleTenant, sampleShelf)
            {
                RentalId = 2,
                StartDate = DateTime.Today,
                EndDate = null,
                MonthlyRent = 850m
            };

            Rentals.Add(sampleRental);
            Rentals.Add(sampleRental2);

            ShowDashboardCommand =
                new RelayCommand(_ => CurrentViewModel = Dashboard);
            ShowTenantsCommand =
                new RelayCommand(_ => CurrentViewModel = TenantManagement);
            ShowShelfManagementCommand =
                new RelayCommand(_ =>
                {
                    ShelfManagement.Refresh();
                    CurrentViewModel = ShelfManagement;
                });
            ShowRentalManagementCommand =
                new RelayCommand(_ =>
                {
                    RentalManagement.Refresh();
                    CurrentViewModel = RentalManagement;
                });
        }
    }
}
