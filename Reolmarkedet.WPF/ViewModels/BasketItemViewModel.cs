using Reolmarkedet.Core.Models;

namespace Reolmarkedet.WPF.ViewModels
{
    public class BasketItemViewModel
    {
        public Item Item { get; }
        public Rental Rental { get; }
        public int ItemId => Item.ItemId;
        public string Description => Item.Description;
        public int ShelfNumber => Rental.Shelf.ShelfNumber;
        public string TenantName => Rental.Tenant.Name;
        public decimal SalePrice { get; }
        public string? Notes { get; }

        public BasketItemViewModel(
            Item item, Rental rental, decimal salePrice, string? notes)
        {
            Item = item;
            Rental = rental;
            SalePrice = salePrice;
            Notes = notes;
        }
    }
}
