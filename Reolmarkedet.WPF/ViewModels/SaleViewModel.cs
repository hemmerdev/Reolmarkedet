using Reolmarkedet.Core.Interfaces;
using Reolmarkedet.Core.Models;
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

        public ObservableCollection<SaleRowViewModel> Sales { get; } = new();
        public ObservableCollection<RentalRowViewModel> RentalOptions { get; } = new();
        public ObservableCollection<Item> ItemOptions { get; } = new();
        public ObservableCollection<BasketItemViewModel> BasketItems { get; } = new();

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

        public string SaleMessage
        {
            get => _saleMessage;
            private set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    SaleConfirmationMessage = string.Empty;
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
                }
                if (_saleConfirmationMessage != value)
                {
                    _saleConfirmationMessage = value;
                    OnPropertyChanged();
                }
            }
        }

        public RelayCommand FindItemCommand { get; }
        public RelayCommand RegisterSaleCommand { get; }
        public RelayCommand AddToBasketCommand { get; }
        public RelayCommand RemoveFromBasketCommand { get; }


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

            // This tells WPF to recalculate the BasketTotal property whenever an item is added or removed from the basket.
            BasketItems.CollectionChanged += (_, _) =>
            {
                OnPropertyChanged(nameof(BasketTotal));
                AddToBasketCommand.RaiseCanExecuteChanged();
                RegisterSaleCommand.RaiseCanExecuteChanged();
            };
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
            SaleMessage = string.Empty;
            SaleConfirmationMessage = string.Empty;

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
            return BasketItems.Count > 0;
        }

        private void RegisterSale(object? parameter)
        {
            SaleMessage = string.Empty;
            SaleConfirmationMessage = string.Empty;

            if (BasketItems.Count == 0)
            {
                return;
            }

            List<BasketItemViewModel> basketItems =
                BasketItems.ToList();

            DateOnly saleDate = DateOnly.FromDateTime(DateTime.Today);
            foreach (var row in basketItems)
            {
                try
                {
                    Sale sale = _salesService.RegisterSale(
                        row.ItemId,
                        row.SalePrice,
                        saleDate,
                        row.Notes);
                    Sales.Add(new SaleRowViewModel(sale, row.Item, row.Rental));
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
            SaleConfirmationMessage = "Købet er blevet registreret.";
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
            catch (DbException)
            {
                SaleMessage = "Varen kunne ikke hentes fra databasen. Prøv igen.";
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
            catch (DbException)
            {
                RentalOptions.Clear();
                ItemOptions.Clear();
                SaleMessage =
                        "Salgsoversigten og varevalget kunne ikke opdateres fra databasen. " +
                        "Åbn salgsvisningen igen for at prøve igen.";
            }
        }
    }
}
