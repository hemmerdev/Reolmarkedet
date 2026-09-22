using Reolmarkedet.Core.Models;
using Reolmarkedet.WPF.ViewModels;
using ReolMarkedet.Tests.Fakes;
using System.Collections.ObjectModel;

namespace ReolMarkedet.Tests;

[TestClass]
public class ShelfViewModelTests
{
    [TestMethod]
    public void DeleteShelfType_WhenTypeIsUsed_LeavesTypeUnchanged()
    {
        // Arrange
        var viewModel = CreateViewModel();
        ShelfType shelfType = viewModel.Shelves[1].ShelfType;

        int originalCount = viewModel.ShelfTypes.Count;

        viewModel.ShelfTypeToDelete = shelfType;

        // Act
        viewModel.DeleteShelfTypeCommand.Execute(null);

        // Assert
        Assert.HasCount(originalCount, viewModel.ShelfTypes);
        Assert.Contains(shelfType, viewModel.ShelfTypes);
        Assert.AreEqual(shelfType, viewModel.ShelfTypeToDelete);
        Assert.IsFalse(string.IsNullOrWhiteSpace(viewModel.ShelfTypeMessage));
    }

    [TestMethod]
    public void AddShelf_WhenNumberAlreadyExists_DoesNotAddShelf()
    {
        // Arrange
        var viewModel = CreateViewModel();

        int originalCount = viewModel.Shelves.Count();

        viewModel.NewShelfNumber = "1";
        viewModel.NewShelfType = viewModel.ShelfTypes[0];

        // Act
        viewModel.AddShelfCommand.Execute(null);

        // Assert
        Assert.HasCount(originalCount, viewModel.Shelves);
        Assert.IsFalse(string.IsNullOrWhiteSpace(viewModel.ShelfMessage));
        Assert.AreEqual("1", viewModel.NewShelfNumber);
    }

    [TestMethod]
    public void DeleteShelf_WhenShelfHasHistoricalRental_LeavesShelfUnchanged()
    {
        // Arrange
        var rentals = new ObservableCollection<Rental>();
        var viewModel = CreateViewModel(rentals);
        Shelf shelf = viewModel.Shelves[0];
        int originalCount = viewModel.Shelves.Count;

        Tenant tenant = new() { TenantId = 1, Name = "Test Tenant" };
        Rental historicalRental = new(tenant, shelf)
        {
            RentalId = 1,
            StartDate = new DateTime(2025, 1, 1),
            EndDate = new DateTime(2025, 1, 31),
            MonthlyRent = 850m
        };
        rentals.Add(historicalRental);
        viewModel.SelectedShelfRow = viewModel.VisibleShelves[0];

        // Act
        viewModel.DeleteShelfCommand.Execute(null);

        // Assert
        Assert.HasCount(originalCount, viewModel.Shelves);
        Assert.Contains(shelf, viewModel.Shelves);
        Assert.Contains(historicalRental, rentals);
        Assert.IsNotEmpty(viewModel.ShelfMessage);
    }

    [TestMethod]
    public void DeactivateShelf_WhenShelfHasOnlyHistoricalRentals_PreservesHistoryAndHidesShelf()
    {
        // Arrange
        var rentals = new ObservableCollection<Rental>();
        var viewModel = CreateViewModel(rentals);
        Shelf shelf = viewModel.Shelves[0];
        int originalCount = viewModel.Shelves.Count;

        Tenant tenant = new() { TenantId = 1, Name = "Test Tenant" };
        Rental historicalRental = new(tenant, shelf)
        {
            RentalId = 1,
            StartDate = new DateTime(2025, 1, 1),
            EndDate = new DateTime(2025, 1, 31),
            MonthlyRent = 850m
        };
        rentals.Add(historicalRental);
        viewModel.SelectedShelfRow = viewModel.VisibleShelves[0];

        // Act
        viewModel.DeactivateShelfCommand.Execute(null);

        // Assert
        Assert.IsFalse(shelf.IsActive);
        Assert.Contains(shelf, viewModel.Shelves);
        Assert.HasCount(originalCount, viewModel.Shelves);
        Assert.Contains(historicalRental, rentals);
        Assert.IsEmpty(viewModel.ShelfMessage);

        foreach (var row in viewModel.VisibleShelves)
        {
            Assert.AreNotSame(shelf, row.Shelf);
        }
    }

    [TestMethod]
    public void DeactivateShelf_WhenShelfHasCurrentRental_LeavesShelfActive()
    {
        // Arrange
        DateTime today = DateTime.Today;
        var rentals = new ObservableCollection<Rental>();
        var viewModel = CreateViewModel(rentals);
        Shelf shelf = viewModel.Shelves[0];
        Tenant tenant = new() { TenantId = 1, Name = "Test Tenant" };

        Rental rental = new(tenant, shelf)
        {
            RentalId = 1,
            StartDate = today.AddDays(-1),
            EndDate = null,
            MonthlyRent = 850m
        };

        rentals.Add(rental);
        ShelfRowViewModel row = viewModel.VisibleShelves[0];
        viewModel.SelectedShelfRow = row;

        // Act
        viewModel.DeactivateShelfCommand.Execute(null);

        // Assert
        Assert.IsTrue(shelf.IsActive);
        Assert.Contains(shelf, viewModel.Shelves);
        Assert.Contains(rental, rentals);
        Assert.Contains(row, viewModel.VisibleShelves);
        Assert.AreSame(row, viewModel.SelectedShelfRow);
        Assert.IsFalse(string.IsNullOrWhiteSpace(viewModel.ShelfMessage));
    }

