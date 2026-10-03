using Reolmarkedet.Core.Interfaces;
using Reolmarkedet.Core.Models;
using Reolmarkedet.Core.Services;
using Reolmarkedet.WPF.Commands;
using Reolmarkedet.WPF.Services;
using Reolmarkedet.WPF.ViewModels.enums;
using System.Collections.ObjectModel;
using System.Data.Common;
using System.Net.Mail;

namespace Reolmarkedet.WPF.ViewModels
{
    public class TenantViewModel : ViewModelBase
    {
        // Observable collection to hold the list of tenants
        public ObservableCollection<Tenant> Tenants { get; } = new();
        public ObservableCollection<Tenant> VisibleTenants { get; } = new();
        public ObservableCollection<Rental> Rentals { get; }
        public ObservableCollection<RentalRowViewModel> TenantRentalRows { get; } = new();

        private readonly RentalService _rentalService = new();
        private readonly IConfirmationService _confirmationService;
        private readonly IRepository<Tenant> _tenantRepository;

        private string _searchText = string.Empty;
        private string _name = string.Empty;
        private string _phoneNumber = string.Empty;
        private string _email = string.Empty;
        private string _bankRegistrationNumber = string.Empty;
        private string _bankAccountNumber = string.Empty;
        private Tenant? _selectedTenant;
        private bool _showInactiveTenants;
        private string _validationMessage = string.Empty;
        private string _confirmationMessage = string.Empty;

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

