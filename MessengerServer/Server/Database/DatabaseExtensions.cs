using System.Data;
using Microsoft.Data.SqlClient;

namespace MessengerServer.Server.Database;

public static class DatabaseExtensions
{
    public static string GetStringSafe(this SqlDataReader reader, int colIndex)
    {
        return reader.IsDBNull(colIndex) ? string.Empty : reader.GetString(colIndex);
    }
    
    public static string GetStringSafe(this SqlDataReader reader, string colName)
    {
        return reader.IsDBNull(colName) ? string.Empty : reader.GetString(colName);
    }
}