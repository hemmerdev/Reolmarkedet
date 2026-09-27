using Reolmarkedet.Core.Interfaces;
using Reolmarkedet.Core.Models;
using Reolmarkedet.Core.Models.enums;
using Reolmarkedet.Core.Services;
using Reolmarkedet.WPF.Commands;
using System.Collections.ObjectModel;
using System.Data.Common;
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
        private string _saleSearchText = string.Empty;
        private DateTime? _saleFromDate;
        private DateTime? _saleToDate;
        private SaleRowViewModel? _selectedSaleRow;
        private string _returnMessage = string.Empty;
        private string _returnConfirmationMessage = string.Empty;
        private PaymentMethod? _selectedPaymentMethod = null;

        public ObservableCollection<SaleRowViewModel> Sales { get; } = new();
        public ObservableCollection<SaleRowViewModel> VisibleSales { get; } = new();
        public ObservableCollection<RentalRowViewModel> RentalOptions { get; } = new();
        public ObservableCollection<Item> ItemOptions { get; } = new();
        public ObservableCollection<BasketItemViewModel> BasketItems { get; } = new();
        public ObservableCollection<PaymentMethod> PaymentMethods { get; } = new()
        {
            PaymentMethod.Cash,
            PaymentMethod.Card,
            PaymentMethod.MobilePay
        };

        public decimal BasketTotal
        {
            get
            {
                decimal total = 0;
                foreach (var item in BasketItems)
                {
                    total += item.SalePrice;
                }
                return total;
            }
        }

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
                    SaleMessage = string.Empty;
                    SaleConfirmationMessage = string.Empty;
                }
            }
        }

        public string SaleSearchText
        {
            get => _saleSearchText;
            set
            {
                if (_saleSearchText != value)
                {
                    _saleSearchText = value;
                    OnPropertyChanged();
                    ApplySaleFilter();
                    ClearSaleFiltersCommand.RaiseCanExecuteChanged();
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
                    SaleMessage = string.Empty;
                    SaleConfirmationMessage = string.Empty;
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
                    SaleMessage = string.Empty;
                    SaleConfirmationMessage = string.Empty;
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
                    AddToBasketCommand.RaiseCanExecuteChanged();
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
                        catch (DbException)
                        {
                            SaleMessage =
                                "Varens lejemål kunne ikke hentes fra databasen. Prøv at vælge varen igen.";
                        }
                    }
                }
            }
        }

        public SaleRowViewModel? SelectedSaleRow
        {
            get => _selectedSaleRow;
            set
            {
                if (_selectedSaleRow != value)
                {
                    _selectedSaleRow = value;
                    ReturnMessage = string.Empty;
                    ReturnConfirmationMessage = string.Empty;
                    OnPropertyChanged();
                    ReturnItemCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public string SaleMessage
        {
            get => _saleMessage;
            private set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    SaleConfirmationMessage = string.Empty;
                    ReturnConfirmationMessage = string.Empty;
                    ReturnMessage = string.Empty;
                }
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
                if (!string.IsNullOrEmpty(value))
                {
                    SaleMessage = string.Empty;
                    ReturnConfirmationMessage = string.Empty;
                    ReturnMessage = string.Empty;
                }
                if (_saleConfirmationMessage != value)
                {
                    _saleConfirmationMessage = value;
                    OnPropertyChanged();
                }
            }
        }

        public string ReturnMessage
        {
            get => _returnMessage;
            private set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    ReturnConfirmationMessage = string.Empty;
                    SaleMessage = string.Empty;
                    SaleConfirmationMessage = string.Empty;
                }
                if (_returnMessage != value)
                {
                    _returnMessage = value;
                    OnPropertyChanged();
                }
            }
        }

        public string ReturnConfirmationMessage
        {
            get => _returnConfirmationMessage;
            private set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    ReturnMessage = string.Empty;
                    SaleMessage = string.Empty;
                    SaleConfirmationMessage = string.Empty;
                }
                if (_returnConfirmationMessage != value)
                {
                    _returnConfirmationMessage = value;
                    OnPropertyChanged();
                }
            }
        }

        public DateTime? SaleFromDate
        {
            get => _saleFromDate;
            set
            {
                if (_saleFromDate != value)
                {
                    _saleFromDate = value;
                    OnPropertyChanged();
                    ApplySaleFilter();
                    ClearSaleFiltersCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public DateTime? SaleToDate
        {
            get => _saleToDate;
            set
            {
                if (_saleToDate != value)
                {
                    _saleToDate = value;
                    OnPropertyChanged();
                    ApplySaleFilter();
                    ClearSaleFiltersCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public PaymentMethod? SelectedPaymentMethod
        {
            get => _selectedPaymentMethod;
            set
            {
                if (_selectedPaymentMethod != value)
                {
                    _selectedPaymentMethod = value;
                    OnPropertyChanged();
                    SaleMessage = string.Empty;
                    SaleConfirmationMessage = string.Empty;
                    RegisterSaleCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public RelayCommand FindItemCommand { get; }
        public RelayCommand RegisterSaleCommand { get; }
        public RelayCommand AddToBasketCommand { get; }
        public RelayCommand RemoveFromBasketCommand { get; }
        public RelayCommand ClearSaleFiltersCommand { get; }
        public RelayCommand ReturnItemCommand { get; }


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
            AddToBasketCommand = new RelayCommand(AddToBasket, CanAddToBasket);
            RemoveFromBasketCommand = new RelayCommand(RemoveFromBasket, CanRemoveFromBasket);
            ClearSaleFiltersCommand = new RelayCommand(ClearSaleFilters, CanClearSaleFilters);
            ReturnItemCommand = new RelayCommand(ReturnItem, CanReturnItem);
            // This tells WPF to recalculate the BasketTotal property whenever an item is added or removed from the basket.
            BasketItems.CollectionChanged += (_, _) =>
            {
                OnPropertyChanged(nameof(BasketTotal));
                AddToBasketCommand.RaiseCanExecuteChanged();
                RegisterSaleCommand.RaiseCanExecuteChanged();
            };

            Sales.CollectionChanged += (_, _) => ApplySaleFilter();
        }

        private bool CanReturnItem(object? parameter)
        {
            return SelectedSaleRow is not null;
        }

        private void ReturnItem(object? parameter)
        {
            ClearMessages();
            var selectedRow = SelectedSaleRow;
            if (selectedRow is null)
            {
                return;
            }

            try
            {
                _salesService.ReturnItem(selectedRow.SaleId);
            }
            catch (InvalidOperationException ex)
            {
                ReturnMessage = ex.Message;
                return;
            }
            catch (DbException)
            {
                ReturnMessage = "Varen kunne ikke returneres pga. en databasefejl. Prøv igen.";
                return;
            }

            Sales.Remove(selectedRow);
            SelectedSaleRow = null;
            SelectedRentalOption = null;
            SelectedItemOption = null;

            try
            {
                LoadRentalOptions();
            }
            catch (DbException)
            {
                ReturnMessage = "Varen er returneret, men varelisten kunne ikke opdateres. Åbn salgsvisningen igen.";
                return;
            }

            ReturnConfirmationMessage = "Varen er returneret og kan sælges igen";
        }

        private bool CanClearSaleFilters(object? parameter)
        {
            return !string.IsNullOrEmpty(SaleSearchText) ||
                   SaleFromDate.HasValue ||
                   SaleToDate.HasValue;
        }

        private void ClearSaleFilters(object? parameter)
        {
            SaleSearchText = string.Empty;
            SaleFromDate = null;
            SaleToDate = null;
        }

        private bool CanRemoveFromBasket(object? parameter)
        {
            // Ensure that the parameter is a BasketItemViewModel and that it exists in the BasketItems collection.
            return parameter is BasketItemViewModel basketItem &&
                   BasketItems.Contains(basketItem);
        }

        private void RemoveFromBasket(object? parameter)
        {
            if (parameter is BasketItemViewModel basketItem &&
                BasketItems.Contains(basketItem))
            {
                BasketItems.Remove(basketItem);
                // After removing the item from the basket, check if it should be added back to the ItemOptions list.
                Item? item = _unsoldItems.FirstOrDefault(
                    item => item.ItemId == basketItem.ItemId);
                if (item is not null &&
                    item.RentalId == SelectedRentalOption?.RentalId &&
                    !ItemOptions.Any(option => option.ItemId == item.ItemId))
                {
                    ItemOptions.Add(item);
                }

                SaleConfirmationMessage = "Varen er blevet fjernet fra kurven.";
            }
        }

        private bool CanAddToBasket(object? parameter)
        {
            // Ensure that an item is selected, a rental is selected, and the item is not already in the basket.
            return SelectedItem is not null &&
                   _selectedRental is not null &&
                   !BasketItems.Any(row => row.ItemId == SelectedItem.ItemId);
        }

        private void AddToBasket(object? parameter)
        {
            ClearMessages();

            if (SelectedItem is null ||
                _selectedRental is null)
            {
                return;
            }
            if (BasketItems.Any(row => row.ItemId == SelectedItem.ItemId))
            {
                SaleMessage = "Varen er allerede i kurven.";
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
                SalesService.ValidateSale(Notes, salePrice);

                BasketItems.Add(new BasketItemViewModel(
                    SelectedItem, _selectedRental, salePrice, Notes));
                FilterItemsForRental();

                SelectedItemOption = null;
                SelectedItem = null;
                SearchText = string.Empty;
                SaleConfirmationMessage = "Varen er blevet tilføjet til kurven.";
            }
            catch (ArgumentException ex)
            {
                SaleMessage = ex.Message;
            }
        }

        private bool CanRegisterSale(object? parameter)
        {
            return BasketItems.Count > 0 &&
                   SelectedPaymentMethod.HasValue;
        }

        private void RegisterSale(object? parameter)
        {
            ClearMessages();

            if (BasketItems.Count == 0)
            {
                return;
            }

            List<BasketItemViewModel> basketItems =
                BasketItems.ToList();

            if (SelectedPaymentMethod is null)
            {
                SaleMessage = "Vælg en betalingsmetode.";
                return;
            }

            DateOnly saleDate = DateOnly.FromDateTime(DateTime.Today);
            foreach (var row in basketItems)
            {
                try
                {
                    Sale sale = _salesService.RegisterSale(
                        row.ItemId,
                        row.SalePrice,
                        saleDate,
                        row.Notes,
                        SelectedPaymentMethod.Value);
                    Sales.Insert(0, new SaleRowViewModel(sale, row.Item, row.Rental));
                    RemoveSoldItemFromOptions(row.Item);
                    BasketItems.Remove(row);
                }
                catch (ArgumentException ex)
                {
                    SaleMessage =
                       $"Vare {row.ItemId} kunne ikke registreres: {ex.Message} " +
                       "Tidligere registrerede salg er gemt. " +
                       "Kontrollér oversigten før du prøver igen.";
                    return;
                }
                catch (InvalidOperationException ex)
                {
                    SaleMessage =
                        $"Vare {row.ItemId} kunne ikke registreres: {ex.Message} " +
                        "Tidligere registrerede salg er gemt. " +
                        "Kontrollér oversigten før du prøver igen.";
                    return;
                }
                catch (DbException)
                {
                    SaleMessage =
                        $"Købet kunne ikke færdigregistreres ved vare {row.ItemId}. " +
                        "Tidligere registrerede salg er gemt. " +
                        "Kontrollér oversigten før du prøver igen.";
                    return;
                }

            }

            SelectedItem = null;
            SearchText = string.Empty;
            SelectedPaymentMethod = null;
            SaleConfirmationMessage = "Købet er blevet registreret.";
        }

        private bool CanFindItem(object? parameter)
        {
            return !string.IsNullOrWhiteSpace(SearchText);
        }

        private void FindItem(object? parameter)
        {
            ClearMessages();

            SelectedItem = null;
            SelectedRentalOption = null;
            SelectedItemOption = null;

            try
            {
                Item item = _salesService.FindItem(SearchText);

                if (BasketItems.Any(row => row.ItemId == item.ItemId))
                {
                    SaleMessage = "Varen er allerede i kurven.";
                    return;
                }

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
            catch (DbException)
            {
                SaleMessage = "Varen kunne ikke hentes fra databasen. Prøv igen.";
            }
        }

        private void LoadSales()
        {
            List<Sale> sales = _saleRepository.GetAll()
                .OrderByDescending(sale => sale.SaleDate)
                .ThenByDescending(sale => sale.SaleId)
                .ToList();
            List<Item> items = _itemRepository.GetAll().ToList();
            List<Rental> rentals = _rentalRepository.GetAll().ToList();
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
                    RentalOptions.Add(
                        new RentalRowViewModel(rental, rental.MonthlyRent));
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
                if (item.RentalId == SelectedRentalOption.RentalId &&
                    !BasketItems.Any(row => row.ItemId == item.ItemId))
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

        private void ApplySaleFilter()
        {
            string searchText = SaleSearchText.Trim();
            VisibleSales.Clear();
            foreach (var row in Sales)
            {
                bool matchesSearch =
                    (string.IsNullOrWhiteSpace(searchText) ||
                     row.ItemDescription.Contains(
                         searchText, StringComparison.OrdinalIgnoreCase) ||
                     row.TenantName.Contains(
                         searchText, StringComparison.OrdinalIgnoreCase) ||
                     row.ShelfNumber.ToString() == searchText);

                bool matchesFromDate =
                    !SaleFromDate.HasValue ||
                    row.SaleDate >= DateOnly.FromDateTime(SaleFromDate.Value);

                bool matchesToDate =
                    !SaleToDate.HasValue ||
                    row.SaleDate <= DateOnly.FromDateTime(SaleToDate.Value);

                if (matchesSearch && matchesFromDate && matchesToDate)
                {
                    VisibleSales.Add(row);
                }
            }
        }

        public void Refresh()
        {
            SelectedItem = null;
            SelectedRentalOption = null;
            SelectedItemOption = null;
            SelectedSaleRow = null;
            ItemOptions.Clear();
            SearchText = string.Empty;
            ClearMessages();

            try
            {
                LoadSales();
                LoadRentalOptions();
            }
            catch (InvalidOperationException ex)
            {
                SaleMessage = ex.Message;
            }
            catch (DbException)
            {
                RentalOptions.Clear();
                ItemOptions.Clear();
                SaleMessage =
                        "Salgsoversigten og varevalget kunne ikke opdateres fra databasen. " +
                        "Åbn salgsvisningen igen for at prøve igen.";
            }
        }

        private void ClearMessages()
        {
            SaleMessage = string.Empty;
            SaleConfirmationMessage = string.Empty;
            ReturnMessage = string.Empty;
            ReturnConfirmationMessage = string.Empty;
        }
    }
}
