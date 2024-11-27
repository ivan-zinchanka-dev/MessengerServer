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
            Logger.LogInformation($"The server is running in a thread {Thread.CurrentThread.ManagedThreadId}.");
            
            Task clientsLoop = Task.Run(StartClientsLoop);
            Task clientServicesLoop = Task.Run(StartClientServicesLoop);
            
            await Task.WhenAll(clientsLoop, clientServicesLoop);

        }
        catch (Exception ex)
        {
            Logger.LogCritical($"{ex.Message}\n{ex.StackTrace}");
        }
        finally
        {
            await DisposeAsync();
        }
    }
    
    private async Task StartClientsLoop()
    {
        _tcpListener = new TcpListener(IPAddress.Any, _options.ClientPort);
        _tcpListener.Start();
    
        Logger.LogInformation($"The client loop is running in a thread {Thread.CurrentThread.ManagedThreadId}.");
        
        while (IsRunning)
        {
            TcpClient tcpClient = await _tcpListener.AcceptTcpClientAsync();
            Task.Run(() => HandleClientAsync(tcpClient));
        }
    }

    private async Task StartClientServicesLoop()
    {
        _udpReceiver = new UdpClient(_options.ClientServicePort);

        Logger.LogInformation($"The client services loop is running in a thread {Thread.CurrentThread.ManagedThreadId}");
        
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
                        response = await SignIn(query.JsonDataString);
                        await networkAdaptor.SendResponseAsync(response);
                        break;
                    
                    case QueryHeader.SignUp:
                        response = await SignUp(query.JsonDataString);
                        await networkAdaptor.SendResponseAsync(response);
                        break;

                    case QueryHeader.UpdateChat:
                        string jsonMessageBuffer = JsonSerializer.Serialize(_messages.ToArray());
                        response = new Response(jsonMessageBuffer);
                        await networkAdaptor.SendResponseAsync(response);
                        break;
                    
                    case QueryHeader.PostMessage:
                        response = await PostMessage(query.JsonDataString);
                        await networkAdaptor.SendResponseAsync(response);
                        break;
                        
                    case QueryHeader.Quit:
                        quitCommandReceived = true;
                        break;

                    default:
                        Logger.LogWarning($"Unknown command received: {(int)query.Header}.");
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogError($"{ex.Message}\n{ex.StackTrace}");
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
                Logger.LogWarning($"Unknown command received: {(int)query.Header}.");
            }
        }
        catch (Exception ex)
        {
            Logger.LogError($"{ex.Message}\n{ex.StackTrace}");
        }
        finally
        {
            udpSender.Close();
        }
    }

    private async Task<Response> SignIn(string jsonDataString)
    {
        User user = JsonSerializer.Deserialize<User>(jsonDataString);
        bool success = await _databaseContext.IsUserExistsAsync(user);

        string jsonMessageBuffer = JsonSerializer.Serialize(success);
        return new Response(jsonMessageBuffer);
    }
    
    private async Task<Response> SignUp(string jsonDataString)
    {
        User user = JsonSerializer.Deserialize<User>(jsonDataString);
        bool success = await _databaseContext.CreateUserAsync(user);

        string jsonMessageBuffer = JsonSerializer.Serialize(success);
        return new Response(jsonMessageBuffer);
    }
    
    private async Task<Response> PostMessage(string jsonDataString)
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