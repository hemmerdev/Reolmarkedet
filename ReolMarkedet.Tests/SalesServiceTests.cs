using Reolmarkedet.Core.Models;
using Reolmarkedet.Core.Models.enums;
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
            item.ItemId, 80.0m, new DateOnly(2026, 9, 26), "Test sale notes", PaymentMethod.Card);

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
            item.ItemId, 80.0m, new DateOnly(2026, 9, 26), "Original sale", PaymentMethod.Card);

        // Act and Assert
        Assert.ThrowsExactly<InvalidOperationException>(() =>
            salesService.RegisterSale(
                item.ItemId, 90.0m, new DateOnly(2026, 9, 27), "Second attempt", PaymentMethod.Card));


        Assert.AreEqual(1, saleRepository.GetAll().Count());

        Sale storedSale = saleRepository.GetAll().Single();

        Assert.AreEqual(item.ItemId, storedSale.ItemId);
        Assert.AreEqual(80m, storedSale.SalePrice);
        Assert.AreEqual(new DateOnly(2026, 9, 26), storedSale.SaleDate);
        Assert.AreEqual("Original sale", storedSale.Notes);
    }

    [TestMethod]
    public void ReturnItem_WhenSaleExists_RemovesSaleAndMakesItemAvailable()
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

        Sale sale = salesService.RegisterSale(
            item.ItemId, 80.0m, new DateOnly(2026, 9, 26), "Test sale notes", PaymentMethod.Card);
        // Act
        salesService.ReturnItem(sale.SaleId);

        // Assert
        Assert.AreEqual(0, saleRepository.GetAll().Count());
        Assert.AreEqual(1, itemRepository.GetUnsold().Count());
    }

    [TestMethod]
    public void RegisterSale_WhenItemHasBeenReturned_AllowsResale()
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
        Sale sale = salesService.RegisterSale(
            item.ItemId, 80.0m, new DateOnly(2026, 9, 26), "Test sale notes", PaymentMethod.Card);
        salesService.ReturnItem(sale.SaleId);

        // Act
        Sale resale = salesService.RegisterSale(
            item.ItemId, 90.0m, new DateOnly(2026, 9, 27), "Resale attempt", PaymentMethod.Card);

        // Assert
        Assert.AreEqual(1, saleRepository.GetAll().Count());
        Assert.Contains(resale, saleRepository.GetAll());
        Assert.AreEqual(0, itemRepository.GetUnsold().Count());
        Assert.AreNotEqual(sale.SaleId, resale.SaleId);
    }

    [TestMethod]
    public void ReturnItem_WhenSaleAlreadyReturned_ThrowsAndKeepsItemUnsold()
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
        Sale sale = salesService.RegisterSale(
            item.ItemId, 80.0m, new DateOnly(2026, 9, 26), "Test sale notes", PaymentMethod.Card);
        salesService.ReturnItem(sale.SaleId);

        // Act and Assert
        Assert.ThrowsExactly<InvalidOperationException>(() =>
            salesService.ReturnItem(sale.SaleId));

        Assert.AreEqual(0, saleRepository.GetAll().Count());
        Assert.Contains(item, itemRepository.GetAll());
        Assert.AreEqual(1, itemRepository.GetUnsold().Count());
    }
}
