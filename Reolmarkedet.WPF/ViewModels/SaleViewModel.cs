using Reolmarkedet.Core.Interfaces;
using Reolmarkedet.Core.Models;
using Reolmarkedet.Core.Services;
using Reolmarkedet.WPF.Commands;
using System.Collections.ObjectModel;
using System.Globalization;

namespace Reolmarkedet.WPF.ViewModels
{
    public class SaleViewModel : ViewModelBase
    {
        private readonly SalesService _salesService;
        private readonly IRepository<Rental> _rentalRepository;
        private readonly IItemRepository _itemRepository;
        private readonly IRepository<Sale> _saleRepository;
        private static readonly CultureInfo PriceCulture
            = CultureInfo.GetCultureInfo("da-DK");

        private List<Item> _unsoldItems = new();
        private RentalRowViewModel? _selectedRentalOption;
        private Item? _selectedItemOption;

        private Rental? _selectedRental;
        private string _searchText = string.Empty;
        private string _salePriceText = string.Empty;
        private string _notes = string.Empty;
        private Item? _selectedItem;
        private string _saleConfirmationMessage = string.Empty;
        private string _saleMessage = string.Empty;

        public ObservableCollection<SaleRowViewModel> Sales { get; } = new();
        public ObservableCollection<RentalRowViewModel> RentalOptions { get; } = new();
        public ObservableCollection<Item> ItemOptions { get; } = new();

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

                    SalePriceText =
                        SelectedItem?.Price.ToString("0.00", PriceCulture) ?? string.Empty;
                    Notes = string.Empty;
                    SaleMessage = string.Empty;
                    SaleConfirmationMessage = string.Empty;

                    RegisterSaleCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public RentalRowViewModel? SelectedRentalOption
        {
            get => _selectedRentalOption;
            set
            {
                if (_selectedRentalOption != value)
                {
                    _selectedRentalOption = value;
                    OnPropertyChanged();
                    SelectedItem = null;
                    SaleMessage = string.Empty;
                    SaleConfirmationMessage = string.Empty;
                    FilterItemsForRental();
                }
            }
        }

        public Item? SelectedItemOption
        {
            get => _selectedItemOption;
            set
            {
                if (_selectedItemOption != value)
                {
                    _selectedItemOption = value;
                    OnPropertyChanged();

                    SelectedItem = null;
                    SaleMessage = string.Empty;
                    SaleConfirmationMessage = string.Empty;

                    if (SelectedItemOption is not null)
                    {
                        try
                        {
                            SelectItem(SelectedItemOption);
                        }
                        catch (InvalidOperationException ex)
                        {
                            SaleMessage = ex.Message;
                        }
                    }
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
            // Explicit Danish format avoids silently interpreting 12.50 as 1250.
            if (!decimal.TryParse(
                SalePriceText,
                NumberStyles.AllowLeadingWhite |
                NumberStyles.AllowTrailingWhite |
                NumberStyles.AllowLeadingSign |
                NumberStyles.AllowDecimalPoint,
                PriceCulture,
                out decimal salePrice))
            {
                SaleMessage =
                    "Angiv en pris med decimalkomma, fx 125,50 (uden tusindtalsseparator).";
                return;
            }

            try
            {
                Item soldItem = SelectedItem;
                Sale sale = _salesService.RegisterSale(
                    soldItem.ItemId,
                    salePrice,
                    DateOnly.FromDateTime(DateTime.Today),
                    Notes);
                Sales.Add(new SaleRowViewModel(sale, soldItem, _selectedRental));
                RemoveSoldItemFromOptions(soldItem);

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
            SelectedRentalOption = null;
            SelectedItemOption = null;

            try
            {
                Item item = _salesService.FindItem(SearchText);
                SelectedRentalOption = RentalOptions.FirstOrDefault(
                    rental => rental.RentalId == item.RentalId);

                SelectedItemOption = ItemOptions.FirstOrDefault(
                    option => option.ItemId == item.ItemId);

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

        private void LoadRentalOptions()
        {
            _unsoldItems = _itemRepository.GetUnsold().ToList();
            List<Rental> rentals = _rentalRepository
                .GetAll()
                .OrderBy(rental => rental.Shelf.ShelfNumber)
                .ToList();

            RentalOptions.Clear();

            foreach (Rental rental in rentals)
            {
                bool itemExists = _unsoldItems.Any(i => i.RentalId == rental.RentalId);
                if (itemExists)
                {
                    RentalOptions.Add(new RentalRowViewModel(rental));
                }
            }
        }

        private void FilterItemsForRental()
        {
            SelectedItemOption = null;
            ItemOptions.Clear();

            if (SelectedRentalOption is null)
            {
                return;
            }

            foreach (var item in _unsoldItems)
            {
                if (item.RentalId == SelectedRentalOption.RentalId)
                {
                    ItemOptions.Add(item);
                }
            }
        }

        private void SelectItem(Item item)
        {
            Rental? rental = _rentalRepository.GetById(item.RentalId);

            if (rental is null)
            {
                SaleMessage = "Ingen lejeaftale fundet for varen.";
                return;
            }

            _selectedRental = rental;
            SelectedItem = item;
        }

        private void RemoveSoldItemFromOptions(Item soldItem)
        {
            _unsoldItems.RemoveAll(item => item.ItemId == soldItem.ItemId);

            bool hasRemainingItems = _unsoldItems.Any(
                item => item.RentalId == soldItem.RentalId);

            if (!hasRemainingItems)
            {
                if (SelectedRentalOption?.RentalId == soldItem.RentalId)
                {
                    SelectedRentalOption = null;
                }

                RentalRowViewModel? option = RentalOptions.FirstOrDefault(
                    rental => rental.RentalId == soldItem.RentalId);

                if (option is not null)
                {
                    RentalOptions.Remove(option);
                }
            }

            FilterItemsForRental();
        }

        public void Refresh()
        {
            SelectedItem = null;
            SelectedRentalOption = null;
            SelectedItemOption = null;
            ItemOptions.Clear();
            SearchText = string.Empty;
            SaleMessage = string.Empty;
            SaleConfirmationMessage = string.Empty;

            try
            {
                LoadSales();
                LoadRentalOptions();
            }
            catch (InvalidOperationException ex)
            {
                SaleMessage = ex.Message;
            }
        }
    }
}
