using Reolmarkedet.Core.Interfaces;
using Reolmarkedet.Core.Models;
using Reolmarkedet.Core.Services;
using Reolmarkedet.WPF.Commands;
using System.Collections.ObjectModel;

namespace Reolmarkedet.WPF.ViewModels
{
    public class SaleViewModel : ViewModelBase
    {
        private readonly SalesService _salesService;
        private readonly IRepository<Rental> _rentalRepository;
        private readonly IItemRepository _itemRepository;
        private readonly IRepository<Sale> _saleRepository;
        private Rental? _selectedRental;
        private string _searchText = string.Empty;
        private string _salePriceText = string.Empty;
        private string _notes = string.Empty;
        private Item? _selectedItem;
        private string _saleConfirmationMessage = string.Empty;
        private string _saleMessage = string.Empty;

        public ObservableCollection<SaleRowViewModel> Sales { get; } = new();

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText != value)
                {
                    _searchText = value;
                    OnPropertyChanged();

                    FindItemCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public string SalePriceText
        {
            get => _salePriceText;
            set
            {
                if (_salePriceText != value)
                {
                    _salePriceText = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Notes
        {
            get => _notes;
            set
            {
                if (_notes != value)
                {
                    _notes = value;
                    OnPropertyChanged();
                }
            }
        }

        public Item? SelectedItem
        {
            get => _selectedItem;
            set
            {
                if (_selectedItem != value)
                {
                    _selectedItem = value;

                    if (_selectedItem is null)
                    {
                        _selectedRental = null;
                    }

                    OnPropertyChanged();
                    OnPropertyChanged(nameof(TenantName));
                    OnPropertyChanged(nameof(ShelfNumber));
                    SalePriceText = SelectedItem?.Price.ToString("0.00") ?? string.Empty;
                    Notes = string.Empty;
                    SaleMessage = string.Empty;
                    SaleConfirmationMessage = string.Empty;

                    RegisterSaleCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public string SaleMessage
        {
            get => _saleMessage;
            private set
            {
                if (_saleMessage != value)
                {
                    _saleMessage = value;
                    OnPropertyChanged();
                }
            }
        }

        public string SaleConfirmationMessage
        {
            get => _saleConfirmationMessage;
            private set
            {
                if (_saleConfirmationMessage != value)
                {
                    _saleConfirmationMessage = value;
                    OnPropertyChanged();
                }
            }
        }

        public string TenantName => _selectedRental?.Tenant.Name ?? string.Empty;
        public int? ShelfNumber => _selectedRental?.Shelf.ShelfNumber;

        public RelayCommand FindItemCommand { get; }
        public RelayCommand RegisterSaleCommand { get; }

        public SaleViewModel(
            IItemRepository itemRepository,
            IRepository<Sale> saleRepository,
            IRepository<Rental> rentalRepository)
        {
            _itemRepository = itemRepository;
            _saleRepository = saleRepository;
            _rentalRepository = rentalRepository;

            _salesService = new SalesService(_itemRepository, _saleRepository);

            FindItemCommand = new RelayCommand(FindItem, CanFindItem);
            RegisterSaleCommand = new RelayCommand(RegisterSale, CanRegisterSale);
        }

        private bool CanRegisterSale(object? parameter)
        {
            return SelectedItem is not null &&
                   _selectedRental is not null;
        }

        private void RegisterSale(object? parameter)
        {
            SaleMessage = string.Empty;
            SaleConfirmationMessage = string.Empty;

            if (SelectedItem is null ||
                _selectedRental is null)
            {
                return;
            }

            if (!decimal.TryParse(SalePriceText, out decimal salePrice))
            {
                SaleMessage = "Salgsprisen skal være et gyldigt tal.";
                return;
            }

            try
            {
                Sale sale = _salesService.RegisterSale(
                    SelectedItem.ItemId,
                    salePrice,
                    DateOnly.FromDateTime(DateTime.Today),
                    Notes);
                Sales.Add(new SaleRowViewModel(sale, SelectedItem, _selectedRental));

                SelectedItem = null;
                SearchText = string.Empty;
                SaleConfirmationMessage = "Salget er blevet registreret.";
            }
            catch (ArgumentException ex)
            {
                SaleMessage = ex.Message;
            }
            catch (InvalidOperationException ex)
            {
                SaleMessage = ex.Message;
            }
        }

        private bool CanFindItem(object? parameter)
        {
            return !string.IsNullOrWhiteSpace(SearchText);
        }

        private void FindItem(object? parameter)
        {
            SaleMessage = string.Empty;
            SaleConfirmationMessage = string.Empty;

            SelectedItem = null;

            try
            {
                Item item = _salesService.FindItem(SearchText);
                Rental? rental = _rentalRepository.GetById(item.RentalId);

                if (rental is null)
                {
                    SaleMessage = "Ingen lejeaftale fundet for varen.";
                    return;
                }

                _selectedRental = rental;
                SelectedItem = item;
            }
            catch (ArgumentException ex)
            {
                SaleMessage = ex.Message;
            }
            catch (InvalidOperationException ex)
            {
                SaleMessage = ex.Message;
            }
        }

        private void LoadSales()
        {
            List<Sale> sales = new List<Sale>(_saleRepository.GetAll());
            List<Item> items = new List<Item>(_itemRepository.GetAll());
            List<Rental> rentals = new List<Rental>(_rentalRepository.GetAll());
            List<SaleRowViewModel> rows = new();

            foreach (Sale sale in sales)
            {
                Item? item = items.FirstOrDefault(i => i.ItemId == sale.ItemId);
                if (item is null)
                {
                    throw new InvalidOperationException(
                        $"Varen til salg {sale.SaleId} blev ikke fundet.");
                }

                Rental? rental = rentals.FirstOrDefault(r => r.RentalId == item.RentalId);
                if (rental is null)
                {
                    throw new InvalidOperationException(
                        $"Lejemålet til salg {sale.SaleId} blev ikke fundet.");
                }

                rows.Add(new SaleRowViewModel(sale, item, rental));
            }

            Sales.Clear();
            foreach (SaleRowViewModel row in rows)
            {
                Sales.Add(row);
            }
        }

        public void Refresh()
        {
            SelectedItem = null;
            SearchText = string.Empty;
            SaleMessage = string.Empty;
            SaleConfirmationMessage = string.Empty;
            try
            {
                LoadSales();
            }
            catch (InvalidOperationException ex)
            {
                SaleMessage = ex.Message;
            }
        }
    }
}
