using Reolmarkedet.Core.Interfaces;
using Reolmarkedet.Core.Models;
using Reolmarkedet.Core.Results;
using Reolmarkedet.Core.Services;
using Reolmarkedet.WPF.Commands;
using System.Collections.ObjectModel;

namespace Reolmarkedet.WPF.ViewModels
{
    public class SettlementViewModel : ViewModelBase
    {
        private readonly SettlementService _settlementService;
        private readonly IRepository<Tenant> _tenantRepository;
        private readonly IRepository<Sale> _saleRepository;
        private readonly IItemRepository _itemRepository;
        private readonly IRepository<Rental> _rentalRepository;
        private int _selectedYear;
        private int _selectedMonth;
        private bool _isExpanded;
        private MonthlySettlementResult? _selectedSettlement;
        public ObservableCollection<MonthlySettlementResult> MonthlySettlements { get; } = new();

        public int SelectedYear
        {
            get => _selectedYear;
            set
            {
                if (value != _selectedYear)
                {
                    _selectedYear = value;
                    OnPropertyChanged();
                    MonthlySettlements.Clear();
                    SelectedSettlement = null;
                    CalculateSettlementsCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public int SelectedMonth
        {
            get => _selectedMonth;
            set
            {
                if (value != _selectedMonth)
                {
                    _selectedMonth = value;
                    OnPropertyChanged();
                    MonthlySettlements.Clear();
                    SelectedSettlement = null;
                    CalculateSettlementsCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public MonthlySettlementResult? SelectedSettlement
        {
            get => _selectedSettlement;
            set
            {
                if (value != _selectedSettlement)
                {
                    IsExpanded = false;
                    _selectedSettlement = value;
                    OnPropertyChanged();
                }
            }
        }

        // Used to control the expansion state of the Expander in the UI.
        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                if (value != _isExpanded)
                {
                    _isExpanded = value;
                    OnPropertyChanged();
                }
            }

        }

        public IReadOnlyList<int> MonthOptions { get; } =
            Enumerable.Range(1, 12).ToList();
        public IReadOnlyList<int> YearOptions { get; } =
            Enumerable.Range(DateTime.Today.Year - 5, 6)
            .Reverse()
            .ToList();


        public RelayCommand CalculateSettlementsCommand { get; }
        public RelayCommand CloseDetailedViewCommand { get; }

        public SettlementViewModel(
            IRepository<Tenant> tenantRepository,
            IRepository<Sale> saleRepository,
            IItemRepository itemRepository,
            IRepository<Rental> rentalRepository)
        {
            _tenantRepository = tenantRepository;
            _saleRepository = saleRepository;
            _itemRepository = itemRepository;
            _rentalRepository = rentalRepository;

            _settlementService = new SettlementService();

            CalculateSettlementsCommand = new RelayCommand(CalculateSettlements, CanCalculateSettlements);
            CloseDetailedViewCommand = new RelayCommand(_ => SelectedSettlement = null);

            DateTime previousMonth =
                new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(-1);

            SelectedYear = previousMonth.Year;
            SelectedMonth = previousMonth.Month;

        }

        private bool CanCalculateSettlements(object? parameter)
        {
            return SelectedMonth >= 1 && SelectedMonth <= 12 &&
                   (SelectedYear < DateTime.Today.Year ||
                   (SelectedYear == DateTime.Today.Year &&
                   SelectedMonth < DateTime.Today.Month));
        }

        private void CalculateSettlements(object? parameter)
        {
            SelectedSettlement = null;
            var tenants = _tenantRepository.GetAll();
            var sales = _saleRepository.GetAll();
            var items = _itemRepository.GetAll();
            var rentals = _rentalRepository.GetAll();

            var results = _settlementService.CalculateForMonth(
                SelectedYear, SelectedMonth, tenants, sales, items, rentals);

            MonthlySettlements.Clear();
            foreach (var result in results)
            {
                MonthlySettlements.Add(result);
            }
        }

        public void Refresh()
        {
            SelectedSettlement = null;
        }
    }
}
