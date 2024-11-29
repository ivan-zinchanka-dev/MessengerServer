using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using MessengerCoreLibrary.Infrastructure;
using MessengerCoreLibrary.Models;
using MessengerServer.Server.Database;
using Microsoft.Extensions.Logging;

namespace MessengerServer.Server.Network;

public class AppServer : IAsyncDisposable
{
    private readonly AppServerOptions _options;
    private readonly DatabaseContext _databaseContext;
    
    private readonly object _stateLocker = new object();
    private bool _isRunning;
    private ConcurrentQueue<Message> _messages;
    
    private TcpListener _tcpListener;
    private UdpClient _udpReceiver;
    
    public ILogger<AppServer> Logger { get; private set; }

    private bool IsRunning
    {
        get
        {
            lock (_stateLocker)
            {
                return _isRunning;
            }
        }
        
        set
        {
            lock (_stateLocker)
            {
                _isRunning = value;
            }
        }
    }
    
    private static class Messages
    {
        public const string ServerStarted = "The server is running in a thread {0}.";
        public const string ClientLoopStarted = "The client loop is running in a thread {0}.";
        public const string ClientServicesLoopStarted = "The client services loop is running in a thread {0}.";
        public const string UnknownCommand = "Unknown command received: {0}.";
        public const string ExceptionOccured = "An exception occurred:\n{0}\n{1}";
    }
    
    public AppServer(AppServerOptions options, DatabaseContext databaseContext, ILogger<AppServer> logger)
    {
        _options = options;
        _databaseContext = databaseContext;
        Logger = logger;
    }
    
    public async void StartAsync()
    {
        try
        {
            await _databaseContext.ConnectToDatabaseAsync();
            _messages = new ConcurrentQueue<Message>(await _databaseContext.GetAllMessagesAsync());

            IsRunning = true;
            Logger.LogInformation(string.Format(Messages.ServerStarted, Thread.CurrentThread.ManagedThreadId));
            
            Task clientsLoop = Task.Run(StartClientsLoopAsync);
            Task clientServicesLoop = Task.Run(StartClientServicesLoopAsync);
            
            await Task.WhenAll(clientsLoop, clientServicesLoop);

        }
        catch (Exception ex)
        {
            Logger.LogCritical(string.Format(Messages.ExceptionOccured, ex.Message, ex.StackTrace));
        }
        finally
        {
            await DisposeAsync();
        }
    }
    
    private async Task StartClientsLoopAsync()
    {
        _tcpListener = new TcpListener(IPAddress.Any, _options.ClientPort);
        _tcpListener.Start();
    
        Logger.LogInformation(string.Format(Messages.ClientLoopStarted, Thread.CurrentThread.ManagedThreadId));
        
        while (IsRunning)
        {
            TcpClient tcpClient = await _tcpListener.AcceptTcpClientAsync();
            Task.Run(() => HandleClientAsync(tcpClient));
        }
    }

    private async Task StartClientServicesLoopAsync()
    {
        _udpReceiver = new UdpClient(_options.ClientServicePort);

        Logger.LogInformation(string.Format(Messages.ClientServicesLoopStarted, Thread.CurrentThread.ManagedThreadId));
        
        while (IsRunning)
        {
            UdpReceiveResult newResult = await _udpReceiver.ReceiveAsync();
            Task.Run(() => HandleClientServiceAsync(newResult));
        }
    }
    
    private async void HandleClientAsync(TcpClient tcpClient)
    {
        try
        {
            NetworkAdaptor networkAdaptor = new NetworkAdaptor(tcpClient.GetStream());
            bool quitCommandReceived = false;
            
            while (!quitCommandReceived)
            {
                Query query = await networkAdaptor.ReceiveQueryAsync();
                Response response;
                
                switch (query.Header)
                {
                    case QueryHeader.SignIn:
                        response = await SignInAsync(query.JsonDataString);
                        await networkAdaptor.SendResponseAsync(response);
                        break;
                    
                    case QueryHeader.SignUp:
                        response = await SignUpAsync(query.JsonDataString);
                        await networkAdaptor.SendResponseAsync(response);
                        break;

                    case QueryHeader.UpdateChat:
                        string jsonMessageBuffer = JsonSerializer.Serialize(_messages.ToArray());
                        response = new Response(jsonMessageBuffer);
                        await networkAdaptor.SendResponseAsync(response);
                        break;
                    
                    case QueryHeader.PostMessage:
                        response = await PostMessageAsync(query.JsonDataString);
                        await networkAdaptor.SendResponseAsync(response);
                        break;
                        
                    case QueryHeader.Quit:
                        quitCommandReceived = true;
                        break;

                    default:
                        Logger.LogWarning(string.Format(Messages.UnknownCommand, nameof(query.Header)));
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(string.Format(Messages.ExceptionOccured, ex.Message, ex.StackTrace));
        }
        finally
        {
            tcpClient.Close();
        }
    }
    
    private async void HandleClientServiceAsync(UdpReceiveResult udpReceiveResult)
    {
        UdpClient udpSender = new UdpClient();
        
        try
        {
            Query query = Query.FromRawLine(Encoding.UTF8.GetString(udpReceiveResult.Buffer));

            if (query.Header == QueryHeader.UpdateChat)
            {
                string jsonMessageBuffer = JsonSerializer.Serialize(_messages.ToArray());
                Response response = new Response(jsonMessageBuffer);
                
                byte[] binaryResponse = Encoding.UTF8.GetBytes(response.ToString());
                await udpSender.SendAsync(binaryResponse, udpReceiveResult.RemoteEndPoint);
            }
            else
            {
                Logger.LogWarning(string.Format(Messages.UnknownCommand, nameof(query.Header)));
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(string.Format(Messages.ExceptionOccured, ex.Message, ex.StackTrace));
        }
        finally
        {
            udpSender.Close();
        }
    }

    private async Task<Response> SignInAsync(string jsonDataString)
    {
        User user = JsonSerializer.Deserialize<User>(jsonDataString);
        bool success = await _databaseContext.IsUserExistsAsync(user);

        string jsonMessageBuffer = JsonSerializer.Serialize(success);
        return new Response(jsonMessageBuffer);
    }
    
    private async Task<Response> SignUpAsync(string jsonDataString)
    {
        User user = JsonSerializer.Deserialize<User>(jsonDataString);
        bool success = await _databaseContext.CreateUserAsync(user);

        string jsonMessageBuffer = JsonSerializer.Serialize(success);
        return new Response(jsonMessageBuffer);
    }
    
    private async Task<Response> PostMessageAsync(string jsonDataString)
    {
        Message message = JsonSerializer.Deserialize<Message>(jsonDataString);
        bool success = await _databaseContext.PostMessageAsync(message);
                        
        if (success)
        {
            _messages.Clear();
            _messages = new ConcurrentQueue<Message>(await _databaseContext.GetAllMessagesAsync());
        }

        string jsonMessageBuffer = JsonSerializer.Serialize(success);
        return new Response(jsonMessageBuffer);
    }

    public async ValueTask DisposeAsync()
    {
        IsRunning = false;
        _tcpListener?.Stop();
        _udpReceiver?.Close();
        
        if (_databaseContext != null)
        {
            await _databaseContext.DisposeAsync();
        }
    }
}