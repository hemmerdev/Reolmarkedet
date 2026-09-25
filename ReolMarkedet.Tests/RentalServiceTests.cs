using Reolmarkedet.Core.Models;
using Reolmarkedet.Core.Services;

namespace ReolMarkedet.Tests;

[TestClass]
public class RentalServiceTests
{
    [TestMethod]
    public void IsShelfAvailable_WhenPeriodsShareBoundaryDay_ReturnsFalse()
    {
        // Arrange
        RentalService rentalService = new();
        Tenant tenant = new() { TenantId = 1, Name = "Test Tenant" };
        ShelfType shelfType = new() { ShelfTypeId = 1, Name = "Test Type" };
        Shelf shelf = new(shelfType) { ShelfId = 1, ShelfNumber = 1 };

        Rental existingRental = new(tenant, shelf)
        {
            StartDate = new DateTime(2026, 9, 1),
            EndDate = new DateTime(2026, 9, 30)
        };

        // Act
        bool available = rentalService.IsShelfAvailable(
            shelf,
            new DateTime(2026, 9, 30),
            new DateTime(2026, 10, 30),
            new List<Rental> { existingRental });

        // Assert
        Assert.IsFalse(available);
    }

    [TestMethod]
    public void IsShelfAvailable_WhenRequestedPeriodStartsAfterExistingEnd_ReturnsTrue()
    {
        // Arrange
        RentalService rentalService = new();
        Tenant tenant = new() { TenantId = 1, Name = "Test Tenant" };
        ShelfType shelfType = new() { ShelfTypeId = 1, Name = "Test Type" };
        Shelf shelf = new(shelfType) { ShelfId = 1, ShelfNumber = 1 };

        Rental existingRental = new(tenant, shelf)
        {
            StartDate = new DateTime(2026, 9, 1),
            EndDate = new DateTime(2026, 9, 30)
        };

        // Act
        bool available = rentalService.IsShelfAvailable(
            shelf,
            new DateTime(2026, 10, 1),
            new DateTime(2026, 10, 30),
            new List<Rental> { existingRental });

        // Assert
        Assert.IsTrue(available);
    }

    [TestMethod]
    public void IsShelfAvailable_WhenExistingRentalHasNoEnd_ReturnsFalse()
    {
        // Arrange
        RentalService rentalService = new();
        Tenant tenant = new() { TenantId = 1, Name = "Test Tenant" };
        ShelfType shelfType = new() { ShelfTypeId = 1, Name = "Test Type" };
        Shelf shelf = new(shelfType) { ShelfId = 1, ShelfNumber = 1 };

        Rental existingRental = new(tenant, shelf)
        {
            StartDate = new DateTime(2026, 9, 1),
            EndDate = null
        };

        // Act
        bool available = rentalService.IsShelfAvailable(
            shelf,
            new DateTime(2026, 10, 1),
            new DateTime(2026, 10, 30),
            new List<Rental> { existingRental });

        // Assert
        Assert.IsFalse(available);
    }

    [TestMethod]
    public void IsShelfAvailable_WhenOpenEndedRequestOverlapsFutureRental_ReturnsFalse()
    {
        // Arrange
        RentalService rentalService = new();
        Tenant tenant = new() { TenantId = 1, Name = "Test Tenant" };
        ShelfType shelfType = new() { ShelfTypeId = 1, Name = "Test Type" };
        Shelf shelf = new(shelfType) { ShelfId = 1, ShelfNumber = 1 };

        Rental existingRental = new(tenant, shelf)
        {
            StartDate = new DateTime(2026, 10, 1),
            EndDate = new DateTime(2026, 10, 30)
        };

        // Act
        bool available = rentalService.IsShelfAvailable(
            shelf,
            new DateTime(2026, 9, 1),
            null,
            new List<Rental> { existingRental });

        // Assert
        Assert.IsFalse(available);
    }

    [TestMethod]
    public void GetShelfStatus_WhenCurrentRentalHasEndDateButNoNotice_ReturnsTerminationPending()
    {
        // Arrange
        RentalService rentalService = new();
        Tenant tenant = new() { TenantId = 1, Name = "Test Tenant" };
        ShelfType shelfType = new() { ShelfTypeId = 1, Name = "Test Type" };
        Shelf shelf = new(shelfType) { ShelfId = 1, ShelfNumber = 1 };

        Rental existingRental = new(tenant, shelf)
        {
            StartDate = new DateTime(2026, 10, 1),
            EndDate = new DateTime(2026, 10, 30),
            TerminationNoticeDate = null
        };

        List<Rental> rentals = new() { existingRental };

        // Act
        ShelfStatus status =
            rentalService.GetShelfStatus(shelf, new DateTime(2026, 10, 15), rentals);

        // Assert
        Assert.AreEqual(ShelfStatus.TerminationPending, status);
    }

