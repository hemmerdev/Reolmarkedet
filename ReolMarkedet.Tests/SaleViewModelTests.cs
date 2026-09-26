using Reolmarkedet.Core.Models;
using Reolmarkedet.WPF.ViewModels;
using ReolMarkedet.Tests.Fakes;

namespace ReolMarkedet.Tests;

[TestClass]
public class SaleViewModelTests
{
    [TestMethod]
    public void SelectingRental_ShowsOnlyItsUnsoldItems()
    {
        // Arrange
        FakeSaleRepository saleRepository = new();
        FakeRentalRepository rentalRepository = new();
        FakeItemRepository itemRepository = new(saleRepository);

        Tenant tenant = new() { TenantId = 1, Name = "Test Tenant" };
        ShelfType shelfType = new() { ShelfTypeId = 1, Name = "Test Shelf Type" };
        Shelf shelf = new(shelfType) { ShelfId = 1, ShelfNumber = 1 };

        Rental firstRental = new(tenant, shelf)
        {
            RentalId = 1,
        };
        Rental secondRental = new(tenant, shelf)
        {
            RentalId = 2,
        };
        rentalRepository.Add(firstRental);
        rentalRepository.Add(secondRental);

        Item itemA = new()
        {
            ItemId = 1,
            Description = "First Rental Item",
            Price = 10.00m,
            RentalId = 1
        };
        Item itemB = new()
        {
            ItemId = 2,
            Description = "Second Rental Item",
            Price = 20.00m,
            RentalId = 2
        };
        Sale sale = new()
        {
            SaleId = 1,
            ItemId = 3,
            SaleDate = DateOnly.FromDateTime(DateTime.Today),
            SalePrice = 20.00m
        };
        Item itemC = new()
        {
            ItemId = 3,
            Description = "Third Rental Item",
            Price = 30.00m,
            RentalId = 1
        };

        itemRepository.Add(itemA);
        itemRepository.Add(itemB);
        itemRepository.Add(itemC);
        saleRepository.Add(sale);

        SaleViewModel saleViewModel = new(
            itemRepository,
            saleRepository,
            rentalRepository);

        saleViewModel.Refresh();

        // Act
        saleViewModel.SelectedRentalOption = saleViewModel.RentalOptions.First(
                option => option.RentalId == firstRental.RentalId);

        // Assert
        Assert.HasCount(1, saleViewModel.ItemOptions);
        Assert.AreEqual(itemA.ItemId, saleViewModel.ItemOptions[0].ItemId);
    }

    [TestMethod]
    public void RegisterSale_WhenLastUnsoldItemSold_RemovesRentalOptionAndClearsSelection()
    {
        // Arrange
        FakeSaleRepository saleRepository = new();
        FakeRentalRepository rentalRepository = new();
        FakeItemRepository itemRepository = new(saleRepository);

        Tenant tenant = new() { TenantId = 1, Name = "Test Tenant" };
        ShelfType shelfType = new() { ShelfTypeId = 1, Name = "Test Shelf Type" };
        Shelf shelf = new(shelfType) { ShelfId = 1, ShelfNumber = 1 };

        Rental firstRental = new(tenant, shelf)
        {
            RentalId = 1,
        };
        Rental secondRental = new(tenant, shelf)
        {
            RentalId = 2,
        };
        rentalRepository.Add(firstRental);
        rentalRepository.Add(secondRental);

        Item itemA = new()
        {
            ItemId = 1,
            Description = "First Rental Item",
            Price = 10.00m,
            RentalId = 1
        };
        Item itemB = new()
        {
            ItemId = 2,
            Description = "Second Rental Item",
            Price = 20.00m,
            RentalId = 2
        };
        Sale sale = new()
        {
            SaleId = 1,
            ItemId = 3,
            SaleDate = DateOnly.FromDateTime(DateTime.Today),
            SalePrice = 20.00m
        };
        Item itemC = new()
        {
            ItemId = 3,
            Description = "Third Rental Item",
            Price = 30.00m,
            RentalId = 1
        };

        itemRepository.Add(itemA);
        itemRepository.Add(itemB);
        itemRepository.Add(itemC);
        saleRepository.Add(sale);

        SaleViewModel saleViewModel = new(
            itemRepository,
            saleRepository,
            rentalRepository);

        saleViewModel.Refresh();

        saleViewModel.SelectedRentalOption = saleViewModel.RentalOptions.First(
                option => option.RentalId == firstRental.RentalId);

        saleViewModel.SelectedItemOption = saleViewModel.ItemOptions.Single();

        // Act
        saleViewModel.RegisterSaleCommand.Execute(null);

        // Assert
        Assert.HasCount(2, saleRepository.GetAll());
        Assert.IsTrue(itemRepository.IsSold(itemA.ItemId));

        Assert.HasCount(1, saleViewModel.RentalOptions);
        Assert.AreEqual(
            secondRental.RentalId,
            saleViewModel.RentalOptions[0].RentalId);

        Assert.HasCount(0, saleViewModel.ItemOptions);

        Assert.IsNull(saleViewModel.SelectedRentalOption);
        Assert.IsNull(saleViewModel.SelectedItemOption);
        Assert.IsNull(saleViewModel.SelectedItem);

        Assert.HasCount(2, saleViewModel.Sales);
    }
}
