using System.Collections.ObjectModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Data.Common;
using Reolmarkedet.Core.Interfaces;
using Reolmarkedet.Core.Models;
using Reolmarkedet.Core.Services;
using Reolmarkedet.WPF.Commands;

namespace Reolmarkedet.WPF.ViewModels;

public class ItemViewModel : ViewModelBase
{
    private readonly IItemRepository _items;
    private readonly IRepository<Rental> _rentals;
    private readonly ItemService _service;
    private static readonly CultureInfo PriceCulture = CultureInfo.GetCultureInfo("da-DK");
    public ObservableCollection<Item> UnsoldItems { get; } = new();
    public ObservableCollection<RentalOption> RentalOptions { get; } = new();
    private Item? _selectedItem;
    private RentalOption? _selectedRental;
    private string _description = "", _priceText = "", _barcode = "", _lookupBarcode = "", _message = "";
    public string Description { get => _description; set => Set(ref _description, value); }
    public string PriceText { get => _priceText; set => Set(ref _priceText, value); }
    public string Barcode { get => _barcode; set => Set(ref _barcode, value); }
    public string LookupBarcode { get => _lookupBarcode; set => Set(ref _lookupBarcode, value); }
    public string Message { get => _message; private set => Set(ref _message, value); }
    public RentalOption? SelectedRental { get => _selectedRental; set => Set(ref _selectedRental, value); }
    public bool IsNew => SelectedItem is null;
    public bool IsEditing => !IsNew;
    public string FormTitle => IsNew ? "Registrer vare" : $"Rediger vare #{SelectedItem!.ItemId}";
    public Item? SelectedItem
    {
        get => _selectedItem;
        set
        {
            _selectedItem = value;
            OnPropertyChanged(); OnPropertyChanged(nameof(IsNew)); OnPropertyChanged(nameof(IsEditing)); OnPropertyChanged(nameof(FormTitle));
            Description = value?.Description ?? "";
            PriceText = value?.Price.ToString("0.00", PriceCulture) ?? "";
            Barcode = value?.Barcode ?? "";
            SelectedRental = RentalOptions.FirstOrDefault(r => r.Id == value?.RentalId);
            Message = "";
        }
    }
    public RelayCommand SaveCommand { get; }
    public RelayCommand NewCommand { get; }
    public RelayCommand FindCommand { get; }
    public RelayCommand RefreshCommand { get; }

    public ItemViewModel(IItemRepository items, IRepository<Rental> rentals)
    {
        _items = items; _rentals = rentals; _service = new(items, rentals);
        SaveCommand = new(_ => Run(Save));
        NewCommand = new(_ => SelectedItem = null);
        FindCommand = new(_ => Run(Find));
        RefreshCommand = new(_ => Refresh());
    }
    public void Refresh() => Run(() => { Reload(); Message = ""; });
    private void Reload()
    {
        // Fetch first: a connection error must not erase the current form/list.
        var rentals = _rentals.GetAll().Select(r => new RentalOption(r.RentalId,
            $"#{r.RentalId} · {r.Tenant.Name} · Reol {r.Shelf.ShelfNumber}")).ToList();
        var items = _items.GetUnsold().ToList();
        SelectedItem = null;
        RentalOptions.Clear(); foreach (var rental in rentals) RentalOptions.Add(rental);
        UnsoldItems.Clear(); foreach (var item in items) UnsoldItems.Add(item);
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
            id = _service.Register(SelectedRental.Id, Description, price, Barcode).ItemId;
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
            SelectedItem = UnsoldItems.FirstOrDefault(i => i.ItemId == id);
            Message = $"Vare #{id} er gemt.";
        }
        catch (DbException)
        { Message = $"Vare #{id} er gemt, men listen kunne ikke opdateres. Prøv Opdater liste."; }
    }
    private void Find()
    {
        SelectedItem = null; // A miss must not leave a previous match available for editing.
        Item? item = _service.FindByBarcode(LookupBarcode);
        if (item is null) { Message = "Ingen vare fundet med denne stregkode."; return; }
        if (_items.IsSold(item.ItemId))
        {
            Message = $"Vare #{item.ItemId}: {item.Description} · {item.Price.ToString("0.00", PriceCulture)} kr. er solgt og kan ikke redigeres.";
            return;
        }
        Reload();
        SelectedItem = UnsoldItems.FirstOrDefault(i => i.ItemId == item.ItemId);
        Message = SelectedItem is null ? "Varen er ikke længere usolgt. Opdater listen." : $"Vare #{item.ItemId} fundet.";
    }
    private void Run(Action action)
    {
        try { action(); }
        catch (ArgumentException ex) { Message = ex.Message; }
        catch (InvalidOperationException ex) { Message = ex.Message; }
        catch (DbException) { Message = "Databasen kunne ikke kontaktes eller handlingen kunne ikke gennemføres. Prøv igen."; }
    }
    private void Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    { field = value; OnPropertyChanged(name); }
}

public record RentalOption(int Id, string Label);
