using Reolmarkedet.Core.Models;
using Reolmarkedet.Core.Models.enums;

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
        public string PaymentMethodText { get; }

        public SaleRowViewModel(Sale sale, Item item, Rental rental)
        {
            SaleId = sale.SaleId;
            SaleDate = sale.SaleDate;
            ItemDescription = item.Description;
            ShelfNumber = rental.Shelf.ShelfNumber;
            TenantName = rental.Tenant.Name;
            SalePrice = sale.SalePrice;
            Notes = sale.Notes;
            PaymentMethodText = sale.PaymentMethod switch
            {
                PaymentMethod.Cash => "Kontant",
                PaymentMethod.Card => "Kort",
                PaymentMethod.MobilePay => "MobilePay",
                _ => "Ikke angivet"
            };
        }
    }
}
