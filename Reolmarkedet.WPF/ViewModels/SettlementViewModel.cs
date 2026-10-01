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
        public ObservableCollection<MonthlySettlementResult> MonthlySettlements { get; } = new();
        public int SelectedYear { get; set; }
        public int SelectedMonth { get; set; }

        public RelayCommand CalculateSettlementsCommand { get; }

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

            DateTime previousMonth =
                new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(-1);

            SelectedYear = previousMonth.Year;
            SelectedMonth = previousMonth.Month;

            CalculateSettlementsCommand = new RelayCommand(CalculateSettlements);
        }

        private void CalculateSettlements(object? parameter)
        {
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
    }
}
