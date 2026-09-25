using Reolmarkedet.Core.Interfaces;
using Reolmarkedet.Core.Models;
using Reolmarkedet.Core.Services;
using Reolmarkedet.WPF.Commands;
using System.Collections.ObjectModel;
using System.Net.Mail;

namespace Reolmarkedet.WPF.ViewModels
{
    public class TenantViewModel : ViewModelBase
    {
        // Observable collection to hold the list of tenants
        public ObservableCollection<Tenant> Tenants { get; } = new();
        public ObservableCollection<Tenant> VisibleTenants { get; } = new();
        public ObservableCollection<Rental> Rentals { get; }

        private readonly RentalService _rentalService = new();
        private readonly IRepository<Tenant> _tenantRepository;

        private string _searchText = string.Empty;
        private string _name = string.Empty;
        private string _phoneNumber = string.Empty;
        private string _email = string.Empty;
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
                    }
                    else // Clear the form fields when no tenant is selected
                    {
                        ClearFormFields();
                    }

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
            IRepository<Tenant> tenantRepository)
        {
            Rentals = rentals;
            _tenantRepository = tenantRepository;

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
            _tenantRepository.Update(tenant);

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

            tenant.IsActive = false;
            _tenantRepository.Update(tenant);

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
            if (!ValidateContactDetails())
            {
                return;
            }

            ValidationMessage = string.Empty;

            Tenant tenant = new Tenant()
            {
                Name = Name,
                Email = Email,
                PhoneNumber = PhoneNumber,
            };

            _tenantRepository.Add(tenant);
            Tenants.Add(tenant);

            ApplySearch();

            SelectedTenant = null;
            ClearFormFields();
            ConfirmationMessage = "Reollejeren er blevet oprettet.";
        }

        private bool CanUpdateTenant(object? parameter)
        {
            return SelectedTenant is not null;
        }

        private void UpdateTenant(object? parameter)
        {
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
            if (!ValidateContactDetails())
            {
                return;
            }

            ValidationMessage = string.Empty;

            tenant.Name = Name;
            tenant.PhoneNumber = string.IsNullOrWhiteSpace(PhoneNumber) ? null : PhoneNumber;
            tenant.Email = string.IsNullOrWhiteSpace(Email) ? null : Email;

            _tenantRepository.Update(tenant);

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

            _tenantRepository.Delete(tenant.TenantId);
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
