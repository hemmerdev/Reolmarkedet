using Reolmarkedet.Core.Models;
using Reolmarkedet.Core.Services;

namespace ReolMarkedet.Tests;

[TestClass]
public class SettlementServiceTests
{
    [TestMethod]
    public void GetSalesForTenantAndMonth_WhenSalesSpanTenantsAndPeriods_ReturnsOnlyMatchingSales()
    {
        // Arrange
        SettlementService service = new();

        Tenant firstTenant = new() { TenantId = 1, Name = "First tenant" };
        Tenant secondTenant = new() { TenantId = 2, Name = "Second tenant" };

        ShelfType shelfType = new() { ShelfTypeId = 1, Name = "Test type" };
        Shelf firstShelf = new(shelfType) { ShelfId = 1, ShelfNumber = 1 };
        Shelf secondShelf = new(shelfType) { ShelfId = 2, ShelfNumber = 2 };

        List<Rental> rentals = new()
    {
        new(firstTenant, firstShelf)
        {
            RentalId = 1,
            StartDate = new DateTime(2025, 1, 1)
        },
        new(secondTenant, secondShelf)
        {
            RentalId = 2,
            StartDate = new DateTime(2025, 1, 1)
        }
    };

        List<Item> items = new()
    {
        new() { ItemId = 1, RentalId = 1 },
        new() { ItemId = 2, RentalId = 1 },
        new() { ItemId = 3, RentalId = 2 },
        new() { ItemId = 4, RentalId = 1 },
        new() { ItemId = 5, RentalId = 1 }
    };

        List<Sale> sales = new()
    {
        // Included: first and last day of the requested month.
        new()
        {
            SaleId = 1,
            ItemId = 1,
            SaleDate = new DateOnly(2026, 9, 1)
        },
        new()
        {
            SaleId = 2,
            ItemId = 2,
            SaleDate = new DateOnly(2026, 9, 30)
        },

        // Excluded: another tenant.
        new()
        {
            SaleId = 3,
            ItemId = 3,
            SaleDate = new DateOnly(2026, 9, 15)
        },

        // Excluded: another month.
        new()
        {
            SaleId = 4,
            ItemId = 4,
            SaleDate = new DateOnly(2026, 10, 1)
        },

        // Excluded: the same month in another year.
        new()
        {
            SaleId = 5,
            ItemId = 5,
            SaleDate = new DateOnly(2025, 9, 15)
        }
    };

        // Act
        List<Sale> result = service.GetSalesForTenantAndMonth(
            firstTenant, 2026, 9, sales, items, rentals);

        // Assert
        Assert.HasCount(2, result);
        Assert.Contains(sales[0], result);
        Assert.Contains(sales[1], result);
    }

    [TestMethod]
    public void CalculateCommission_RoundsEachSaleBeforeSumming()
    {
        // Arrange
        SettlementService service = new();

        List<Sale> sales = new()
    {
        new()
        {
            SalePrice = 0.05m // 10% commission = 0.005, rounded to 0.01
        },
        new()
        {
            SalePrice = 0.05m // 10% commission = 0.005, rounded to 0.01
        },
    };
        // Act
        decimal result = service.CalculateCommission(sales);

        // Assert
        Assert.AreEqual(0.02m, result); // 0.01 + 0.01 = 0.02
    }

    [TestMethod]
    public void CalculateRentalChargeForMonth_ExcludesFirstPeriodAndProratesFinalMonth()
    {
        // Arrange
        SettlementService service = new();

        Tenant tenant = new() { TenantId = 1, Name = "Test tenant" };
        ShelfType shelfType = new() { ShelfTypeId = 1, Name = "Test type" };
        Shelf shelf = new(shelfType) { ShelfId = 1, ShelfNumber = 1 };

        Rental rental = new(tenant, shelf)
        {
            RentalId = 1,
            StartDate = new DateTime(2026, 09, 20),
            EndDate = new DateTime(2026, 10, 10),
            MonthlyRent = 310m,
            IsCustomPrice = true
        };

        List<Rental> rentals = new() { rental };

        // Act
        decimal sepCharge =
            service.CalculateRentalChargeForMonth(rental, 2026, 9, rentals);
        decimal octCharge =
            service.CalculateRentalChargeForMonth(rental, 2026, 10, rentals);

        // Assert
        Assert.AreEqual(0m, sepCharge); // First period, already charged
        Assert.AreEqual(100m, octCharge); // 310 / 31 days in Oct * 10 days = 100
    }

    [TestMethod]
    public void CalculateForTenantAndMonth_CombinesSalesCommissionAndRent()
    {
        // Arrange
        SettlementService service = new();

        Tenant tenant = new() { TenantId = 1, Name = "Test tenant" };
        ShelfType shelfType = new() { ShelfTypeId = 1, Name = "Test type" };
        Shelf shelf = new(shelfType) { ShelfId = 1, ShelfNumber = 1 };

        Rental rental = new(tenant, shelf)
        {
            RentalId = 1,
            StartDate = new DateTime(2026, 8, 1),
            MonthlyRent = 100m,
            IsCustomPrice = true
        };

        Item item = new() { ItemId = 1, RentalId = rental.RentalId };

        Sale sale = new()
        {
            ItemId = item.ItemId,
            SaleDate = new DateOnly(2026, 9, 15),
            SalePrice = 200m
        };

        // Act
        var result = service.CalculateForTenantAndMonth(
            tenant,
            2026,
            9,
            new[] { sale },
            new[] { item },
            new[] { rental });

        // Assert
        Assert.AreEqual(200m, result.TotalSales);
        Assert.AreEqual(20m, result.Commission);
        Assert.AreEqual(100m, result.Rent);
        Assert.AreEqual(80m, result.Balance);

        Assert.HasCount(1, result.SaleLines);

        var line = result.SaleLines[0];
        Assert.AreSame(sale, line.Sale);
        Assert.AreSame(item, line.Item);
        Assert.AreSame(rental, line.Rental);
        Assert.AreEqual(20m, line.Commission);
    }

    [TestMethod]
    public void CalculateForMonth_IncludesInactiveTenantOnceAndChargesNextMonthRent()
    {
        // Arrange
        SettlementService service = new();

        Tenant inactiveTenant = new()
        {
            TenantId = 1,
            Name = "Inactive tenant",
            IsActive = false
        };
        Tenant noActivityTenant = new()
        {
            TenantId = 2,
            Name = "No activity"
        };

        ShelfType shelfType = new() { ShelfTypeId = 1, Name = "Test type" };
        Shelf firstShelf = new(shelfType) { ShelfId = 1, ShelfNumber = 1 };
        Shelf secondShelf = new(shelfType) { ShelfId = 2, ShelfNumber = 2 };

        List<Rental> rentals = new()
    {
        new(inactiveTenant, firstShelf)
        {
            RentalId = 1,
            StartDate = new DateTime(2026, 9, 1),
            MonthlyRent = 100m,
            IsCustomPrice = true
        },
        new(inactiveTenant, secondShelf)
        {
            RentalId = 2,
            StartDate = new DateTime(2026, 9, 30),
            MonthlyRent = 100m,
            IsCustomPrice = true
        }
    };

        // Act
        var results = service.CalculateForMonth(
            2026,
            9,
            new[] { inactiveTenant, noActivityTenant },
            Array.Empty<Sale>(),
            Array.Empty<Item>(),
            rentals);

        // Assert
        Assert.HasCount(1, results);
        Assert.AreEqual(inactiveTenant.TenantId, results[0].Tenant.TenantId);
        Assert.AreEqual(200m, results[0].Rent);
    }
}