    [TestMethod]
    public void DeactivateShelf_WhenShelfHasFutureRental_LeavesShelfActive()
    {
        // Arrange
        DateTime today = DateTime.Today;
        var rentals = new ObservableCollection<Rental>();
        var viewModel = CreateViewModel(rentals);
        Shelf shelf = viewModel.Shelves[0];
        Tenant tenant = new() { TenantId = 1, Name = "Test Tenant" };

        Rental rental = new(tenant, shelf)
        {
            RentalId = 1,
            StartDate = today.AddDays(1),
            EndDate = null,
            MonthlyRent = 850m
        };

        rentals.Add(rental);
        ShelfRowViewModel row = viewModel.VisibleShelves[0];
        viewModel.SelectedShelfRow = row;

        // Act
        viewModel.DeactivateShelfCommand.Execute(null);

        // Assert
        Assert.IsTrue(shelf.IsActive);
        Assert.Contains(shelf, viewModel.Shelves);
        Assert.Contains(rental, rentals);
        Assert.Contains(row, viewModel.VisibleShelves);
        Assert.AreSame(row, viewModel.SelectedShelfRow);
        Assert.IsFalse(string.IsNullOrWhiteSpace(viewModel.ShelfMessage));
    }

    [TestMethod]
    public void ReactivateShelf_WhenInactive_RestoresSameShelfAndPreservesHistory()
    {
        // Arrange
        var rentals = new ObservableCollection<Rental>();
        var viewModel = CreateViewModel(rentals);
        Shelf shelf = viewModel.Shelves[0];
        int originalCount = viewModel.Shelves.Count;
        Tenant tenant = new() { TenantId = 1, Name = "Test Tenant" };

        Rental rental = new(tenant, shelf)
        {
            StartDate = new DateTime(2025, 1, 1),
            EndDate = new DateTime(2025, 1, 31)
        };

        rentals.Add(rental);
        shelf.IsActive = false;
        viewModel.ShowInactiveShelves = true;
        viewModel.SelectedShelfRow = viewModel.VisibleShelves[0];

        // Act
        viewModel.ReactivateShelfCommand.Execute(null);

        // Assert
        Assert.IsTrue(shelf.IsActive);
        Assert.HasCount(originalCount, viewModel.Shelves);
        Assert.Contains(shelf, viewModel.Shelves);
        Assert.Contains(rental, rentals);
        Assert.AreSame(shelf, rental.Shelf);
        Assert.IsEmpty(viewModel.VisibleShelves);
        Assert.IsNull(viewModel.SelectedShelfRow);
        Assert.IsEmpty(viewModel.ShelfMessage);

        // Return to the active list; the default status filter is All
        viewModel.ShowInactiveShelves = false;
        Assert.HasCount(originalCount, viewModel.VisibleShelves);
        Assert.AreSame(shelf, viewModel.VisibleShelves[0].Shelf);
    }

    [TestMethod]
    public void ShowInactiveShelves_WhenStatusFilterIsRented_ShowsOnlyInactiveShelves()
    {
        // Arrange
        var viewModel = CreateViewModel();
        Shelf inactiveShelf = viewModel.Shelves[0];
        inactiveShelf.IsActive = false;
        viewModel.SelectedStatusFilter = ShelfStatusFilter.Rented;

        // Act
        viewModel.ShowInactiveShelves = true;

        // Assert
        Assert.HasCount(1, viewModel.VisibleShelves);
        Assert.AreSame(inactiveShelf, viewModel.VisibleShelves[0].Shelf);
        Assert.IsFalse(inactiveShelf.IsActive);
        Assert.IsNull(viewModel.SelectedShelfRow);
    }

    private static ShelfViewModel CreateViewModel(
    ObservableCollection<Rental>? rentals = null)
    {
        var shelfTypeRepository = new FakeShelfTypeRepository();

        ShelfType sixShelves = new()
        {
            Name = "6 hylder"
        };
        ShelfType clothesRail = new()
        {
            Name = "3 hylder og bøjlestang"
        };

        shelfTypeRepository.Add(sixShelves);
        shelfTypeRepository.Add(clothesRail);

        var shelfRepository = new FakeShelfRepository();

        shelfRepository.Add(new Shelf(sixShelves)
        {
            ShelfNumber = 1
        });

        shelfRepository.Add(new Shelf(clothesRail)
        {
            ShelfNumber = 2
        });

        return new ShelfViewModel(
            rentals ?? new ObservableCollection<Rental>(),
            shelfRepository,
            shelfTypeRepository);
    }
}
