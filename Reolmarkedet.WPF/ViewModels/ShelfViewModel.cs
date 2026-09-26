using Reolmarkedet.Core.Interfaces;
using Reolmarkedet.Core.Models;
using Reolmarkedet.Core.Services;
using Reolmarkedet.WPF.Commands;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.Common;

namespace Reolmarkedet.WPF.ViewModels
{
    public class ShelfViewModel : ViewModelBase
    {
        public ObservableCollection<Shelf> Shelves { get; } = new();
        public ObservableCollection<ShelfRowViewModel> VisibleShelves { get; } = new();
        public ObservableCollection<ShelfType> ShelfTypes { get; } = new();
        public ObservableCollection<Rental> Rentals { get; }

        private readonly RentalService _rentalService = new();
        private readonly IRepository<Shelf> _shelfRepository;
        private readonly IRepository<ShelfType> _shelfTypeRepository;

        private string _newShelfTypeName = string.Empty;
        private ShelfType? _newShelfType;
        private ShelfType? _shelfTypeToDelete;
        private string _shelfTypeMessage = string.Empty;
        private string _newShelfNumber = string.Empty;
        private string _shelfMessage = string.Empty;
        private string _shelfConfirmationMessage = string.Empty;
        private string _shelfTypeConfirmationMessage = string.Empty;
        private bool _showInactiveShelves;
        private ShelfRowViewModel? _selectedShelfRow;
        private ShelfStatusFilter _selectedStatusFilter = ShelfStatusFilter.All;

        public ShelfStatusFilter SelectedStatusFilter
        {
            get => _selectedStatusFilter;
            set
            {
                if (_selectedStatusFilter != value)
                {
                    _selectedStatusFilter = value;
                    ShelfConfirmationMessage = string.Empty;
                    ShelfMessage = string.Empty;
                    OnPropertyChanged();

                    ApplyShelfFilter();
                }
            }
        }

