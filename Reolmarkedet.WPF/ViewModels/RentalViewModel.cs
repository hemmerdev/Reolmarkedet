using Reolmarkedet.Core.Interfaces;
using Reolmarkedet.Core.Models;
using Reolmarkedet.Core.Services;
using Reolmarkedet.WPF.Commands;
using System.Collections.ObjectModel;

namespace Reolmarkedet.WPF.ViewModels
{
    public class RentalViewModel : ViewModelBase
    {
        // Collections for binding to the view
        public ObservableCollection<Tenant> Tenants { get; }
        public ObservableCollection<Tenant> ActiveTenants { get; } = new();
        public ObservableCollection<Shelf> Shelves { get; }
        public ObservableCollection<Shelf> AvailableShelves { get; } = new();
        public ObservableCollection<Shelf> SelectedShelves { get; } = new();
        public ObservableCollection<Rental> Rentals { get; }
        public ObservableCollection<RentalRowViewModel> RentalRows { get; } = new();


        private readonly RentalService _rentalService = new();
        private readonly IRepository<Rental> _rentalRepository;

        // Private backing fields for properties
        private Tenant? _selectedTenant;
        private Shelf? _selectedShelf;
        private DateTime? _startDate = DateTime.Today;
        private DateTime? _endDate;
        private string _monthlyRent = string.Empty;
        private string _rentalMessage = string.Empty;
        private string _rentalConfirmationMessage = string.Empty;
        private decimal _standardMonthlyRent;
        private bool _isCustomPrice;
        private RentalRowViewModel? _selectedRentalRow;
        private DateTime? _terminationEndDate;
        private string _terminationMessage = string.Empty;
        private string _terminationConfirmationMessage = string.Empty;
        private RentalStatusFilter _selectedRentalStatusFilter =
            RentalStatusFilter.Active;

