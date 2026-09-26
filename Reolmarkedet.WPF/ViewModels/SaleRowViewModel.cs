using Reolmarkedet.Core.Models;

namespace Reolmarkedet.WPF.ViewModels
{
    public class SaleRowViewModel : ViewModelBase
    {
        public int SaleId { get; }
        public DateOnly SaleDate { get; }
        public string ItemDescription { get; }
        public int ShelfNumber { get; }
        public string TenantName { get; }
        public decimal SalePrice { get; }
        public string? Notes { get; }

        public SaleRowViewModel(Sale sale, Item item, Rental rental)
        {
            SaleId = sale.SaleId;
            SaleDate = sale.SaleDate;
            ItemDescription = item.Description;
            ShelfNumber = rental.Shelf.ShelfNumber;
            TenantName = rental.Tenant.Name;
            SalePrice = sale.SalePrice;
            Notes = sale.Notes;
        }
    }
}
