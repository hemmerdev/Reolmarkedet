using Reolmarkedet.Core.Interfaces;
using Reolmarkedet.Core.Models;

namespace ReolMarkedet.Tests;

internal class FakeTenantRepository : IRepository<Tenant>
{
    private readonly List<Tenant> _tenants = new();
    private int _nextTenantId = 1;

    public IEnumerable<Tenant> GetAll()
    {
        return _tenants;
    }

    public Tenant? GetById(int id)
    {
        foreach (var tenant in _tenants)
        {
            if (tenant.TenantId == id)
            {
                return tenant;
            }
        }

        return null;
    }

    public void Add(Tenant tenant)
    {
        if (tenant.TenantId == 0)
        {
            tenant.TenantId = _nextTenantId++;
        }
        else if (tenant.TenantId >= _nextTenantId)
        {
            _nextTenantId = tenant.TenantId + 1;
        }

        _tenants.Add(tenant);
    }

    public void Update(Tenant tenant)
    {
        Tenant? storedTenant = GetById(tenant.TenantId);
        if (storedTenant is null)
        {
            throw new InvalidOperationException(
                "No tenant found with the specified TenantId");
        }

        storedTenant.Name = tenant.Name;
        storedTenant.PhoneNumber = tenant.PhoneNumber;
        storedTenant.Email = tenant.Email;
        storedTenant.IsActive = tenant.IsActive;
    }

    public void Delete(int id)
    {
        Tenant? tenant = GetById(id);

        if (tenant is null)
        {
            throw new InvalidOperationException(
                "No tenant found with the specified TenantId");
        }

        _tenants.Remove(tenant);
    }
}
