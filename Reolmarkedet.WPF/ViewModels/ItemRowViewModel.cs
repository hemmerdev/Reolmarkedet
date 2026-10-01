using Reolmarkedet.Core.Models;

namespace Reolmarkedet.WPF.ViewModels
{
    public class ItemRowViewModel : ViewModelBase
    {
        public Item Item { get; }
        public int ItemId => Item.ItemId;
        public string Description => Item.Description;
        public decimal Price => Item.Price;
        public string Barcode => Item.Barcode;
        public int ShelfNumber { get; }
        public string TenantName { get; }

        public ItemRowViewModel(Item item, Rental rental)
        {
            Item = item;
            ShelfNumber = rental.Shelf.ShelfNumber;
            TenantName = rental.Tenant.Name;
        }
    }
}
