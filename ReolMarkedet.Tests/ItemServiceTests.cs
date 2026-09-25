using Reolmarkedet.Core.Interfaces;
using Reolmarkedet.Core.Models;
using Reolmarkedet.Core.Services;
using ReolMarkedet.Tests.Fakes;

namespace ReolMarkedet.Tests;

[TestClass]
public class ItemServiceTests
{
    [TestMethod]
    public void Delete_WhenItemIsSold_ThrowsAndKeepsItem()
    {
        // Arrange
        Item item = new()
        {
            ItemId = 1,
            Description = "Bog"
        };

        SoldItemRepository items = new(item);
        ItemService service = new(items, new FakeRentalRepository());

        // Act
        Assert.ThrowsExactly<InvalidOperationException>(
            () => service.Delete(item.ItemId));

        // Assert
        Assert.IsNotNull(items.GetById(item.ItemId));
        Assert.IsFalse(items.DeleteCalled);
    }

    private sealed class SoldItemRepository(Item item) : IItemRepository
    {
        public bool DeleteCalled { get; private set; }

        public Item? GetById(int id) => id == item.ItemId ? item : null;
        public bool IsSold(int id) => id == item.ItemId;
        public void Delete(int id) => DeleteCalled = true;

        // Required members of IRepository<Item> that are not used in this test
        public IEnumerable<Item> GetAll() => new[] { item };
        public IEnumerable<Item> GetUnsold() => Array.Empty<Item>();
        public Item? GetByBarcode(string barcode) => null;
        public void Add(Item value) => throw new NotSupportedException();
        public void Update(Item value) => throw new NotSupportedException();
    }
}