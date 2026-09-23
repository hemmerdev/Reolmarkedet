using Microsoft.Data.SqlClient;
using Reolmarkedet.Core.Interfaces;
using Reolmarkedet.Core.Models;

namespace Reolmarkedet.Data.Repositories
{
    public class SqlRentalRepository : IRepository<Rental>
    {
        private readonly string _connectionString;
        public SqlRentalRepository(string connectionString)
        {
            _connectionString = connectionString;
        }
        public IEnumerable<Rental> GetAll()
        {
            var rentals = new List<Rental>();
            string query =
                @"SELECT r.RentalId, r.StartDate, r.EndDate, 
                r.TerminationNoticeDate, r.MonthlyRent, 
                t.TenantId, t.Name, t.Phone, t.Email, t.IsActive, 
                s.ShelfId, s.ShelfNumber, s.IsActive, 
                st.ShelfTypeId, st.ShelfTypeName 
                FROM dbo.RENTAL AS r 
                INNER JOIN dbo.TENANT AS t 
                    ON r.TenantId = t.TenantId 
                INNER JOIN dbo.SHELF AS s 
                    ON r.ShelfId = s.ShelfId 
                INNER JOIN dbo.SHELFTYPE AS st 
                    ON s.ShelfTypeId = st.ShelfTypeId";


            using (SqlConnection connection = new(_connectionString))
            {
                SqlCommand command = new(query, connection);
                connection.Open();

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        rentals.Add(CreateRentalFromReader(reader));
                    }
                }
            }

            return rentals;
        }

        public Rental? GetById(int id)
        {
            Rental? rental = null;
            string query =
                @"SELECT r.RentalId, r.StartDate, r.EndDate, 
                r.TerminationNoticeDate, r.MonthlyRent, 
                t.TenantId, t.Name, t.Phone, t.Email, t.IsActive, 
                s.ShelfId, s.ShelfNumber, s.IsActive, 
                st.ShelfTypeId, st.ShelfTypeName 
                FROM dbo.RENTAL AS r 
                INNER JOIN dbo.TENANT AS t 
                    ON r.TenantId = t.TenantId 
                INNER JOIN dbo.SHELF AS s 
                    ON r.ShelfId = s.ShelfId 
                INNER JOIN dbo.SHELFTYPE AS st 
                    ON s.ShelfTypeId = st.ShelfTypeId 
                WHERE r.RentalId = @RentalId";

            using (SqlConnection connection = new(_connectionString))
            {
                SqlCommand command = new(query, connection);
                command.Parameters.AddWithValue("@RentalId", id);
                connection.Open();

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        rental = CreateRentalFromReader(reader);
                    }
                }
            }

            return rental;
        }

        public void Add(Rental rental)
        {
            string query = @"INSERT INTO dbo.RENTAL
                                (StartDate, EndDate, TerminationNoticeDate,
                                 MonthlyRent, TenantID, ShelfID)
                            OUTPUT INSERTED.RentalID
                            VALUES
                                (@StartDate, @EndDate, @TerminationNoticeDate,
                                 @MonthlyRent, @TenantID, @ShelfID)";

            using (SqlConnection connection = new(_connectionString))
            {
                SqlCommand command = new(query, connection);

                command.Parameters.AddWithValue("@StartDate", rental.StartDate);
                command.Parameters.AddWithValue("@EndDate", (object?)rental.EndDate ?? DBNull.Value);
                command.Parameters.AddWithValue("@TerminationNoticeDate", (object?)rental.TerminationNoticeDate ?? DBNull.Value);
                command.Parameters.AddWithValue("@MonthlyRent", rental.MonthlyRent);
                command.Parameters.AddWithValue("@TenantID", rental.Tenant.TenantId);
                command.Parameters.AddWithValue("@ShelfID", rental.Shelf.ShelfId);

                connection.Open();
                object? result = command.ExecuteScalar();

                if (result is not int rentalId)
                {
                    throw new InvalidOperationException(
                        "The database did not return a RentalId");
                }

                rental.RentalId = rentalId;
            }
        }

        public void Update(Rental rental)
        {
            string query = @"UPDATE dbo.RENTAL
                            SET StartDate = @StartDate,
                                EndDate = @EndDate,
                                TerminationNoticeDate = @TerminationNoticeDate,
                                MonthlyRent = @MonthlyRent,
                                TenantId = @TenantId,
                                ShelfId = @ShelfId
                            WHERE RentalId = @RentalId";

            using (SqlConnection connection = new(_connectionString))
            {
                SqlCommand command = new(query, connection);

                command.Parameters.AddWithValue("@StartDate", rental.StartDate);
                command.Parameters.AddWithValue("@EndDate", (object?)rental.EndDate ?? DBNull.Value);
                command.Parameters.AddWithValue("@TerminationNoticeDate", (object?)rental.TerminationNoticeDate ?? DBNull.Value);
                command.Parameters.AddWithValue("@MonthlyRent", rental.MonthlyRent);
                command.Parameters.AddWithValue("@TenantId", rental.Tenant.TenantId);
                command.Parameters.AddWithValue("@ShelfId", rental.Shelf.ShelfId);
                command.Parameters.AddWithValue("@RentalId", rental.RentalId);

                connection.Open();

                int rowsAffected = command.ExecuteNonQuery();
                if (rowsAffected == 0)
                {
                    throw new InvalidOperationException(
                        $"No rentals found with RentalId {rental.RentalId}");
                }
            }
        }
        public void Delete(int id)
        {
            string query = @"DELETE FROM dbo.RENTAL 
                           WHERE RentalId = @RentalId";

            using (SqlConnection connection = new(_connectionString))
            {
                SqlCommand command = new(query, connection);
                command.Parameters.AddWithValue("@RentalId", id);

                connection.Open();

                int rowsAffected = command.ExecuteNonQuery();
                if (rowsAffected == 0)
                {
                    throw new InvalidOperationException(
                        $"No rentals found with RentalId {id}");
                }
            }
        }

        private Rental CreateRentalFromReader(SqlDataReader reader)
        {
            Tenant tenant = new Tenant
            {
                TenantId = reader.GetInt32(5),
                Name = reader.GetString(6),
                PhoneNumber = reader.IsDBNull(7) ? null : reader.GetString(7),
                Email = reader.IsDBNull(8) ? null : reader.GetString(8),
                IsActive = reader.GetBoolean(9)
            };
            ShelfType shelfType = new ShelfType
            {
                ShelfTypeId = reader.GetInt32(13),
                Name = reader.GetString(14)
            };
            Shelf shelf = new Shelf(shelfType)
            {
                ShelfId = reader.GetInt32(10),
                ShelfNumber = reader.GetInt32(11),
                IsActive = reader.GetBoolean(12),
            };

            return new Rental(tenant, shelf)
            {
                RentalId = reader.GetInt32(0),
                StartDate = reader.GetDateTime(1),
                EndDate = reader.IsDBNull(2) ? null : reader.GetDateTime(2),
                TerminationNoticeDate =
                    reader.IsDBNull(3) ? null : reader.GetDateTime(3),
                MonthlyRent = reader.GetDecimal(4)
            };
        }
    }
}
