using Reolmarkedet.Core.Models;
using Reolmarkedet.Core.Models.enums;
using Reolmarkedet.WPF.ViewModels;
using ReolMarkedet.Tests.Fakes;
using System.Collections.ObjectModel;

namespace ReolMarkedet.Tests;

[TestClass]
public class RentalViewModelTests
{

    [TestMethod]
    public void CreateRentals_WhenTwoShelvesSelected_CreatesOneRentalPerShelf()
    {
        // Arrange
        Tenant tenant = new() { TenantId = 1, Name = "Test Tenant" };
        ShelfType shelfType = new() { ShelfTypeId = 1, Name = "Test Type" };
        Shelf firstShelf = new(shelfType) { ShelfId = 1, ShelfNumber = 1 };
        Shelf secondShelf = new(shelfType) { ShelfId = 2, ShelfNumber = 2 };
        var rentalRepository = new FakeRentalRepository();
        List<Rental> existingRentals = new();
        var tomorrow = DateTime.Now.AddDays(1);

        RentalViewModel viewModel = new(
            new ObservableCollection<Tenant> { tenant },
            new ObservableCollection<Shelf> { firstShelf, secondShelf },
            new ObservableCollection<Rental>(existingRentals),
            rentalRepository,
            new FakeConfirmationService())
        {
            SelectedTenant = tenant,
            SelectedPaymentMethod = PaymentMethod.Card,
            StartDate = tomorrow.Date,
            EndDate = null
        };

        viewModel.SelectedShelf = firstShelf;
        viewModel.AddShelfToSelectionCommand.Execute(null);

        viewModel.SelectedShelf = secondShelf;
        viewModel.AddShelfToSelectionCommand.Execute(null);

        // Act
        viewModel.CreateRentalsCommand.Execute(null);

        // Assert
        Assert.HasCount(2, viewModel.Rentals);

        Rental firstRental = viewModel.Rentals[0];
        Rental secondRental = viewModel.Rentals[1];

        Assert.AreSame(firstShelf, firstRental.Shelf);
        Assert.AreSame(secondShelf, secondRental.Shelf);
        Assert.AreNotEqual(firstRental.RentalId, secondRental.RentalId);

        foreach (Rental rental in viewModel.Rentals)
        {
            Assert.AreSame(tenant, rental.Tenant);
            Assert.AreEqual(PaymentMethod.Card, rental.InitialPaymentMethod);
            Assert.AreEqual(tomorrow.Date, rental.StartDate);
            Assert.IsNull(rental.EndDate);
        }
        Assert.IsNull(viewModel.SelectedPaymentMethod);
        Assert.AreEqual(850m, firstRental.MonthlyRent);
        Assert.AreEqual(825m, secondRental.MonthlyRent);
    }

    [TestMethod]
    public void FirstPeriodTotal_WhenTwoShelvesSelected_ReturnsCorrectTotalWithoutSaving()
    {
        // Arrange
        Tenant tenant = new() { TenantId = 1, Name = "Test Tenant" };
        ShelfType shelfType = new() { ShelfTypeId = 1, Name = "Test Type" };
        Shelf firstShelf = new(shelfType) { ShelfId = 1, ShelfNumber = 1 };
        Shelf secondShelf = new(shelfType) { ShelfId = 2, ShelfNumber = 2 };
        FakeRentalRepository rentalRepository = new();

        RentalViewModel viewModel = new(
            new ObservableCollection<Tenant> { tenant },
            new ObservableCollection<Shelf> { firstShelf, secondShelf },
            new ObservableCollection<Rental>(),
            rentalRepository,
            new FakeConfirmationService())
        {
            SelectedTenant = tenant,
            SelectedPaymentMethod = PaymentMethod.Card,
            StartDate = new DateTime(2026, 10, 16),
            EndDate = null
        };

        viewModel.SelectedShelf = firstShelf;
        viewModel.AddShelfToSelectionCommand.Execute(null);

        viewModel.SelectedShelf = secondShelf;
        viewModel.AddShelfToSelectionCommand.Execute(null);

        // Act
        decimal? firstPeriodTotal = viewModel.FirstPeriodTotal;

        // Assert
        Assert.AreEqual((decimal?)864.52m, firstPeriodTotal);
        Assert.HasCount(0, viewModel.Rentals);
        Assert.IsFalse(rentalRepository.GetAll().Any());
    }
}
