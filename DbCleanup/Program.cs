using System;
using System.Threading.Tasks;
using Npgsql;

namespace DbCleanup
{
    class Program
    {
        static async Task Main(string[] args)
        {
            string connectionString = "Host=localhost;Port=5432;Database=dbHotelBooking;Username=postgres;Password=1729";
            
            try 
            {
                await using var conn = new NpgsqlConnection(connectionString);
                await conn.OpenAsync();

                // Because of cascade delete, deleting the user will delete the business and business documents
                string sql = "DELETE FROM users WHERE email IN ('anhngoctran11223@gmail.com', 'an.nguyen@gmail.com');";

                await using var cmd = new NpgsqlCommand(sql, conn);
                int rowsAffected = await cmd.ExecuteNonQueryAsync();

                Console.WriteLine($"Deleted {rowsAffected} users (and associated businesses via cascade) successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }
}