        public string NewShelfTypeName
        {
            get => _newShelfTypeName;
            set
            {
                if (_newShelfTypeName != value)
                {
                    _newShelfTypeName = value;
                    ShelfTypeConfirmationMessage = string.Empty;
                    ShelfTypeMessage = string.Empty;
                    OnPropertyChanged();

                    AddShelfTypeCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public ShelfType? ShelfTypeToDelete
        {
            get => _shelfTypeToDelete;
            set
            {
                if (_shelfTypeToDelete != value)
                {
                    _shelfTypeToDelete = value;
                    ShelfTypeConfirmationMessage = string.Empty;
                    ShelfTypeMessage = string.Empty;
                    OnPropertyChanged();

                    DeleteShelfTypeCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public string ShelfTypeMessage
        {
            get => _shelfTypeMessage;
            set
            {
                _shelfTypeMessage = value;
                OnPropertyChanged();
            }
        }

        public string NewShelfNumber
        {
            get => _newShelfNumber;
            set
            {
                if (_newShelfNumber != value)
                {
                    _newShelfNumber = value;
                    ShelfConfirmationMessage = string.Empty;
                    ShelfTypeConfirmationMessage = string.Empty;
                    OnPropertyChanged();
                    ValidateShelfNumber();
                    AddShelfCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public string ShelfMessage
        {
            get => _shelfMessage;
            set
            {
                _shelfMessage = value;
                ShelfConfirmationMessage = string.Empty;
                OnPropertyChanged();
            }
        }
        public string ShelfConfirmationMessage
        {
            get => _shelfConfirmationMessage;
            set
            {
                _shelfConfirmationMessage = value;
                OnPropertyChanged();
            }
        }
        public string ShelfTypeConfirmationMessage
        {
            get => _shelfTypeConfirmationMessage;
            set
            {
                _shelfTypeConfirmationMessage = value;
                OnPropertyChanged();
            }
        }

        public ShelfType? NewShelfType
        {
            get => _newShelfType;
            set
            {
                if (_newShelfType != value)
                {
                    _newShelfType = value;
                    ShelfTypeMessage = string.Empty;
                    ShelfConfirmationMessage = string.Empty;
                    OnPropertyChanged();

                    AddShelfCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public bool ShowInactiveShelves
        {
            get => _showInactiveShelves;
            set
            {
                if (_showInactiveShelves != value)
                {
                    _showInactiveShelves = value;
                    ShelfConfirmationMessage = string.Empty;
                    ShelfMessage = string.Empty;
                    OnPropertyChanged();
                    ApplyShelfFilter();
                }
            }
        }

        public ShelfRowViewModel? SelectedShelfRow
        {
            get => _selectedShelfRow;
            set
            {
                if (_selectedShelfRow != value)
                {
                    _selectedShelfRow = value;
                    ShelfConfirmationMessage = string.Empty;
                    ShelfMessage = string.Empty;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(HasSelectedShelf));
                    OnPropertyChanged(nameof(ShowDeactivateButton));
                    OnPropertyChanged(nameof(ShowReactivateButton));

                    DeleteShelfCommand.RaiseCanExecuteChanged();
                    DeactivateShelfCommand.RaiseCanExecuteChanged();
                    ReactivateShelfCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public bool HasSelectedShelf => SelectedShelfRow is not null;
        public bool ShowDeactivateButton =>
            SelectedShelfRow is not null && SelectedShelfRow.Shelf.IsActive;
        public bool ShowReactivateButton =>
            SelectedShelfRow is not null && !SelectedShelfRow.Shelf.IsActive;

        public RelayCommand AddShelfTypeCommand { get; }
        public RelayCommand DeleteShelfTypeCommand { get; }
        public RelayCommand AddShelfCommand { get; }
        public RelayCommand DeleteShelfCommand { get; }
        public RelayCommand DeactivateShelfCommand { get; }
        public RelayCommand ReactivateShelfCommand { get; }

        public ShelfViewModel(
            ObservableCollection<Rental> rentals,
            IRepository<Shelf> shelfRepository,
            IRepository<ShelfType> shelfTypeRepository)
        {
            Rentals = rentals;
            _shelfRepository = shelfRepository;
            _shelfTypeRepository = shelfTypeRepository;

            // Load shelf types and shelves from the repositories
            foreach (ShelfType shelfType in _shelfTypeRepository.GetAll())
            {
                ShelfTypes.Add(shelfType);
            }

            foreach (Shelf shelf in _shelfRepository.GetAll())
            {
                ShelfType? matchingShelfType = null;

                // Find the corresponding ShelfType from the ShelfTypes collection
                foreach (ShelfType shelfType in ShelfTypes)
                {
                    if (shelfType.ShelfTypeId == shelf.ShelfType.ShelfTypeId)
                    {
                        matchingShelfType = shelfType;
                        break;
                    }
                }

                if (matchingShelfType is null)
                {
                    throw new InvalidOperationException(
                        "Shelf type could not be found");
                }

                shelf.ShelfType = matchingShelfType;
                shelf.PropertyChanged += OnShelfPropertyChanged;
                Shelves.Add(shelf);
            }

            AddShelfTypeCommand = new RelayCommand(AddShelfType, CanAddShelfType);
            DeleteShelfTypeCommand = new RelayCommand(DeleteShelfType, CanDeleteShelfType);
            AddShelfCommand = new RelayCommand(AddShelf, CanAddShelf);
            DeleteShelfCommand = new RelayCommand(DeleteShelf, CanDeleteShelf);
            DeactivateShelfCommand = new RelayCommand(DeactivateShelf, CanDeactivateShelf);
            ReactivateShelfCommand = new RelayCommand(ReactivateShelf, CanReactivateShelf);

            // Refreshes the list of visible shelves whenever the Shelves or Rentals collections change
            Shelves.CollectionChanged += (_, _) => ApplyShelfFilter();
            Rentals.CollectionChanged += (_, _) => ApplyShelfFilter();

            ApplyShelfFilter();
        }

        // Persists shelf-type changes made directly through the DataGrid dropdown.
        private void OnShelfPropertyChanged(
            object? sender, PropertyChangedEventArgs e)
        {
            if (sender is Shelf shelf &&
                e.PropertyName == nameof(Shelf.ShelfType))
            {
                _shelfRepository.Update(shelf);
            }
        }

        private bool CanReactivateShelf(object? parameter)
        {
            return SelectedShelfRow is not null &&
                !SelectedShelfRow.Shelf.IsActive;
        }

        private void ReactivateShelf(object? parameter)
        {
            ShelfTypeMessage = string.Empty;
            ShelfTypeConfirmationMessage = string.Empty;
            ShelfConfirmationMessage = string.Empty;

            if (SelectedShelfRow is null)
            {
                return;
            }

            Shelf shelf = SelectedShelfRow.Shelf;
            if (shelf.IsActive)
            {
                return;
            }

            shelf.IsActive = true;
            try
            {
                _shelfRepository.Update(shelf);
            }
            catch (DbException)
            {
                shelf.IsActive = false; // Revert the change if the update fails
                ShelfMessage = "Reolen kunne ikke genaktiveres i databasen. Prøv igen.";
                return;
            }

            ShelfMessage = string.Empty;
            ApplyShelfFilter();
            ShelfConfirmationMessage = "Reolen er blevet reaktiveret.";
        }

        private bool CanDeactivateShelf(object? parameter)
        {
            return SelectedShelfRow is not null && SelectedShelfRow.Shelf.IsActive;
        }

        private void DeactivateShelf(object? parameter)
        {
            ShelfTypeMessage = string.Empty;
            ShelfTypeConfirmationMessage = string.Empty;
            ShelfConfirmationMessage = string.Empty;

            if (SelectedShelfRow is null)
            {
                return;
            }

            Shelf shelf = SelectedShelfRow.Shelf;

            if (!shelf.IsActive)
            {
                return;
            }

            bool hasCurrentOrFutureRentals =
                _rentalService.HasCurrentOrFutureRentalsForShelf(
                    shelf, DateTime.Today, Rentals);

            if (hasCurrentOrFutureRentals)
            {
                ShelfMessage = "Reolen kan ikke deaktiveres, da den har igangværende eller fremtidige lejemål.";
                return;
            }

            shelf.IsActive = false;
            try
            {
                _shelfRepository.Update(shelf);
            }
            catch (DbException)
            {
                shelf.IsActive = true; // Revert the change if the update fails
                ShelfMessage = "Reolen kunne ikke deaktiveres i databasen. Prøv igen.";
                return;
            }

            ShelfMessage = string.Empty;
            ApplyShelfFilter();
            ShelfConfirmationMessage = "Reolen er blevet deaktiveret.";

        }

        private bool CanDeleteShelf(object? parameter)
        {
            return SelectedShelfRow is not null;
        }

        private void DeleteShelf(object? parameter)
        {
            ShelfTypeMessage = string.Empty;
            ShelfTypeConfirmationMessage = string.Empty;
            ShelfConfirmationMessage = string.Empty;

            if (SelectedShelfRow is null)
            {
                return;
            }

            Shelf shelf = SelectedShelfRow.Shelf;
            if (_rentalService.HasRentalsForShelf(shelf, Rentals))
            {
                ShelfMessage = "Reolen kan ikke slettes, da den har tilknyttede lejemål.";
                return;
            }

            try
            {
                _shelfRepository.Delete(shelf.ShelfId);
            }
            catch (DbException)
            {
                ShelfMessage = "Reolen kunne ikke slettes i databasen. Prøv igen.";
                return;
            }
            shelf.PropertyChanged -= OnShelfPropertyChanged;
            Shelves.Remove(SelectedShelfRow.Shelf);

            SelectedShelfRow = null;
            ShelfMessage = string.Empty;
            ShelfConfirmationMessage = "Reolen er blevet slettet.";
        }

        private bool CanAddShelf(object? parameter)
        {
            return int.TryParse(NewShelfNumber, out int shelfNumber)
                && shelfNumber > 0
                && NewShelfType is not null;
        }

        private void AddShelf(object? parameter)
        {
            ShelfTypeMessage = string.Empty;
            ShelfTypeConfirmationMessage = string.Empty;
            ShelfConfirmationMessage = string.Empty;


            if (!int.TryParse(NewShelfNumber, out int shelfNumber) ||
                shelfNumber <= 0 ||
                NewShelfType is null)
            {
                return;
            }

            foreach (var shelf in Shelves)
            {
                if (shelf.ShelfNumber == shelfNumber)
                {
                    ShelfMessage = "Reolnummeret findes allerede.";
                    return;
                }
            }

            Shelf newShelf = new Shelf(NewShelfType)
            {
                ShelfNumber = shelfNumber
            };

            try
            {
                _shelfRepository.Add(newShelf);
            }
            catch (DbException)
            {
                ShelfMessage = "Reolen kunne ikke oprettes i databasen. Prøv igen.";
                return;
            }
            newShelf.PropertyChanged += OnShelfPropertyChanged;
            Shelves.Add(newShelf);


            NewShelfNumber = string.Empty;
            ShelfMessage = string.Empty;
            NewShelfType = null;
            ShelfConfirmationMessage = "Reolen er blevet tilføjet.";
        }

        private void ValidateShelfNumber()
        {
            if (string.IsNullOrWhiteSpace(NewShelfNumber))
            {
                ShelfMessage = string.Empty;
            }
            else if (!int.TryParse(NewShelfNumber, out int number)
                     || number <= 0)
            {
                ShelfMessage = "Reolnummeret skal være et helt tal større end 0.";
            }
            else
            {
                ShelfMessage = string.Empty;
            }
        }

        private bool CanDeleteShelfType(object? parameter)
        {
            return ShelfTypeToDelete is not null;
        }
        private void DeleteShelfType(object? parameter)
        {
            ShelfMessage = string.Empty;
            ShelfConfirmationMessage = string.Empty;
            ShelfTypeConfirmationMessage = string.Empty;

            if (ShelfTypeToDelete is null)
            {
                return;
            }

            foreach (var shelf in Shelves)
            {
                if (shelf.ShelfType == ShelfTypeToDelete)
                {
                    ShelfTypeMessage = "Reoltypen bruges af en reol og kan ikke slettes.";
                    return;
                }

            }

            ShelfType shelfType = ShelfTypeToDelete;

            try
            {
                _shelfTypeRepository.Delete(shelfType.ShelfTypeId);
            }
            catch (DbException)
            {
                ShelfTypeMessage = "Reoltypen kunne ikke slettes i databasen. Prøv igen.";
                return;
            }

            ShelfTypes.Remove(shelfType);

            ShelfTypeToDelete = null;
            ShelfTypeMessage = string.Empty;
            ShelfTypeConfirmationMessage = "Reoltype er blevet slettet.";
        }

        private void AddShelfType(object? parameter)
        {
            ShelfMessage = string.Empty;
            ShelfConfirmationMessage = string.Empty;
            ShelfTypeConfirmationMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(NewShelfTypeName))
            {
                return;
            }

            string shelfTypeName = NewShelfTypeName.Trim();

            foreach (ShelfType existingShelfTypes in ShelfTypes)
            {
                if (string.Equals(
                    existingShelfTypes.Name,
                    shelfTypeName,
                    StringComparison.OrdinalIgnoreCase))
                {
                    ShelfTypeMessage = "Reoltypen findes allerede.";
                    return;
                }
            }

            ShelfType shelfType = new ShelfType()
            {
                Name = shelfTypeName
            };

            try
            {
                _shelfTypeRepository.Add(shelfType);
            }
            catch (DbException)
            {
                ShelfTypeMessage = "Reoltypen kunne ikke oprettes i databasen. Prøv igen.";
                return;
            }

            ShelfTypes.Add(shelfType);
            NewShelfTypeName = string.Empty;
            ShelfTypeMessage = string.Empty;
            ShelfTypeConfirmationMessage = "Reoltype er blevet oprettet.";
        }


        private bool CanAddShelfType(object? parameter)
        {
            return !string.IsNullOrWhiteSpace(NewShelfTypeName);
        }

        private void ApplyShelfFilter()
        {
            SelectedShelfRow = null;
            VisibleShelves.Clear();

            var dateToday = DateTime.Today;

            foreach (var shelf in Shelves)
            {
                bool matchesActivity =
                    (!ShowInactiveShelves && shelf.IsActive) ||
                    (ShowInactiveShelves && !shelf.IsActive);

                if (!matchesActivity)
                {
                    continue; // Skip shelves that don't match the activity filter
                }

                var currentRental =
                    _rentalService.GetCurrentRentalByShelfAndDate(shelf, dateToday, Rentals);
                var status =
                    _rentalService.GetShelfStatus(shelf, dateToday, Rentals);

                // Determine if the shelf matches the selected filter
                bool matchesFilter =
                    SelectedStatusFilter == ShelfStatusFilter.All ||
                    (SelectedStatusFilter == ShelfStatusFilter.Available &&
                     status == ShelfStatus.Available) ||
                    (SelectedStatusFilter == ShelfStatusFilter.Rented &&
                     status == ShelfStatus.Rented) ||
                     (SelectedStatusFilter == ShelfStatusFilter.TerminationPending &&
                     status == ShelfStatus.TerminationPending);

                if (ShowInactiveShelves || matchesFilter)
                {
                    VisibleShelves.Add(new ShelfRowViewModel(shelf, currentRental, status));
                }
            }
        }

        public void Refresh()
        {
            ShelfTypeMessage = string.Empty;
            ShelfTypeConfirmationMessage = string.Empty;
            ShelfMessage = string.Empty;
            ShelfConfirmationMessage = string.Empty;
            ApplyShelfFilter();
        }
    }
}
