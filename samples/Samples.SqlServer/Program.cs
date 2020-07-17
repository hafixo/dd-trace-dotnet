using System;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;
using Samples.DatabaseHelper;

namespace Samples.SqlServer
{
    internal static class Program
    {
        private static async Task Main()
        {
            var cts = new CancellationTokenSource();
            var commandFactory = new DbCommandFactory();

            var commandExecutor = new DbCommandExecutor<SqlCommand, SqlDataReader>(
                command => command.ExecuteNonQuery(),
                command => command.ExecuteScalar(),
                command => command.ExecuteReader(),
                (command, behavior) => command.ExecuteReader(behavior),
                command => command.ExecuteNonQueryAsync(),
                (command, ct) => command.ExecuteNonQueryAsync(ct),
                command => command.ExecuteScalarAsync(),
                (command, ct) => command.ExecuteScalarAsync(ct),
                command => command.ExecuteReaderAsync(),
                (command, behavior) => command.ExecuteReaderAsync(behavior),
                (command, ct) => command.ExecuteReaderAsync(ct),
                (command, behavior, ct) => command.ExecuteReaderAsync(behavior, ct));

            using (var connection = CreateConnection())
            {
                await RelationalDatabaseTestHarness.RunAllAsync(connection, commandFactory, commandExecutor, cts.Token);
            }

            // allow time to flush
            await Task.Delay(2000, cts.Token);
        }

        private static SqlConnection CreateConnection()
        {
            var connectionString = Environment.GetEnvironmentVariable("SQLSERVER_CONNECTION_STRING") ??
                                   @"Server=(localdb)\MSSQLLocalDB;Integrated Security=true;Connection Timeout=30";

            return new SqlConnection(connectionString);
        }
    }
}
