using Microsoft.Data.SqlClient;

namespace Reolmarkedet.Data.Database
{
    public class DatabaseConnectionTester
    {
        private readonly string _connectionString;

        public DatabaseConnectionTester(string connectionString)
        {
            _connectionString = connectionString;
        }


        public void TestConnection()
        {
            // Create the connection 
            using SqlConnection connection = new(_connectionString);

            // Open the connection
            connection.Open();

            // Defines a simple query that ask the SQL server to return 1
            const string sql = "SELECT 1";

            // Creates a sql command 
            using SqlCommand command = new(sql, connection);

            // Executes the command and retrieves the result
            object? result = command.ExecuteScalar();

            // Checks the result and throws an exception if it's not as expected
            if (result is not int value || value != 1)
            {
                throw new InvalidOperationException(
                    "The database connection test returned an unexpected result.");
            }
        }

    }
}
