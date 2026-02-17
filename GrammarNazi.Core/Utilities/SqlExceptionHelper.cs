using Microsoft.Data.SqlClient;
using System;
using System.Linq;

namespace GrammarNazi.Core.Utilities;

public static class SqlExceptionHelper
{
    private static readonly int[] TransientNumbers = { 53, 258, 10054, 10060, 4060, 10928, 10929, 40197, 40501, 40613 };

    private static readonly string[] TransientMessages =
    {
        "TCP Provider",
        "error: 40",
        "Could not open a connection to SQL Server",
        "server was not found or was not accessible",
        "A network-related or instance-specific error occurred"
    };

    public static bool IsTransient(SqlException ex) => IsTransient(ex.Number, ex.Message);

    public static bool IsTransient(int number, string message)
    {
        if (TransientNumbers.Contains(number))
        {
            return true;
        }

        return TransientMessages.Any(m => message?.Contains(m, StringComparison.OrdinalIgnoreCase) == true);
    }
}
