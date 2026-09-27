namespace Reolmarkedet.Core.Models
{
    public class Tenant
    {
        public int TenantId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
        public string? BankRegistrationNumber { get; set; }
        public string? BankAccountNumber { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
