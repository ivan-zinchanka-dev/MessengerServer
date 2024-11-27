namespace MessengerServer.Server.Database;

public class DatabaseOptions
{
    public string ConnectionString { get; private set; }

    public DatabaseOptions(string connectionString)
    {
        ConnectionString = connectionString;
    }
}