using Microsoft.Data.SqlClient;
using Reolmarkedet.Core.Interfaces;
using Reolmarkedet.Core.Models;

namespace Reolmarkedet.Data.Repositories
{
    public class SqlTenantRepository : IRepository<Tenant>
    {
        private readonly string _connectionString;

        public SqlTenantRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public IEnumerable<Tenant> GetAll()
        {
            var tenants = new List<Tenant>();
            string query = "SELECT TenantId, Name, Phone, Email, IsActive " +
                           "FROM dbo.TENANT";

            using (SqlConnection connection = new(_connectionString))
            {
                SqlCommand command = new(query, connection);
                connection.Open();

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        tenants.Add(new Tenant
                        {
                            TenantId = reader.GetInt32(0),
                            Name = reader.GetString(1),
                            PhoneNumber = reader.IsDBNull(2) ? null : reader.GetString(2),
                            Email = reader.IsDBNull(3) ? null : reader.GetString(3),
                            IsActive = reader.GetBoolean(4)
                        });
                    }
                }
            }

            return tenants;
        }

        public Tenant? GetById(int id)
        {
            Tenant? tenant = null;

            string query = "SELECT TenantId, Name, Phone, Email, IsActive " +
                           "FROM dbo.TENANT " +
                           "WHERE TenantId = @TenantId";

            using (SqlConnection connection = new(_connectionString))
            {
                SqlCommand command = new(query, connection);
                command.Parameters.AddWithValue("@TenantId", id);
                connection.Open();

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        tenant = new Tenant
                        {
                            TenantId = reader.GetInt32(0),
                            Name = reader.GetString(1),
                            PhoneNumber = reader.IsDBNull(2) ? null : reader.GetString(2),
                            Email = reader.IsDBNull(3) ? null : reader.GetString(3),
                            IsActive = reader.GetBoolean(4)
                        };
                    }
                }
            }

            return tenant;
        }

        public void Add(Tenant tenant)
        {
            string query = "INSERT INTO dbo.TENANT (Name, Phone, Email, IsActive) " +
                           "OUTPUT INSERTED.TenantId " +
                           "VALUES (@Name, @Phone, @Email, @IsActive)";

            using (SqlConnection connection = new(_connectionString))
            {
                SqlCommand command = new(query, connection);

                command.Parameters.AddWithValue("@Name", tenant.Name);
                command.Parameters.AddWithValue("@Phone", (object?)tenant.PhoneNumber ?? DBNull.Value);
                command.Parameters.AddWithValue("@Email", (object?)tenant.Email ?? DBNull.Value);
                command.Parameters.AddWithValue("@IsActive", tenant.IsActive);

                connection.Open();

                object? result = command.ExecuteScalar();

                if (result is not int tenantId)
                {
                    throw new InvalidOperationException(
                        "The database did not return a tenantId");
                }

                tenant.TenantId = tenantId;
            }
        }
        public void Update(Tenant tenant)
        {
            string query = "UPDATE dbo.TENANT " +
                           "SET Name = @Name, Phone = @Phone, Email = @Email, IsActive = @IsActive " +
                           "WHERE TenantId = @TenantId";

            using (SqlConnection connection = new(_connectionString))
            {
                SqlCommand command = new(query, connection);

                command.Parameters.AddWithValue("@TenantId", tenant.TenantId);
                command.Parameters.AddWithValue("@Name", tenant.Name);
                command.Parameters.AddWithValue("@Phone", (object?)tenant.PhoneNumber ?? DBNull.Value);
                command.Parameters.AddWithValue("@Email", (object?)tenant.Email ?? DBNull.Value);
                command.Parameters.AddWithValue("@IsActive", tenant.IsActive);

                connection.Open();

                int rowsAffected = command.ExecuteNonQuery();

                if (rowsAffected == 0)
                {
                    throw new InvalidOperationException(
                        "No tenant found with the specified TenantId.");
                }
            }
        }

        public void Delete(int id)
        {
            string query = "DELETE FROM dbo.TENANT " +
                           "WHERE TenantId = @TenantId";

            using (SqlConnection connection = new(_connectionString))
            {
                SqlCommand command = new(query, connection);
                command.Parameters.AddWithValue("@TenantId", id);
                connection.Open();

                int rowsAffected = command.ExecuteNonQuery();

                if (rowsAffected == 0)
                {
                    throw new InvalidOperationException(
                        "No tenant found with the specified TenantId.");
                }
            }
        }
    }
}
