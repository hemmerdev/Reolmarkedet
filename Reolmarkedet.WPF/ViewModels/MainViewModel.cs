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