    [TestMethod]
    public void GetShelfStatus_WhenCurrentRentalHasTerminationNotice_ReturnsTerminationPending()
    {
        // Arrange
        RentalService rentalService = new();
        Tenant tenant = new() { TenantId = 1, Name = "Test Tenant" };
        ShelfType shelfType = new() { ShelfTypeId = 1, Name = "Test Type" };
        Shelf shelf = new(shelfType) { ShelfId = 1, ShelfNumber = 1 };

        Rental existingRental = new(tenant, shelf)
        {
            StartDate = new DateTime(2026, 10, 1),
            EndDate = new DateTime(2026, 10, 30),
            TerminationNoticeDate = new DateTime(2026, 10, 10)
        };

        List<Rental> rentals = new() { existingRental };

        // Act
        ShelfStatus status =
            rentalService.GetShelfStatus(shelf, new DateTime(2026, 10, 15), rentals);

        // Assert
        Assert.AreEqual(ShelfStatus.TerminationPending, status);
    }

    [TestMethod]
    public void GetShelfStatus_WhenCurrentRentalEnded_ReturnsAvailable()
    {
        // Arrange
        RentalService rentalService = new();
        Tenant tenant = new() { TenantId = 1, Name = "Test Tenant" };
        ShelfType shelfType = new() { ShelfTypeId = 1, Name = "Test Type" };
        Shelf shelf = new(shelfType) { ShelfId = 1, ShelfNumber = 1 };

        Rental existingRental = new(tenant, shelf)
        {
            StartDate = new DateTime(2026, 10, 1),
            EndDate = new DateTime(2026, 10, 14),
        };

        List<Rental> rentals = new() { existingRental };

        // Act
        ShelfStatus status =
            rentalService.GetShelfStatus(shelf, new DateTime(2026, 10, 15), rentals);

        // Assert
        Assert.AreEqual(ShelfStatus.Available, status);
    }

    [TestMethod]
    public void TerminateRental_WhenValid_RecordsDatesAndPreservesRental()
    {
        // Arrange
        RentalService rentalService = new();
        Tenant tenant = new() { TenantId = 1, Name = "Test Tenant" };
        ShelfType shelfType = new() { ShelfTypeId = 1, Name = "Test Type" };
        Shelf shelf = new(shelfType) { ShelfId = 1, ShelfNumber = 1 };

        Rental rental = new(tenant, shelf)
        {
            StartDate = new DateTime(2026, 8, 18),
            EndDate = null,
            TerminationNoticeDate = null
        };

        List<Rental> existingRentals = new() { rental };

        // Act        
        rentalService.TerminateRental(
            rental,
            new DateTime(2026, 9, 18),
            new DateTime(2026, 10, 31),
            existingRentals);

        // Assert
        Assert.AreEqual(rental.EndDate, new DateTime(2026, 10, 31));
        Assert.AreEqual(rental.TerminationNoticeDate, new DateTime(2026, 9, 18));
        CollectionAssert.Contains(existingRentals, rental);
        ShelfStatus status =
            rentalService.GetShelfStatus(
                shelf, new DateTime(2026, 9, 18), existingRentals);
        Assert.AreEqual(ShelfStatus.TerminationPending, status);
    }

    [TestMethod]
    public void TerminateRental_NoticeOnTwentieth_AllowsCurrentMonthEnd()
    {
        // Arrange 
        RentalService rentalService = new();
        Tenant tenant = new() { TenantId = 1, Name = "Test Tenant" };
        ShelfType shelfType = new() { ShelfTypeId = 1, Name = "Test Type" };
        Shelf shelf = new(shelfType) { ShelfId = 1, ShelfNumber = 1 };

        Rental rental = new(tenant, shelf)
        {
            StartDate = new(2026, 8, 18)
        };

        List<Rental> existingRentals = new() { rental };
        DateTime noticeDate = new(2026, 9, 20);
        DateTime expectedEndDate = new(2026, 9, 30);

        // Act
        rentalService.TerminateRental(
            rental,
            noticeDate,
            expectedEndDate,
            existingRentals);

        // Assert
        Assert.AreEqual(expectedEndDate, rental.EndDate);
        Assert.AreEqual(noticeDate, rental.TerminationNoticeDate);

    }

    [TestMethod]
    public void ChangeTerminationEndDate_WhenOverlappingAnotherRental_ThrowsAndLeavesDatesUnchanged()
    {
        RentalService rentalService = new();
        Tenant tenant = new() { TenantId = 1, Name = "Test Tenant" };
        ShelfType shelfType = new() { ShelfTypeId = 1, Name = "Test Type" };
        Shelf shelf = new(shelfType) { ShelfId = 1, ShelfNumber = 1 };

        Rental rental = new(tenant, shelf)
        {
            StartDate = new DateTime(2026, 8, 18),
            EndDate = new DateTime(2026, 9, 30),
            TerminationNoticeDate = null
        };

        Rental rentalTwo = new(tenant, shelf)
        {
            StartDate = new DateTime(2026, 10, 1),
            EndDate = null,
            TerminationNoticeDate = null
        };

        List<Rental> existingRentals = new() { rental, rentalTwo };


        // Act & Assert
        Assert.ThrowsExactly<InvalidOperationException>(() =>
            rentalService.ChangeTerminationEndDate(
                rental,
                new DateTime(2026, 9, 18),
                new DateTime(2026, 10, 31),
                existingRentals));

        Assert.AreEqual(new DateTime(2026, 9, 30), rental.EndDate);
        Assert.IsNull(rental.TerminationNoticeDate);
        CollectionAssert.Contains(existingRentals, rental);
        Assert.HasCount(2, existingRentals);
    }

}
