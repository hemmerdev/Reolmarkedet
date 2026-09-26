using Reolmarkedet.Core.Models;
using Reolmarkedet.Core.Services;
using ReolMarkedet.Tests.Fakes;

namespace ReolMarkedet.Tests;

[TestClass]
public class SalesServiceTests
{
    [TestMethod]
    public void RegisterSale_WhenItemIsUnsold_StoresSaleWithEnteredDetails()
    {
        // Arrange
        FakeSaleRepository saleRepository = new();
        FakeItemRepository itemRepository = new(saleRepository);

        Item item = new()
        {
            ItemId = 1,
            Description = "Test Item",
            Price = 100.0m,
            Barcode = "1234567890"
        };
        itemRepository.Add(item);

        SalesService salesService = new(itemRepository, saleRepository);

        // Act
        salesService.RegisterSale(
            item.ItemId, 80.0m, new DateOnly(2026, 9, 26), "Test sale notes");

        // Assert
        Assert.AreEqual(1, saleRepository.GetAll().Count());

        Sale storedSale = saleRepository.GetAll().Single();

        Assert.IsGreaterThan(0, storedSale.SaleId);
        Assert.AreEqual(item.ItemId, storedSale.ItemId);
        Assert.AreEqual(80m, storedSale.SalePrice);
        Assert.AreEqual(new DateOnly(2026, 9, 26), storedSale.SaleDate);
        Assert.AreEqual("Test sale notes", storedSale.Notes);
        Assert.IsTrue(itemRepository.IsSold(item.ItemId));
    }

    [TestMethod]
    public void RegisterSale_WhenItemIsAlreadySold_ThrowsAndDoesNotAddSale()
    {
        // Arrange
        FakeSaleRepository saleRepository = new();
        FakeItemRepository itemRepository = new(saleRepository);

        Item item = new()
        {
            ItemId = 1,
            Description = "Test Item",
            Price = 100.0m,
            Barcode = "1234567890"
        };
        itemRepository.Add(item);

        SalesService salesService = new(itemRepository, saleRepository);
        salesService.RegisterSale(
            item.ItemId, 80.0m, new DateOnly(2026, 9, 26), "Original sale");

        // Act and Assert
        Assert.ThrowsExactly<InvalidOperationException>(() =>
            salesService.RegisterSale(
                item.ItemId, 80.0m, new DateOnly(2026, 9, 26), "Second attempt"));


        Assert.AreEqual(1, saleRepository.GetAll().Count());

        Sale storedSale = saleRepository.GetAll().Single();

        Assert.AreEqual(item.ItemId, storedSale.ItemId);
        Assert.AreEqual(80m, storedSale.SalePrice);
        Assert.AreEqual(new DateOnly(2026, 9, 26), storedSale.SaleDate);
        Assert.AreEqual("Original sale", storedSale.Notes);
    }
}
