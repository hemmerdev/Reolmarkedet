using Reolmarkedet.Core.Models;
using Reolmarkedet.WPF.ViewModels;
using System.Collections.ObjectModel;

namespace ReolMarkedet.Tests;

[TestClass]
public class TenantViewModelTests
{
    [TestMethod]
    public void UpdateTenant_WhenNameIsBlank_LeavesTenantUnchanged()
    {
        // Arrange
        TenantViewModel viewModel = new(
            new ObservableCollection<Rental>(),
            new FakeTenantRepository());
        viewModel.Name = "John Doe";
        viewModel.AddTenantCommand.Execute(null);

        Tenant tenant = viewModel.Tenants[0];
        viewModel.SelectedTenant = tenant;
        viewModel.Name = ""; // Set name to blank

        // Act
        viewModel.UpdateTenantCommand.Execute(null);

        // Assert
        Assert.AreEqual("John Doe", tenant.Name);
        Assert.AreSame(tenant, viewModel.SelectedTenant);
        Assert.IsFalse(string.IsNullOrWhiteSpace(viewModel.ValidationMessage));
    }

    [TestMethod]
    public void CancelUpdateTenant_WhenDraftHasChanges_LeavesTenantUnchanged()
    {
        // Arrange
        TenantViewModel viewModel = new(
            new ObservableCollection<Rental>(),
            new FakeTenantRepository());
        viewModel.Name = "John Doe";
        viewModel.AddTenantCommand.Execute(null);

        Tenant tenant = viewModel.Tenants[0];
        viewModel.SelectedTenant = tenant;
        viewModel.Name = "Changed Name";

        // Act
        viewModel.CancelUpdateTenantCommand.Execute(null);

        // Assert
        Assert.AreEqual("John Doe", tenant.Name);
        Assert.IsNull(viewModel.SelectedTenant);
        Assert.IsEmpty(viewModel.Name);
    }

    [TestMethod]
    public void DeactivateTenant_WhenOnlyHistoricalRentalsExist_PreservesHistory()
    {
        // Arrange
        var rentals = new ObservableCollection<Rental>();
        var viewModel = new TenantViewModel(rentals, new FakeTenantRepository());
        viewModel.Name = "Test Tenant";
        viewModel.AddTenantCommand.Execute(null);

        Tenant tenant = viewModel.Tenants[0];
        Shelf shelf = new(new ShelfType { Name = "Test Type" })
        {
            ShelfId = 1
        };

        Rental rental = new(tenant, shelf)
        {
            StartDate = new DateTime(2025, 1, 1),
            EndDate = new DateTime(2025, 1, 31)
        };

        rentals.Add(rental);
        viewModel.SelectedTenant = tenant;

        // Act
        viewModel.DeactivateTenantCommand.Execute(null);

        // Assert
        Assert.IsFalse(tenant.IsActive);
        Assert.Contains(tenant, viewModel.Tenants);
        Assert.Contains(rental, rentals);
        Assert.IsNull(viewModel.SelectedTenant);
        Assert.IsEmpty(viewModel.ValidationMessage);
    }

    [TestMethod]
    public void DeactivateTenant_WhenCurrentRentalExists_LeavesTenantActive()
    {
        // Arrange
        var rentals = new ObservableCollection<Rental>();
        var viewModel = new TenantViewModel(rentals, new FakeTenantRepository());
        viewModel.Name = "Test Tenant";
        viewModel.AddTenantCommand.Execute(null);

        Tenant tenant = viewModel.Tenants[0];
        Shelf shelf = new(new ShelfType { Name = "Test Type" })
        {
            ShelfId = 1
        };

        Rental rental = new(tenant, shelf)
        {
            StartDate = DateTime.Today.AddDays(-1),
            EndDate = null
        };

        rentals.Add(rental);
        viewModel.SelectedTenant = tenant;

        // Act
        viewModel.DeactivateTenantCommand.Execute(null);

        // Assert
        Assert.IsTrue(tenant.IsActive);
        Assert.Contains(tenant, viewModel.Tenants);
        Assert.Contains(rental, rentals);
        Assert.AreSame(tenant, viewModel.SelectedTenant);
        Assert.IsFalse(string.IsNullOrWhiteSpace(viewModel.ValidationMessage));
    }

    [TestMethod]
    public void DeactivateTenant_WhenFutureRentalExists_LeavesTenantActive()
    {
        // Arrange
        var rentals = new ObservableCollection<Rental>();
        var viewModel = new TenantViewModel(rentals, new FakeTenantRepository());
        viewModel.Name = "Test Tenant";
        viewModel.AddTenantCommand.Execute(null);

        Tenant tenant = viewModel.Tenants[0];
        Shelf shelf = new(new ShelfType { Name = "Test Type" })
        {
            ShelfId = 1
        };

        Rental rental = new(tenant, shelf)
        {
            StartDate = DateTime.Today.AddDays(1),
            EndDate = null
        };

        rentals.Add(rental);
        viewModel.SelectedTenant = tenant;

        // Act
        viewModel.DeactivateTenantCommand.Execute(null);

        // Assert
        Assert.IsTrue(tenant.IsActive);
        Assert.Contains(tenant, viewModel.Tenants);
        Assert.Contains(rental, rentals);
        Assert.AreSame(tenant, viewModel.SelectedTenant);
        Assert.IsFalse(string.IsNullOrWhiteSpace(viewModel.ValidationMessage));
    }

    [TestMethod]
    public void ReactivateTenant_WhenInactive_RestoresSameTenantAndPreservesHistory()
    {
        // Arrange
        var rentals = new ObservableCollection<Rental>();
        var viewModel = new TenantViewModel(rentals, new FakeTenantRepository());
        viewModel.Name = "Test Tenant";
        viewModel.AddTenantCommand.Execute(null);

        Tenant tenant = viewModel.Tenants[0];
        Shelf shelf = new(new ShelfType { Name = "Test Type" })
        {
            ShelfId = 1
        };

        Rental rental = new(tenant, shelf)
        {
            StartDate = new DateTime(2025, 1, 1),
            EndDate = new DateTime(2025, 1, 31)
        };

        rentals.Add(rental);
        tenant.IsActive = false;
        viewModel.ShowInactiveTenants = true;
        viewModel.SelectedTenant = viewModel.VisibleTenants[0];

        // Act
        viewModel.ReactivateTenantCommand.Execute(null);

        // Assert
        Assert.IsTrue(tenant.IsActive);
        Assert.HasCount(1, viewModel.Tenants);
        Assert.Contains(tenant, viewModel.Tenants);
        Assert.Contains(rental, rentals);
        Assert.AreSame(tenant, rental.Tenant);
        Assert.IsEmpty(viewModel.VisibleTenants);
        Assert.IsNull(viewModel.SelectedTenant);
        Assert.IsEmpty(viewModel.ValidationMessage);

        // Return to the active list
        viewModel.ShowInactiveTenants = false;
        Assert.HasCount(1, viewModel.VisibleTenants);
        Assert.AreSame(tenant, viewModel.VisibleTenants[0]);
    }
}
