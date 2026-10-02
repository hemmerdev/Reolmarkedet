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
            get => _currentViewModel;
            private set
            {
                if (_currentViewModel != value)
                {   // Stop the clock when switching away from the Dashboard
                    if (_currentViewModel is DashboardViewModel previousViewModel)
                    {
                        previousViewModel.StopClock();
                    }

                    _currentViewModel = value;
                    // Start the clock when switching to the Dashboard
                    if (_currentViewModel is DashboardViewModel nextViewModel)
                    {
                        nextViewModel.StartClock();
                    }

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
            ItemViewModel => NavigationPage.Items,
            SaleViewModel => NavigationPage.Sales,
            SettlementViewModel => NavigationPage.Settlements,
            _ => null
        };

        public ObservableCollection<Rental> Rentals { get; } = new();

        public DashboardViewModel Dashboard { get; }
        public TenantViewModel TenantManagement { get; }
        public ShelfViewModel ShelfManagement { get; }
        public RentalViewModel RentalManagement { get; }
        public ItemViewModel ItemManagement { get; }
        public SaleViewModel SaleManagement { get; }
        public SettlementViewModel SettlementManagement { get; }

        public RelayCommand ShowItemManagementCommand { get; }
        public RelayCommand ShowDashboardCommand { get; }
        public RelayCommand ShowTenantsCommand { get; }
        public RelayCommand ShowShelfManagementCommand { get; }
        public RelayCommand ShowRentalManagementCommand { get; }
        public RelayCommand ShowSaleManagementCommand { get; }
        public RelayCommand ShowSettlementCommand { get; }

        public MainViewModel(
            IRepository<Tenant> tenantRepository,
            IRepository<Shelf> shelfRepository,
            IRepository<ShelfType> shelfTypeRepository,
            IRepository<Rental> rentalRepository,
            IItemRepository itemRepository,
            IRepository<Sale> saleRepository)
        {
            TenantManagement = new TenantViewModel(Rentals, tenantRepository);
            ShelfManagement = new ShelfViewModel(
                Rentals,
                shelfRepository,
                shelfTypeRepository);

            RentalManagement = new RentalViewModel(
                TenantManagement.Tenants,
                ShelfManagement.Shelves,
                Rentals,
                rentalRepository);

            Dashboard = new DashboardViewModel(ShelfManagement.Shelves, Rentals);
            CurrentViewModel = Dashboard;
            ItemManagement = new ItemViewModel(itemRepository, rentalRepository);
            SaleManagement = new SaleViewModel(itemRepository, saleRepository, rentalRepository);
            SettlementManagement = new SettlementViewModel(
                tenantRepository, saleRepository, itemRepository, rentalRepository);

            ShowDashboardCommand =
                new RelayCommand(_ =>
                {
                    Dashboard.RefreshShelfCounts();
                    CurrentViewModel = Dashboard;
                });
            ShowTenantsCommand =
                new RelayCommand(_ =>
                {
                    TenantManagement.Refresh();
                    CurrentViewModel = TenantManagement;
                });
            ShowShelfManagementCommand = new RelayCommand(_ =>
                {
                    ShelfManagement.Refresh();
                    CurrentViewModel = ShelfManagement;
                });
            ShowRentalManagementCommand = new RelayCommand(_ =>
                {
                    RentalManagement.Refresh();
                    CurrentViewModel = RentalManagement;
                });
            ShowItemManagementCommand = new RelayCommand(_ =>
            {
                ItemManagement.Refresh();
                CurrentViewModel = ItemManagement;
            });
            ShowSaleManagementCommand = new RelayCommand(_ =>
            {
                SaleManagement.Refresh();
                CurrentViewModel = SaleManagement;
            });
            ShowSettlementCommand =
                new RelayCommand(_ =>
                {
                    SettlementManagement.Refresh();
                    CurrentViewModel = SettlementManagement;
                });

        }
    }
}
