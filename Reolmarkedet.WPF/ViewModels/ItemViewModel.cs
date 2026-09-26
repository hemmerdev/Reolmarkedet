using Reolmarkedet.Core.Interfaces;
using Reolmarkedet.Core.Models;
using Reolmarkedet.Core.Services;
using Reolmarkedet.WPF.Commands;
using System.Collections.ObjectModel;
using System.Data.Common;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace Reolmarkedet.WPF.ViewModels;

public class ItemViewModel : ViewModelBase
{
    private readonly IItemRepository _items;
    private readonly IRepository<Rental> _rentals;
    private readonly ItemService _service;
    private readonly List<ItemRowViewModel> _itemRows = new();
    private ItemRowViewModel? _selectedItemRow;
    private static readonly CultureInfo PriceCulture = CultureInfo.GetCultureInfo("da-DK");
    public ObservableCollection<Item> UnsoldItems { get; } = new();
    public ObservableCollection<ItemRowViewModel> VisibleItems { get; } = new();
    public ObservableCollection<RentalOption> RentalOptions { get; } = new();
    public ObservableCollection<RentalOption> EligibleRentalOptions { get; } = new();
    private Item? _selectedItem;
    private RentalOption? _selectedRental;
    private string _description = "", _priceText = "", _message = "";
    private string _searchText = string.Empty;
    private string _confirmationMessage = string.Empty;

    public string Description
    {
        get => _description;
        set
        {
            if (_description != value)
            {
                Set(ref _description, value);
                ConfirmationMessage = string.Empty;
            }
        }
    }
    public string PriceText
    {
        get => _priceText;
        set
        {
            if (_priceText != value)
            {
                Set(ref _priceText, value);
                ConfirmationMessage = string.Empty;
            }
        }
    }

    public string Message { get => _message; private set => Set(ref _message, value); }
    public string ConfirmationMessage { get => _confirmationMessage; private set => Set(ref _confirmationMessage, value); }
    public RentalOption? SelectedRental
    {
        get => _selectedRental;
        set
        {
            if (_selectedRental != value)
            {
                Set(ref _selectedRental, value);
                ConfirmationMessage = string.Empty;
            }
        }
    }

    public ItemRowViewModel? SelectedItemRow
    {
        get => _selectedItemRow;
        set
        {
            if (_selectedItemRow != value)
            {
                Set(ref _selectedItemRow, value);
                SelectedItem = value?.Item;
            }
        }
    }

    public bool IsNew => SelectedItem is null;
    public bool IsEditing => !IsNew;
    public string FormTitle => IsNew ? "Registrer vare" : $"Rediger vare #{SelectedItem!.ItemId}";
    public string SearchText
    {
        get => _searchText;
        set
        {
            if (_searchText != value)
            {
                _searchText = value;
                ConfirmationMessage = string.Empty;
                OnPropertyChanged();
                ApplySearch();
            }
        }
    }

    public Item? SelectedItem
    {
        get => _selectedItem;
        set
        {
            _selectedItem = value;

            _selectedItemRow = _itemRows.FirstOrDefault(
                row => row.Item == value);
            OnPropertyChanged(nameof(SelectedItemRow));
            OnPropertyChanged(); OnPropertyChanged(nameof(IsNew)); OnPropertyChanged(nameof(IsEditing)); OnPropertyChanged(nameof(FormTitle));
            Description = value?.Description ?? "";
            PriceText = value?.Price.ToString("0.00", PriceCulture) ?? "";
            SelectedRental = RentalOptions.FirstOrDefault(r => r.Id == value?.RentalId);
            Message = "";
            ConfirmationMessage = "";

            DeleteItemCommand.RaiseCanExecuteChanged();
        }
    }
    public RelayCommand SaveCommand { get; }
    public RelayCommand NewCommand { get; }
    public RelayCommand DeleteItemCommand { get; }

    public ItemViewModel(IItemRepository items, IRepository<Rental> rentals)
    {
        _items = items; _rentals = rentals; _service = new(items, rentals);
        SaveCommand = new(_ => Run(Save));
        NewCommand = new(_ => SelectedItem = null);
        DeleteItemCommand = new(DeleteItem, CanDeleteItem);
    }