        public string Name
        {
            get => _name;
            set
            {
                if (_name != value)
                {
                    _name = value;
                    ConfirmationMessage = string.Empty;
                    OnPropertyChanged();
                    UpdateTenantCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public string PhoneNumber
        {
            get => _phoneNumber;
            set
            {
                if (_phoneNumber != value)
                {
                    _phoneNumber = value;
                    ConfirmationMessage = string.Empty;
                    OnPropertyChanged();
                    UpdateTenantCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public string Email
        {
            get => _email;
            set
            {
                if (_email != value)
                {
                    _email = value;
                    ConfirmationMessage = string.Empty;
                    OnPropertyChanged();
                    UpdateTenantCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public string BankRegistrationNumber
        {
            get => _bankRegistrationNumber;
            set
            {
                if (_bankRegistrationNumber != value)
                {
                    _bankRegistrationNumber = value;
                    ConfirmationMessage = string.Empty;
                    OnPropertyChanged();
                    UpdateTenantCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public string BankAccountNumber
        {
            get => _bankAccountNumber;
            set
            {
                if (_bankAccountNumber != value)
                {
                    _bankAccountNumber = value;
                    ConfirmationMessage = string.Empty;
                    OnPropertyChanged();
                    UpdateTenantCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public Tenant? SelectedTenant
        {
            get => _selectedTenant;
            set
            {
                if (_selectedTenant != value)
                {
                    _selectedTenant = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(FormTitle));
                    OnPropertyChanged(nameof(IsEditing));
                    OnPropertyChanged(nameof(ShowCreateButton));
                    OnPropertyChanged(nameof(ShowDeactivateButton));
                    OnPropertyChanged(nameof(ShowReactivateButton));
                    OnPropertyChanged(nameof(ShowInactivePrompt));
                    ValidationMessage = string.Empty;
                    ConfirmationMessage = string.Empty; // Clear confirmation message when a tenant is selected

                    if (_selectedTenant is not null)
                    {
                        Name = _selectedTenant.Name;
                        PhoneNumber = _selectedTenant.PhoneNumber ?? string.Empty;
                        Email = _selectedTenant.Email ?? string.Empty;
                        BankRegistrationNumber = _selectedTenant.BankRegistrationNumber ?? string.Empty;
                        BankAccountNumber = _selectedTenant.BankAccountNumber ?? string.Empty;
                    }
                    else // Clear the form fields when no tenant is selected
                    {
                        ClearFormFields();
                    }

                    RefreshTenantRentals();

                    AddTenantCommand.RaiseCanExecuteChanged();
                    UpdateTenantCommand.RaiseCanExecuteChanged();
                    CancelUpdateTenantCommand.RaiseCanExecuteChanged();
                    DeleteTenantCommand.RaiseCanExecuteChanged();
                    DeactivateTenantCommand.RaiseCanExecuteChanged();
                    ReactivateTenantCommand.RaiseCanExecuteChanged();

                }
            }
        }

        public bool ShowInactiveTenants
        {
            get => _showInactiveTenants;
            set
            {
                if (_showInactiveTenants != value)
                {
                    _showInactiveTenants = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(ShowCreateButton));
                    OnPropertyChanged(nameof(ShowInactivePrompt));
                    OnPropertyChanged(nameof(FormTitle));
                    SelectedTenant = null; // Clear the selected tenant when toggling the filter
                    ValidationMessage = string.Empty;
                    ConfirmationMessage = string.Empty;
                    ApplySearch();
                }
            }
        }

        public int ActiveShelfCount =>
            TenantRentalRows.Count(row => row.Status == RentalStatus.Active);

        public decimal CurrentMonthlyRent =>
            TenantRentalRows
            .Where(row => row.Status == RentalStatus.Active)
            .Sum(row => row.MonthlyRent);

        public string ValidationMessage
        {
            get => _validationMessage;
            set
            {
                if (_validationMessage != value)
                {
                    _validationMessage = value;
                    OnPropertyChanged();
                }
            }
        }

        public string ConfirmationMessage
        {
            get => _confirmationMessage;
            set
            {
                if (_confirmationMessage != value)
                {
                    _confirmationMessage = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsEditing => SelectedTenant is not null;
        public bool ShowCreateButton => !IsEditing && !ShowInactiveTenants;
        public bool ShowDeactivateButton => SelectedTenant is not null && SelectedTenant.IsActive;
        public bool ShowReactivateButton => SelectedTenant is not null && !SelectedTenant.IsActive;
        public bool ShowInactivePrompt => ShowInactiveTenants && !IsEditing;
        public string FormTitle
        {
            get
            {
                if (IsEditing)
                {
                    return $"Rediger reollejer: {SelectedTenant?.Name}";
                }
                if (ShowInactivePrompt)
                {
                    if (VisibleTenants.Count == 0)
                    {
                        return "Ingen reollejere fundet";
                    }

                    return "Vælg en deaktiveret reollejer";
                }
                else
                {
                    return "Opret ny reollejer";
                }
            }
        }


        public RelayCommand AddTenantCommand { get; }
        public RelayCommand UpdateTenantCommand { get; }
        public RelayCommand CancelUpdateTenantCommand { get; }
        public RelayCommand DeleteTenantCommand { get; }
        public RelayCommand DeactivateTenantCommand { get; }
        public RelayCommand ReactivateTenantCommand { get; }

        public TenantViewModel(
            ObservableCollection<Rental> rentals,
            IRepository<Tenant> tenantRepository,
            IConfirmationService confirmationService)
        {
            Rentals = rentals;
            _tenantRepository = tenantRepository;
            _confirmationService = confirmationService;

            // Load tenants from the repository into the Tenants collection
            foreach (var tenant in _tenantRepository.GetAll())
            {
                Tenants.Add(tenant);
            }
            ApplySearch();

            AddTenantCommand = new RelayCommand(AddTenant, CanAddTenant);
            UpdateTenantCommand = new RelayCommand(UpdateTenant, CanUpdateTenant);
            CancelUpdateTenantCommand = new RelayCommand(CancelUpdateTenant, CanCancelUpdateTenant);
            DeleteTenantCommand = new RelayCommand(DeleteTenant, CanDeleteTenant);
            DeactivateTenantCommand = new RelayCommand(DeactivateTenant, CanDeactivateTenant);
            ReactivateTenantCommand = new RelayCommand(ReactivateTenant, CanReactivateTenant);
        }

        private bool CanReactivateTenant(object? parameter)
        {
            return SelectedTenant is not null && !SelectedTenant.IsActive;
        }

        private void ReactivateTenant(object? obj)
        {
            ConfirmationMessage = string.Empty;
            if (SelectedTenant is null)
            {
                return;
            }

            Tenant tenant = SelectedTenant;

            if (tenant.IsActive)
            {
                return;
            }

            tenant.IsActive = true;

            try
            {
                _tenantRepository.Update(tenant);
            }
            catch (DbException)
            {
                tenant.IsActive = false; // Revert the change if the update fails
                ValidationMessage = "Reollejeren kunne ikke genaktiveres i databasen. Prøv igen.";
                return;
            }

            SelectedTenant = null;
            ValidationMessage = string.Empty;
            ConfirmationMessage = "Reollejeren er blevet genaktiveret.";
            ApplySearch();
        }


        private bool CanDeactivateTenant(object? parameter)
        {
            return SelectedTenant is not null && SelectedTenant.IsActive;
        }

        private void DeactivateTenant(object? parameter)
        {
            ConfirmationMessage = string.Empty;
            if (SelectedTenant is null)
            {
                return;
            }

            Tenant tenant = SelectedTenant;

            if (!tenant.IsActive)
            {
                return;
            }

            bool hasCurrentOrFutureRentals =
                _rentalService.HasCurrentOrFutureRentalsForTenant(
                    tenant, DateTime.Now, Rentals);

            if (hasCurrentOrFutureRentals)
            {
                ValidationMessage =
                    "Reollejeren har nuværende eller kommende lejemål og kan ikke deaktiveres.";
                return;
            }
            if (!_confirmationService.Confirm($"Vil du deaktivere reollejeren {tenant.Name}?"))
            {
                return;
            }

            tenant.IsActive = false;

            try
            {
                _tenantRepository.Update(tenant);
            }
            catch (DbException)
            {
                tenant.IsActive = true;
                ValidationMessage = "Reollejeren kunne ikke deaktiveres i databasen. Prøv igen.";
                return;
            }

            SelectedTenant = null;
            ValidationMessage = string.Empty;
            ConfirmationMessage = "Reollejeren er blevet deaktiveret.";
            ApplySearch();

        }

        private bool CanAddTenant(object? parameter)
        {
            return SelectedTenant is null;
        }
        private void AddTenant(object? parameter)
        {
            ConfirmationMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(Name))
            {
                ValidationMessage = "Navn må ikke være tomt.";
                return;
            }
            if (!ValidateContactDetails() ||
                !ValidateBankDetails())
            {
                return;
            }

            ValidationMessage = string.Empty;

            Tenant tenant = new Tenant()
            {
                Name = this.Name,
                Email = this.Email,
                PhoneNumber = this.PhoneNumber,
                BankRegistrationNumber = this.BankRegistrationNumber.Trim(),
                BankAccountNumber = this.BankAccountNumber.Trim()
            };

            try
            {
                _tenantRepository.Add(tenant);
            }
            catch (DbException)
            {
                ValidationMessage = "Reollejeren kunne ikke oprettes i databasen. Prøv igen.";
                return;
            }

            Tenants.Add(tenant);
            ApplySearch();

            SelectedTenant = null;
            ClearFormFields();
            ConfirmationMessage = "Reollejeren er blevet oprettet.";
        }

        private bool CanUpdateTenant(object? parameter)
        {
            return SelectedTenant is not null &&
                   (Name != SelectedTenant.Name ||
                    PhoneNumber != (SelectedTenant.PhoneNumber ?? string.Empty) ||
                    Email != (SelectedTenant.Email ?? string.Empty) ||
                    BankRegistrationNumber != (SelectedTenant.BankRegistrationNumber ?? string.Empty) ||
                    BankAccountNumber != (SelectedTenant.BankAccountNumber ?? string.Empty));
        }

        private void UpdateTenant(object? parameter)
        {
            ConfirmationMessage = string.Empty;
            Tenant? tenant = SelectedTenant;
            if (tenant is null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(Name))
            {
                ValidationMessage =
                    "Navn må ikke være tomt. Angiv et navn, eller annuller redigeringen.";
                return;
            }
            if (!ValidateContactDetails() ||
                !ValidateBankDetails())
            {
                return;
            }

            ValidationMessage = string.Empty;

            // store the original values in case the update fails
            string originalName = tenant.Name;
            string? originalPhoneNumber = tenant.PhoneNumber;
            string? originalEmail = tenant.Email;
            string? originalBankRegistrationNumber = tenant.BankRegistrationNumber;
            string? originalBankAccountNumber = tenant.BankAccountNumber;

            tenant.Name = Name;
            tenant.PhoneNumber =
                string.IsNullOrWhiteSpace(PhoneNumber) ? null : PhoneNumber;
            tenant.Email =
                string.IsNullOrWhiteSpace(Email) ? null : Email;
            tenant.BankRegistrationNumber =
                string.IsNullOrWhiteSpace(BankRegistrationNumber) ? null : BankRegistrationNumber.Trim();
            tenant.BankAccountNumber =
                string.IsNullOrWhiteSpace(BankAccountNumber) ? null : BankAccountNumber.Trim();

            try
            {
                _tenantRepository.Update(tenant);
            }
            catch (DbException)
            {
                tenant.Name = originalName;
                tenant.PhoneNumber = originalPhoneNumber;
                tenant.Email = originalEmail;
                tenant.BankRegistrationNumber = originalBankRegistrationNumber;
                tenant.BankAccountNumber = originalBankAccountNumber;
                ValidationMessage = "Ændringerne kunne ikke gemmes i databasen. Prøv igen.";
                return;
            }

            ApplySearch();
            SelectedTenant = null;
            ClearFormFields();

            ConfirmationMessage = "Reollejeren er blevet opdateret.";
        }

        private bool CanCancelUpdateTenant(object? parameter)
        {
            return SelectedTenant is not null;
        }

        private void CancelUpdateTenant(object? parameter)
        {
            SelectedTenant = null;
            ClearFormFields();
        }

        private bool CanDeleteTenant(object? parameter)
        {
            return SelectedTenant is not null;
        }

        private void DeleteTenant(object? parameter)
        {
            ConfirmationMessage = string.Empty;
            Tenant? tenant = SelectedTenant;
            if (tenant is null)
            {
                return;
            }

            if (_rentalService.HasRentalsForTenant(tenant, Rentals))
            {
                ValidationMessage = "Reollejeren har tilknyttede lejemål og kan ikke slettes.";
                return;
            }
            if (!_confirmationService.Confirm($"Vil du slette reollejeren {tenant.Name}?"))
            {
                return;
            }

            try
            {
                _tenantRepository.Delete(tenant.TenantId);
            }
            catch (DbException)
            {
                ValidationMessage = "Reollejeren kunne ikke slettes fra databasen. Prøv igen.";
                return;
            }

            Tenants.Remove(tenant);

            ApplySearch();
            SelectedTenant = null;
            ClearFormFields();
            ConfirmationMessage = "Reollejeren er blevet slettet.";
        }

        private void ClearFormFields()
        {
            Name = string.Empty;
            PhoneNumber = string.Empty;
            Email = string.Empty;
            ValidationMessage = string.Empty;
            ConfirmationMessage = string.Empty;
            BankRegistrationNumber = string.Empty;
            BankAccountNumber = string.Empty;
        }

        private void ApplySearch()
        {
            VisibleTenants.Clear();
            foreach (var tenant in Tenants)
            {
                // Check if the tenant matches the active/inactive filter
                bool matchesActivity =
                    (!ShowInactiveTenants && tenant.IsActive) ||
                    (ShowInactiveTenants && !tenant.IsActive);

                if (matchesActivity &&
                    (string.IsNullOrWhiteSpace(SearchText) ||
                    tenant.Name.Contains(SearchText.Trim(),
                        StringComparison.OrdinalIgnoreCase)))
                {
                    VisibleTenants.Add(tenant);
                }
            }

            OnPropertyChanged(nameof(FormTitle));
        }

        private bool ValidateContactDetails()
        {
            string? phoneError = GetPhoneNumberValidationMessage(PhoneNumber);

            if (phoneError is not null)
            {
                ValidationMessage = phoneError;
                return false;
            }

            string? emailError = GetEmailValidationMessage(Email);

            if (emailError is not null)
            {
                ValidationMessage = emailError;
                return false;
            }

            return true;
        }

        private bool ValidateBankDetails()
        {
            if (string.IsNullOrWhiteSpace(BankRegistrationNumber))
            {
                ValidationMessage = "Angiv registreringsnummer.";
                return false;
            }
            if (string.IsNullOrWhiteSpace(BankAccountNumber))
            {
                ValidationMessage = "Angiv kontonummer.";
                return false;
            }
            string trimmedRegistrationNumber = BankRegistrationNumber.Trim();
            string trimmedAccountNumber = BankAccountNumber.Trim();
            if (trimmedRegistrationNumber.Length != 4)
            {
                ValidationMessage = "Registreringsnummeret skal have 4 cifre.";
                return false;
            }

            foreach (char c in trimmedRegistrationNumber)
            {
                if (c < '0' || c > '9')
                {
                    ValidationMessage = "Registreringsnummeret indeholder ugyldige tegn. Brug kun cifre.";
                    return false;
                }
            }

            if (trimmedAccountNumber.Length > 10)
            {
                ValidationMessage = "Kontonummeret må højst have 10 cifre.";
                return false;
            }

            foreach (char c in trimmedAccountNumber)
            {
                if (c < '0' || c > '9')
                {
                    ValidationMessage = "Kontonummeret indeholder ugyldige tegn. Brug kun cifre.";
                    return false;
                }
            }

            return true;
        }

        private void RefreshTenantRentals()
        {
            TenantRentalRows.Clear();

            OnPropertyChanged(nameof(ActiveShelfCount));
            OnPropertyChanged(nameof(CurrentMonthlyRent));

            DateTime today = DateTime.Today;
            if (SelectedTenant is null)
            {
                return;
            }

            List<RentalRowViewModel> tenantRentalRows = new();
            foreach (var rental in Rentals)
            {
                if (rental.Tenant.TenantId == SelectedTenant.TenantId)
                {
                    DateTime priceDate = today;

                    if (rental.StartDate.Date > today) // future rentals price are calculated based on their start date
                    {
                        priceDate = rental.StartDate.Date;
                    }
                    else if (rental.EndDate.HasValue &&
                        rental.EndDate.Value.Date < today) // rental has already ended, price is calculated based on the end date
                    {
                        priceDate = rental.EndDate.Value.Date;
                    }

                    decimal monthlyRent = _rentalService.GetMonthlyRentForDate(
                        rental, priceDate, Rentals);
                    tenantRentalRows.Add(new RentalRowViewModel(rental, monthlyRent));
                }
            }

            // Sort the tenantRentalRows based on the defined status order
            RentalStatus[] statusOrder =
            {
                RentalStatus.Active,
                RentalStatus.Upcoming,
                RentalStatus.Historical
            };
            foreach (RentalStatus status in statusOrder)
            {
                tenantRentalRows
                    .Where(row => row.Status == status)
                    .ToList()
                    .ForEach(row => TenantRentalRows.Add(row));
            }

            OnPropertyChanged(nameof(ActiveShelfCount));
            OnPropertyChanged(nameof(CurrentMonthlyRent));
        }

        public void Refresh()
        {
            SelectedTenant = null;
            RefreshTenantRentals();
        }

        private string? GetPhoneNumberValidationMessage(string phoneNumber)
        {
            // The field is optional
            if (string.IsNullOrWhiteSpace(phoneNumber))
            {
                return null;
            }


            string cleanedPhoneNumber = phoneNumber
                    .Replace(" ", "")
                    .Replace("-", "")
                    .Replace("(", "")
                    .Replace(")", "");

            bool isInternational = cleanedPhoneNumber.StartsWith("+");

            string digits = isInternational
                ? cleanedPhoneNumber.Substring(1)
                : cleanedPhoneNumber;

            if (digits.Length == 0)
            {
                return isInternational
                    ? "Angiv landekode og telefonnummer efter +."
                    : "Et dansk telefonnummer uden landekode skal have 8 cifre.";
            }

            foreach (char character in digits)
            {
                if (character < '0' || character > '9')
                {
                    return "Telefonnummeret indeholder ugyldige tegn. Brug kun cifre, " +
                           "mellemrum, bindestreger eller parenteser samt + foran landekoden.";
                }
            }

            if (isInternational)
            {
                if (digits[0] == '0')
                {
                    return "Landekoden efter + må ikke begynde med 0.";
                }

                if (digits.Length > 15)
                {
                    return "Et internationalt telefonnummer må højst have " +
                           "15 cifre inklusivt landekoden.";
                }
            }
            else if (digits.Length != 8)
            {
                return "Et dansk telefonnummer uden landekode skal have 8 cifre.";
            }

            return null;
        }


        private string? GetEmailValidationMessage(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return null;
            }

            string cleanedEmail = email.Trim();

            if (!MailAddress.TryCreate(cleanedEmail, out MailAddress? address) ||
                !address.Address.Equals(cleanedEmail, StringComparison.OrdinalIgnoreCase))
            {
                return "Angiv en emailadresse i korrekt format, fx navn@eksempel.dk.";
            }

            return null;
        }
    }
}
