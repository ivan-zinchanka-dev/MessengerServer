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

        public const string NicknameParam = "@Nickname";
        public const string PasswordParam = "@Password";
        
        public const string SenderNicknameParam = "@SenderNickname";
        public const string ReceiverNicknameParam = "@ReceiverNickname";
        public const string TextParam = "@Text";
        public const string PostDateTimeParam = "@PostDateTime";
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
            command.Parameters.Add(new SqlParameter(QueryStrings.NicknameParam, user.Nickname));
            command.Parameters.Add(new SqlParameter(QueryStrings.PasswordParam, user.Password));
            
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
            command.Parameters.Add(new SqlParameter(QueryStrings.NicknameParam, user.Nickname));
            command.Parameters.Add(new SqlParameter(QueryStrings.PasswordParam, user.Password));
            
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
                        reader.GetString(nameof(message.SenderNickname)),
                        reader.GetStringSafe(nameof(message.ReceiverNickname)),
                        reader.GetString(nameof(message.Text)),
                        reader.GetDateTime(nameof(message.PostDateTime)));
                    
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
            command.Parameters.Add(new SqlParameter(QueryStrings.SenderNicknameParam, message.SenderNickname));
            command.Parameters.Add(new SqlParameter(QueryStrings.ReceiverNicknameParam, ToNullableDbObject(message.ReceiverNickname)));
            command.Parameters.Add(new SqlParameter(QueryStrings.TextParam, message.Text));
            command.Parameters.Add(new SqlParameter(QueryStrings.PostDateTimeParam, message.PostDateTime));

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