        // Public properties for binding to the view
        public Tenant? SelectedTenant
        {
            get => _selectedTenant;
            set
            {
                if (_selectedTenant != value)
                {
                    _selectedTenant = value;
                    RentalConfirmationMessage = string.Empty;
                    OnPropertyChanged();
                    RefreshPrice();
                    CreateRentalsCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public Shelf? SelectedShelf
        {
            get => _selectedShelf;
            set
            {
                if (_selectedShelf != value)
                {
                    _selectedShelf = value;
                    RentalConfirmationMessage = string.Empty;
                    OnPropertyChanged();

                    AddShelfToSelectionCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public DateTime? StartDate
        {
            get => _startDate;
            set
            {
                if (_startDate != value)
                {
                    _startDate = value;
                    RentalConfirmationMessage = string.Empty;
                    TerminationConfirmationMessage = string.Empty;
                    OnPropertyChanged();
                    RefreshAvailableShelves();
                    RefreshPrice();
                }
            }
        }

        public DateTime? EndDate
        {
            get => _endDate;
            set
            {
                if (_endDate != value)
                {
                    _endDate = value;
                    RentalConfirmationMessage = string.Empty;
                    TerminationConfirmationMessage = string.Empty;
                    OnPropertyChanged();
                    RefreshAvailableShelves();
                    RefreshPrice();
                }
            }
        }

        public string MonthlyRent
        {
            get => _monthlyRent;
            set
            {
                if (_monthlyRent != value)
                {
                    _monthlyRent = value;
                    RentalConfirmationMessage = string.Empty;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(TotalMonthlyRent));
                }
            }
        }

        public decimal? TotalMonthlyRent
        {
            get
            {
                if (SelectedShelves.Count == 0)
                {
                    return 0m;
                }
                if (!decimal.TryParse(MonthlyRent, out decimal rentPerShelf) ||
                    rentPerShelf <= 0)
                {
                    return null;
                }

                return rentPerShelf * SelectedShelves.Count;
            }
        }

        public decimal StandardMonthlyRent
        {
            get => _standardMonthlyRent;
            private set
            {
                if (_standardMonthlyRent != value)
                {
                    _standardMonthlyRent = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsCustomPrice
        {
            get => _isCustomPrice;
            private set
            {
                if (_isCustomPrice != value)
                {
                    _isCustomPrice = value;
                    OnPropertyChanged();
                    EnableCustomPriceCommand.RaiseCanExecuteChanged();
                    UseStandardPriceCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public string RentalMessage
        {
            get => _rentalMessage;
            private set
            {
                if (_rentalMessage != value)
                {
                    _rentalMessage = value;
                    OnPropertyChanged();
                }
            }
        }

        public string RentalConfirmationMessage
        {
            get => _rentalConfirmationMessage;
            private set
            {
                if (_rentalConfirmationMessage != value)
                {
                    _rentalConfirmationMessage = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool HasSelectedRental => SelectedRentalRow is not null;
        public bool ShowTerminateButton => CanTerminateRental(null);
        public bool ShowChangeEndDateButton => CanChangeTerminationEndDate(null);
        public bool ShowUndoTerminationButton => CanUndoTermination(null);
        public bool IsSelectedRentalHistorical =>
            SelectedRentalRow is not null &&
            SelectedRentalRow.Rental.EndDate.HasValue &&
            SelectedRentalRow.Rental.EndDate.Value.Date < DateTime.Today;

        public RentalRowViewModel? SelectedRentalRow
        {
            get => _selectedRentalRow;
            set
            {
                if (_selectedRentalRow != value)
                {
                    _selectedRentalRow = value;
                    TerminationConfirmationMessage = string.Empty;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(HasSelectedRental));
                    OnPropertyChanged(nameof(ShowTerminateButton));
                    OnPropertyChanged(nameof(ShowChangeEndDateButton));
                    OnPropertyChanged(nameof(ShowUndoTerminationButton));
                    OnPropertyChanged(nameof(IsSelectedRentalHistorical));

                    TerminationEndDate = _selectedRentalRow?.EndDate;
                    OnPropertyChanged(nameof(MinimumTerminationEndDate));
                    TerminationMessage = string.Empty;

                    TerminateRentalCommand.RaiseCanExecuteChanged();
                    ChangeTerminationEndDateCommand.RaiseCanExecuteChanged();
                    UndoTerminationCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public DateTime MinimumStartDate => DateTime.Today;
        public DateTime MinimumTerminationEndDate
        {
            get
            {
                DateTime minimum = DateTime.Today;
                if (SelectedRentalRow?.Rental is null)
                {
                    return minimum;
                }
                else if (SelectedRentalRow.Rental.TerminationNoticeDate.HasValue)
                {
                    DateTime noticeDate =
                        SelectedRentalRow.Rental.TerminationNoticeDate.Value;
                    minimum =
                        _rentalService.GetEarliestTerminationEndDate(noticeDate);
                }
                else if (!SelectedRentalRow.Rental.EndDate.HasValue)
                {
                    minimum =
                        _rentalService.GetEarliestTerminationEndDate(DateTime.Today);
                }
                else
                {
                    minimum = DateTime.Today;
                }

                if (minimum.Date < DateTime.Today)
                {
                    minimum = DateTime.Today;
                }
                if (minimum.Date <= SelectedRentalRow.Rental.StartDate.Date)
                {
                    minimum = SelectedRentalRow.Rental.StartDate.Date.AddDays(1);
                }

                return minimum;
            }
        }

        public DateTime? TerminationEndDate
        {
            get => _terminationEndDate;
            set
            {
                if (_terminationEndDate != value)
                {
                    _terminationEndDate = value;
                    TerminationConfirmationMessage = string.Empty;
                    OnPropertyChanged();
                }
            }
        }

        public string TerminationMessage
        {
            get => _terminationMessage;
            private set
            {
                if (_terminationMessage != value)
                {
                    _terminationMessage = value;
                    OnPropertyChanged();
                }
            }
        }

        public string TerminationConfirmationMessage
        {
            get => _terminationConfirmationMessage;
            private set
            {
                if (_terminationConfirmationMessage != value)
                {
                    _terminationConfirmationMessage = value;
                    OnPropertyChanged();
                }
            }
        }

        public RentalStatusFilter SelectedRentalStatusFilter
        {
            get => _selectedRentalStatusFilter;
            set
            {
                if (_selectedRentalStatusFilter != value)
                {
                    _selectedRentalStatusFilter = value;
                    TerminationConfirmationMessage = string.Empty;
                    OnPropertyChanged();
                    RefreshRentalRows();
                }
            }
        }

        // Commands for user interactions
        public RelayCommand AddShelfToSelectionCommand { get; }
        public RelayCommand RemoveShelfFromSelectionCommand { get; }
        public RelayCommand EnableCustomPriceCommand { get; }
        public RelayCommand UseStandardPriceCommand { get; }
        public RelayCommand CreateRentalsCommand { get; }
        public RelayCommand TerminateRentalCommand { get; }
        public RelayCommand ChangeTerminationEndDateCommand { get; }
        public RelayCommand UndoTerminationCommand { get; }

        public RentalViewModel(
            ObservableCollection<Tenant> tenants,
            ObservableCollection<Shelf> shelves,
            ObservableCollection<Rental> rentals,
            IRepository<Rental> rentalRepository)
        {
            Tenants = tenants;
            Shelves = shelves;
            Rentals = rentals;
            _rentalRepository = rentalRepository;

            // Loads existing rentals from the repository and populates the Rentals collection
            foreach (Rental rental in _rentalRepository.GetAll())
            {
                Tenant? matchingTenant = null;

                foreach (Tenant tenant in Tenants)
                {
                    if (tenant.TenantId == rental.Tenant.TenantId)
                    {
                        matchingTenant = tenant;
                        break;
                    }
                }

                Shelf? matchingShelf = null;

                foreach (Shelf shelf in Shelves)
                {
                    if (shelf.ShelfId == rental.Shelf.ShelfId)
                    {
                        matchingShelf = shelf;
                        break;
                    }
                }

                if (matchingTenant is null || matchingShelf is null)
                {
                    throw new InvalidOperationException(
                        "Rental tenant or shelf could not be found");
                }

                rental.Tenant = matchingTenant;
                rental.Shelf = matchingShelf;
                Rentals.Add(rental);
            }

            AddShelfToSelectionCommand =
                new RelayCommand(AddShelfToSelection, CanAddShelfToSelection);
            RemoveShelfFromSelectionCommand =
                new RelayCommand(RemoveShelfFromSelection, CanRemoveShelfFromSelection);
            EnableCustomPriceCommand =
                new RelayCommand(EnableCustomPrice, CanEnableCustomPrice);
            UseStandardPriceCommand =
                new RelayCommand(UseStandardPrice, CanUseStandardPrice);
            CreateRentalsCommand =
                new RelayCommand(CreateRentals, CanCreateRentals);
            TerminateRentalCommand =
                new RelayCommand(TerminateRental, CanTerminateRental);
            ChangeTerminationEndDateCommand =
                new RelayCommand(ChangeTerminationEndDate, CanChangeTerminationEndDate);
            UndoTerminationCommand =
                new RelayCommand(UndoTermination, CanUndoTermination);

            // Subscribe to changes in the SelectedShelves collection to update button availability 
            SelectedShelves.CollectionChanged += (_, _) =>
            {
                EnableCustomPriceCommand.RaiseCanExecuteChanged();
                UseStandardPriceCommand.RaiseCanExecuteChanged();
                OnPropertyChanged(nameof(TotalMonthlyRent));
                RefreshPrice();
                CreateRentalsCommand.RaiseCanExecuteChanged();

            };

            Refresh();
        }

        private bool CanUndoTermination(object? parameter)
        {
            Rental? rental = SelectedRentalRow?.Rental;

            return rental is not null &&
                   Rentals.Contains(rental) &&
                   rental.EndDate.HasValue &&
                   rental.EndDate.Value.Date >= DateTime.Today;
        }

        private void UndoTermination(object? parameter)
        {
            TerminationConfirmationMessage = string.Empty;
            RentalConfirmationMessage = string.Empty;

            Rental? rental = SelectedRentalRow?.Rental;

            if (rental is null || !Rentals.Contains(rental))
            {
                TerminationMessage = "Vælg et gyldigt lejemål at fortryde opsigelsen for.";
                return;
            }

            try
            {
                _rentalService.UndoTermination(
                    rental,
                    DateTime.Today,
                    Rentals);

                _rentalRepository.Update(rental);
            }
            catch (InvalidOperationException ex)
            {
                TerminationMessage = ex.Message;
                return;
            }

            Refresh();
            TerminationConfirmationMessage = "Lejemålet er ikke længere opsagt.";
        }

        private bool CanChangeTerminationEndDate(object? parameter)
        {
            Rental? rental = SelectedRentalRow?.Rental;

            return rental is not null &&
                   Rentals.Contains(rental) &&
                   rental.EndDate.HasValue &&
                   rental.EndDate.Value.Date >= DateTime.Today;
        }

        private void ChangeTerminationEndDate(object? parameter)
        {
            TerminationConfirmationMessage = string.Empty;
            RentalConfirmationMessage = string.Empty;

            Rental? rental = SelectedRentalRow?.Rental;

            if (rental is null || !Rentals.Contains(rental))
            {
                TerminationMessage = "Vælg et gyldigt lejemål at ændre slutdatoen for.";
                return;
            }
            if (TerminationEndDate is null)
            {
                TerminationMessage = "Vælg en ny slutdato";
                return;
            }
            if (rental.EndDate.HasValue &&
                TerminationEndDate.HasValue &&
                TerminationEndDate.Value.Date == rental.EndDate.Value.Date)
            {
                TerminationMessage = "Slutdatoen kan ikke være den samme som den nuværende slutdato.";
                return;
            }

            try
            {
                _rentalService.ChangeTerminationEndDate(
                    rental,
                    DateTime.Today,
                    TerminationEndDate.Value,
                    Rentals);
                _rentalRepository.Update(rental);

                Refresh();
                TerminationConfirmationMessage = "Lejemålet blev ændret.";

            }
            catch (ArgumentException ex)
            {
                TerminationMessage = ex.Message;
                return;
            }
            catch (InvalidOperationException ex)
            {
                TerminationMessage = ex.Message;
                return;

            }
        }

        private bool CanTerminateRental(object? parameter)
        {
            Rental? rental = SelectedRentalRow?.Rental;

            return rental is not null &&
                   Rentals.Contains(rental) &&
                   !rental.EndDate.HasValue;
        }

        private void TerminateRental(object? parameter)
        {
            TerminationConfirmationMessage = string.Empty;
            RentalConfirmationMessage = string.Empty;

            Rental? rental = SelectedRentalRow?.Rental;

            if (rental is null || !Rentals.Contains(rental))
            {
                TerminationMessage = "Vælg et gyldigt lejemål at opsige.";
                return;
            }

            if (TerminationEndDate is null)
            {
                TerminationMessage = "Vælg en slutdato for opsigelsen.";
                return;
            }

            try
            {
                _rentalService.TerminateRental(
                    rental,
                    DateTime.Today,
                    TerminationEndDate.Value,
                    Rentals);
                _rentalRepository.Update(rental);
            }
            catch (ArgumentException ex)
            {
                TerminationMessage = ex.Message;
                return;
            }
            catch (InvalidOperationException ex)
            {
                TerminationMessage = ex.Message;
                return;
            }

            SelectedRentalRow = null;
            Refresh();
            TerminationConfirmationMessage = "Lejemålet blev opsagt.";
        }

        private bool CanCreateRentals(object? parameter)
        {
            return SelectedTenant is not null &&
                   SelectedTenant.IsActive &&
                   SelectedShelves.Count > 0;
        }

        private void CreateRentals(object? parameter)
        {
            RentalConfirmationMessage = string.Empty;
            TerminationConfirmationMessage = string.Empty;

            Tenant? tenant = SelectedTenant;

            if (tenant is null ||
                !Tenants.Contains(tenant) ||
                !tenant.IsActive)
            {
                RentalMessage = "Vælg en aktiv reollejer.";
                return;
            }

            if (SelectedShelves.Count == 0)
            {
                RentalMessage = "Vælg mindst én reol.";
                return;
            }

            if (StartDate is null)
            {
                RentalMessage = "Vælg en startdato.";
                return;
            }

            DateTime startDate = StartDate.Value.Date;
            DateTime? endDate = EndDate?.Date;

            if (startDate < DateTime.Today)
            {
                RentalMessage = "Startdatoen må ikke være i fortiden.";
                return;
            }

            if (endDate.HasValue && endDate.Value <= startDate)
            {
                RentalMessage = "Slutdatoen skal være efter startdatoen.";
                return;
            }

            // Update the standard price, preserving an entered custom price.
            RefreshPrice();

            if (!decimal.TryParse(MonthlyRent, out decimal rentPerShelf) ||
                rentPerShelf <= 0)
            {
                RentalMessage = "Månedlig leje skal være et positivt tal.";
                return;
            }

            // Copy the selection so later collection changes cannot affect this loop.
            List<Shelf> shelvesToRent = new(SelectedShelves);

            // Check every shelf before adding any rentals.
            foreach (Shelf shelf in shelvesToRent)
            {
                if (!Shelves.Contains(shelf) ||
                    !_rentalService.IsShelfAvailable(
                        shelf, startDate, endDate, Rentals))
                {
                    RentalMessage =
                        $"Reol {shelf.ShelfNumber} er ikke tilgængelig i den valgte periode.";
                    return;
                }
            }

            // All validation passed. Create one rental for each selected shelf.
            foreach (Shelf shelf in shelvesToRent)
            {
                Rental rental = new(tenant, shelf)
                {
                    StartDate = startDate,
                    EndDate = endDate,
                    MonthlyRent = rentPerShelf
                };
                _rentalRepository.Add(rental);
                Rentals.Add(rental);
            }

            // Reset the draft for the next creation.
            IsCustomPrice = false;
            SelectedShelves.Clear();
            SelectedTenant = null;
            SelectedShelf = null;
            EndDate = null;
            StartDate = DateTime.Today;

            Refresh();

            // Set feedback last because refreshing clears earlier messages.
            RentalConfirmationMessage = $"{shelvesToRent.Count} lejemål blev oprettet.";
        }

        private bool CanUseStandardPrice(object? parameter)
        {
            return IsCustomPrice;
        }

        private void UseStandardPrice(object? parameter)
        {
            if (!CanUseStandardPrice(parameter))
            {
                return;
            }

            IsCustomPrice = false;
            // Reset the monthly rent to the standard price
            MonthlyRent = StandardMonthlyRent.ToString("0.00");
        }

        private bool CanEnableCustomPrice(object? parameter)
        {
            return SelectedShelves.Count > 0 && !IsCustomPrice;
        }

        private void EnableCustomPrice(object? parameter)
        {
            if (!CanEnableCustomPrice(parameter))
            {
                return;
            }

            IsCustomPrice = true;

        }

        private bool CanRemoveShelfFromSelection(object? parameter)
        {
            return parameter is Shelf shelf && SelectedShelves.Contains(shelf);
        }

        private void RemoveShelfFromSelection(object? parameter)
        {
            if (parameter is not Shelf shelf || !SelectedShelves.Contains(shelf))
            {
                return;
            }

            SelectedShelves.Remove(shelf);
            RefreshAvailableShelves();
        }

        private bool CanAddShelfToSelection(object? parameter)
        {
            return SelectedShelf is not null
                && !SelectedShelves.Contains(SelectedShelf)
                && AvailableShelves.Contains(SelectedShelf);
        }

        private void AddShelfToSelection(object? parameter)
        {
            if (SelectedShelf is null ||
                !CanAddShelfToSelection(parameter))
            {
                return;
            }

            Shelf shelf = SelectedShelf;
            SelectedShelves.Add(shelf);
            RefreshAvailableShelves();
        }

        public void Refresh()
        {
            TerminationMessage = string.Empty;
            TerminationConfirmationMessage = string.Empty;
            RentalConfirmationMessage = string.Empty;
            RefreshActiveTenants();
            RefreshAvailableShelves();
            RefreshPrice();
            RefreshRentalRows();
        }

        private void RefreshActiveTenants()
        {
            ActiveTenants.Clear();

            foreach (var tenant in Tenants)
            {
                if (tenant.IsActive)
                {
                    ActiveTenants.Add(tenant);
                }
            }

            // If the currently selected tenant is no longer active, clear the selection
            if (SelectedTenant is not null &&
                !ActiveTenants.Contains(SelectedTenant))
            {
                SelectedTenant = null;
            }
        }

        private void RefreshAvailableShelves()
        {
            AvailableShelves.Clear();

            // If the start date is not set, we cannot determine available shelves, so we clear the selection and return
            if (StartDate is null)
            {
                SelectedShelf = null;
                SelectedShelves.Clear();
                RentalMessage = "Vælg en startdato for at se tilgængelige reoler.";
                return;
            }

            if (EndDate.HasValue &&
                EndDate.Value.Date < StartDate.Value.Date)
            {
                SelectedShelf = null;
                SelectedShelves.Clear();
                RentalMessage = "Slutdatoen kan ikke være før startdatoen.";
                return;
            }

            RentalMessage = string.Empty;

            // Handles case: A shelf is selected, then the date is changed, and the shelf is no longer available. It should be removed from the selection.
            // Going backwards avoids skipping elements when removing from the collection.
            for (int i = SelectedShelves.Count - 1; i >= 0; i--)
            {
                Shelf shelf = SelectedShelves[i];

                bool isAvailable = Shelves.Contains(shelf) &&
                    _rentalService.IsShelfAvailable(
                        shelf, StartDate.Value, EndDate, Rentals);

                if (!isAvailable)
                {
                    SelectedShelves.RemoveAt(i);
                    RentalMessage =
                        "En eller flere valgte reoler blev fjernet, da de ikke længere er tilgængelige.";
                }
            }

            foreach (var shelf in Shelves)
            {
                bool isAvailable = _rentalService.IsShelfAvailable(
                    shelf, StartDate.Value, EndDate, Rentals);

                // If the shelf is available and not already selected, add it to the available shelves
                if (isAvailable && !SelectedShelves.Contains(shelf))
                {
                    AvailableShelves.Add(shelf);
                }
            }

            // If the currently selected shelf is no longer available, clear the selection
            if (SelectedShelf is not null &&
                !AvailableShelves.Contains(SelectedShelf))
            {
                SelectedShelf = null;
            }
        }

        private void RefreshPrice()
        {
            if (SelectedTenant is null ||
                StartDate is null ||
                SelectedShelves.Count == 0)
            {
                StandardMonthlyRent = 0m;
                if (!IsCustomPrice)
                {
                    MonthlyRent = "0.00";
                }

                return;
            }

            int existingShelfCount = _rentalService.GetRentedShelfCountForTenant(
                SelectedTenant, StartDate.Value, Rentals);

            int totalShelfCount = existingShelfCount + SelectedShelves.Count;

            StandardMonthlyRent =
                _rentalService.GetStandardMonthlyRentPerShelf(totalShelfCount);

            if (!IsCustomPrice)
            {
                MonthlyRent = StandardMonthlyRent.ToString("0.00");
            }
        }

        private void RefreshRentalRows()
        {
            SelectedRentalRow = null;
            RentalRows.Clear();

            DateTime today = DateTime.Today;
            foreach (var rental in Rentals)
            {
                bool matchesFilter = SelectedRentalStatusFilter switch
                {
                    RentalStatusFilter.All => true,
                    RentalStatusFilter.Active =>
                        rental.StartDate.Date <= today &&
                       (rental.EndDate is null ||
                        rental.EndDate.Value.Date >= today),
                    RentalStatusFilter.Upcoming =>
                        rental.StartDate.Date > today,
                    RentalStatusFilter.Historical =>
                        rental.EndDate is not null &&
                        rental.EndDate.Value.Date < today,
                    _ => false
                };
                if (matchesFilter)
                {
                    RentalRows.Add(new RentalRowViewModel(rental));
                }
            }
        }
    }
}
