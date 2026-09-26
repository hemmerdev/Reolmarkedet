using Reolmarkedet.Core.Interfaces;
using Reolmarkedet.Core.Models;
using Reolmarkedet.Core.Services;
using Reolmarkedet.WPF.Commands;

namespace Reolmarkedet.WPF.ViewModels
{
    public class SaleViewModel : ViewModelBase
    {
        private readonly SalesService _salesService;
        private string _searchText = string.Empty;
        private string _salePriceText = string.Empty;
        private string _notes = string.Empty;
        private Item? _selectedItem;
        private string _saleConfirmationMessage = string.Empty;
        private string _saleMessage = string.Empty;

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
                    OnPropertyChanged();
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

        public RelayCommand FindItemCommand { get; }
        public RelayCommand RegisterSaleCommand { get; }

        public SaleViewModel(
            IItemRepository itemRepository,
            IRepository<Sale> saleRepository)
        {
            _salesService = new SalesService(itemRepository, saleRepository);

            FindItemCommand = new RelayCommand(FindItem, CanFindItem);
            RegisterSaleCommand = new RelayCommand(RegisterSale, CanRegisterSale);
        }

        private bool CanRegisterSale(object? parameter)
        {
            return SelectedItem is not null;
        }

        private void RegisterSale(object? parameter)
        {
            SaleMessage = string.Empty;
            SaleConfirmationMessage = string.Empty;

            if (SelectedItem is null)
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
                _salesService.RegisterSale(
                    SelectedItem.ItemId,
                    salePrice,
                    DateOnly.FromDateTime(DateTime.Today),
                    Notes);

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
                SelectedItem = _salesService.FindItem(SearchText);
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
    }
}
