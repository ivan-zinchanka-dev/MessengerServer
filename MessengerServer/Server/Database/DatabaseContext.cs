using System.Data;
using MessengerCoreLibrary.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace MessengerServer.Server.Database;

public class DatabaseContext : IAsyncDisposable
{
    private static class QueryStrings
    {
        public const string FindUserExpression = "SELECT [Nickname] FROM [User] WHERE [Nickname] = @Nickname AND [Password] = @Password";
        public const string CreateUserExpression = "INSERT INTO [User] VALUES (@Nickname, @Password)";
        public const string GetAllSortedMessagesExpression = "SELECT * FROM [Message] ORDER BY [PostDateTime]";
        public const string PostMessageExpression = "INSERT INTO [Message] VALUES (@SenderNickname, @ReceiverNickname, @Text, @PostDateTime)";
    }
    
    private readonly DatabaseOptions _options;
    private readonly ILogger<DatabaseContext> _logger;
    private readonly SqlConnection _connection;
    
    public DatabaseContext(DatabaseOptions options, ILogger<DatabaseContext> logger)
    {
        _options = options;
        _logger = logger;
        _connection = new SqlConnection(_options.ConnectionString);
    }

    public async Task ConnectToDatabaseAsync()
    {
        try
        {
            await _connection.OpenAsync();
            _logger.LogInformation("The client has successfully connected to the database.");
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex.Message);
        }
    }

    public async Task<bool> IsUserExistsAsync(User user)
    {
        try
        {
            SqlCommand command = new SqlCommand(QueryStrings.FindUserExpression, _connection);
            command.Parameters.Add(new SqlParameter("@Nickname", user.Nickname));
            command.Parameters.Add(new SqlParameter("@Password", user.Password));
            
            SqlDataReader reader = await command.ExecuteReaderAsync();

            bool result = reader.HasRows;
            await reader.CloseAsync();
            
            return result;
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex.Message);
        }

        return false;
    }
    
    public async Task<bool> CreateUserAsync(User user)
    {
        try
        {
            SqlCommand command = new SqlCommand(QueryStrings.CreateUserExpression, _connection);
            command.Parameters.Add(new SqlParameter("@Nickname", user.Nickname));
            command.Parameters.Add(new SqlParameter("@Password", user.Password));
            
            int affectedRows = await command.ExecuteNonQueryAsync();
            return affectedRows > 0;
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex.Message);
        }

        return false;
    }
    
    public async Task<LinkedList<Message>> GetAllMessagesAsync()
    {
        try
        {
            SqlCommand command = new SqlCommand(QueryStrings.GetAllSortedMessagesExpression, _connection);
            SqlDataReader reader = await command.ExecuteReaderAsync();
            
            LinkedList<Message> messages = new LinkedList<Message>();
            
            if (reader.HasRows)
            {
                while (reader.Read())
                {
                    Message message = new Message(
                        reader.GetString("SenderNickname"),
                        reader.GetStringSafe("ReceiverNickname"),
                        reader.GetString("Text"),
                        reader.GetDateTime("PostDateTime"));
                    
                    messages.AddLast(message);
                }
            }

            await reader.CloseAsync();
            return messages;
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex.Message);
            return null;
        }
    }

    public async Task<bool> PostMessageAsync(Message message)
    {
        try
        {
            SqlCommand command = new SqlCommand(QueryStrings.PostMessageExpression, _connection);
            command.Parameters.Add(new SqlParameter("@SenderNickname", message.SenderNickname));
            command.Parameters.Add(new SqlParameter("@ReceiverNickname", ToNullableDbObject(message.ReceiverNickname)));
            command.Parameters.Add(new SqlParameter("@Text", message.Text));
            command.Parameters.Add(new SqlParameter("@PostDateTime", message.PostDateTime));

            int affectedRows = await command.ExecuteNonQueryAsync();
            return affectedRows > 0;
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex.Message);
            return false;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection.State == ConnectionState.Open)
        {
            await _connection.CloseAsync();
            _logger.LogInformation("The client has disconnected from the database.");
        }
    }

    private static object ToNullableDbObject<T>(T source) where T : class
    {
        return source != null ? source : DBNull.Value;
    }
}