    private void DeleteItem(object? parameter)
    {
        Run(() =>
        {
            if (SelectedItem != null)
            {
                int itemId = SelectedItem.ItemId;
                _service.Delete(itemId);
                Reload();
                ConfirmationMessage = $"Vare er slettet.";
            }
        });
    }

    private bool CanDeleteItem(object? parameter)
    {
        return SelectedItem is not null;
    }

    private void ApplySearch()
    {
        VisibleItems.Clear();
        var trimmedText = SearchText.Trim();
        foreach (var item in _itemRows)
        {
            if (string.IsNullOrEmpty(trimmedText) ||
                item.Description.Contains(trimmedText, StringComparison.OrdinalIgnoreCase) ||
                item.Barcode.Contains(trimmedText, StringComparison.OrdinalIgnoreCase))
            {
                VisibleItems.Add(item);
            }
        }
    }

    public void Refresh() => Run(() => { Reload(); Message = ""; });
    private void Reload()
    {
        var rentals = _rentals.GetAll().ToList();
        var items = _items.GetUnsold().ToList();
        List<ItemRowViewModel> itemRows = new();

        foreach (var item in items)
        {
            Rental? rental = rentals.FirstOrDefault(
                rental => rental.RentalId == item.RentalId);
            if (rental is null)
            {
                throw new InvalidOperationException(
                    $"Lejemålet til vare {item.ItemId} blev ikke fundet.");
            }

            itemRows.Add(new ItemRowViewModel(item, rental));
        }

        _itemRows.Clear();
        foreach (var itemRow in itemRows)
        {
            _itemRows.Add(itemRow);
        }

        SelectedItem = null;
        RentalOptions.Clear();
        EligibleRentalOptions.Clear();

        DateTime today = DateTime.Today;
        foreach (Rental rental in rentals)
        {
            RentalOption option = new(
                rental.RentalId,
                $"#{rental.RentalId} - {rental.Tenant.Name} - Reol {rental.Shelf.ShelfNumber}");

            RentalOptions.Add(option);

            bool hasStarted = rental.StartDate.Date <= today;
            bool hasNotEnded = rental.EndDate is null ||
                rental.EndDate.Value.Date >= today;
            bool bothActive = rental.Tenant.IsActive &&
                rental.Shelf.IsActive;

            if (hasStarted &&
                hasNotEnded &&
                bothActive)
            {
                EligibleRentalOptions.Add(option);
            }
        }

        UnsoldItems.Clear(); foreach (var item in items) UnsoldItems.Add(item);

        ApplySearch();
    }
    private void Save()
    {
        // Explicit Danish format avoids silently interpreting 12.50 as 1250.
        if (!decimal.TryParse(PriceText, NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite |
            NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, PriceCulture, out decimal price))
            throw new ArgumentException("Angiv en pris med decimalkomma, fx 125,50 (uden tusindtalsseparator).");
        int id;
        if (SelectedItem is null)
        {
            if (SelectedRental is null) throw new ArgumentException("Vælg et lejemål.");
            id = _service.Register(SelectedRental.Id, Description, price, null).ItemId;
        }
        else
        {
            id = SelectedItem.ItemId;
            _service.Update(id, Description, price);
        }
        // Clear the draft after a successful write, even if refreshing subsequently fails.
        SelectedItem = null;
        try
        {
            Reload();
            ConfirmationMessage = $"Vare er gemt.";
        }
        catch (DbException)
        { Message = $"Vare er gemt, men listen kunne ikke opdateres. Skift visning og åbn varer igen for at opdatere listen"; }
    }

    private void Run(Action action)
    {
        ConfirmationMessage = string.Empty;
        Message = string.Empty;
        try { action(); }
        catch (ArgumentException ex) { Message = ex.Message; }
        catch (InvalidOperationException ex) { Message = ex.Message; }
        catch (DbException) { Message = "Databasen kunne ikke kontaktes eller handlingen kunne ikke gennemføres. Prøv igen."; }
    }
    private void Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    { field = value; OnPropertyChanged(name); }
}

public record RentalOption(int Id, string Label);
