using Reolmarkedet.Core.Interfaces;
using Reolmarkedet.Core.Models;

namespace ReolMarkedet.Tests.Fakes
{
    internal class FakeItemRepository : IItemRepository
    {
        private readonly List<Item> _items = new();
        private readonly IRepository<Sale> _saleRepository;

        public FakeItemRepository(IRepository<Sale> saleRepository)
        {
            _saleRepository = saleRepository;
        }
        private int _nextItemId = 1;

        public IEnumerable<Item> GetAll()
        {
            return _items;
        }

        public Item? GetById(int id)
        {
            foreach (var item in _items)
            {
                if (item.ItemId == id)
                {
                    return item;
                }
            }

            return null;
        }

        public void Add(Item item)
        {
            if (item.ItemId == 0)
            {
                item.ItemId = _nextItemId++;
            }
            else if (item.ItemId >= _nextItemId)
            {
                _nextItemId = item.ItemId + 1;
            }

            _items.Add(item);
        }

        public void Update(Item item)
        {
            Item? storedItem = GetById(item.ItemId);
            if (storedItem is null)
            {
                throw new InvalidOperationException(
                    "No item found with the specified ItemId");
            }

            storedItem.Description = item.Description;
            storedItem.Price = item.Price;
            storedItem.Barcode = item.Barcode;
            storedItem.RentalId = item.RentalId;

        }

        public void Delete(int id)
        {
            Item? item = GetById(id);

            if (item is null)
            {
                throw new InvalidOperationException(
                    "No item found with the specified ItemId");
            }

            _items.Remove(item);
        }

        public Item? GetByBarcode(string barcode)
        {
            foreach (Item item in _items)
            {
                if (item.Barcode == barcode)
                {
                    return item;
                }
            }

            return null;
        }

        public bool IsSold(int itemId)
        {
            foreach (Sale sale in _saleRepository.GetAll())
            {
                if (sale.ItemId == itemId)
                {
                    return true;
                }
            }

            return false;
        }

        public IEnumerable<Item> GetUnsold()
        {
            List<Item> unsoldItems = new();

            foreach (Item item in _items)
            {
                if (!IsSold(item.ItemId))
                {
                    unsoldItems.Add(item);
                }
            }

            return unsoldItems;
        }
    }
}
