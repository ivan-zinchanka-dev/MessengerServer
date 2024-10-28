using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using MessengerServer.Core.Infrastructure;
using MessengerServer.Core.Models;

namespace MessengerServer.Server;

public class AppServer : IAsyncDisposable
{
    private TcpListener _tcpListener;
    private UdpClient _udpReceiver;
    private DatabaseContext _databaseContext;
    
    private readonly object _stateLocker = new object();
    private bool _isRunning;
    
    private ConcurrentQueue<Message> _messages;
    
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

    public AppServer(DatabaseContext databaseContext)
    {
        _databaseContext = databaseContext;
    }
    
    public async void StartAsync()
    {
        try
        {
            await _databaseContext.ConnectToDatabaseAsync();
            _messages = new ConcurrentQueue<Message>(await _databaseContext.GetAllMessagesAsync());

            IsRunning = true;
            Console.WriteLine("Server is started on thread " + Thread.CurrentThread.ManagedThreadId);
            
            Task clientsLoop = Task.Run(StartClientsLoop);
            Task clientServicesLoop = Task.Run(StartClientServicesLoop);
            
            await Task.WhenAll(clientsLoop, clientServicesLoop);

        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message + "\n" + ex.StackTrace);
        }
        finally
        {
            await DisposeAsync();
        }
    }

    private async Task StartClientsLoop()
    {
        _tcpListener = new TcpListener(IPAddress.Any, 8888);
        _tcpListener.Start();
    
        Console.WriteLine("Clients loop started on thread " + Thread.CurrentThread.ManagedThreadId);
        
        while (IsRunning)
        {
            TcpClient tcpClient = await _tcpListener.AcceptTcpClientAsync();
            Task.Run(() => HandleClientAsync(tcpClient));
        }
    }

    private async Task StartClientServicesLoop()
    {
        _udpReceiver = new UdpClient(5555);

        Console.WriteLine("Client services loop started on thread " + Thread.CurrentThread.ManagedThreadId);
        
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
                        Console.WriteLine("Unknown command");
                        break;
                }

            }
            
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message + "\n" + ex.StackTrace);
        }
        finally
        {
            tcpClient.Close();
        }
        
    }

    /*private async void HandleServicesAsync()
    {
        UdpClient udpClient = new UdpClient(5555);

        while (IsRunning)
        {
            UdpReceiveResult receiveResult = await udpClient.ReceiveAsync();
            Query query = Query.FromRawLine(Encoding.UTF8.GetString(receiveResult.Buffer));

            if (query.Header == QueryHeader.UpdateChat)
            {
                string jsonMessageBuffer = JsonSerializer.Serialize(_messages.ToArray());
                Response response = new Response(jsonMessageBuffer);
                
                byte[] binaryResponse = Encoding.UTF8.GetBytes(response.ToString());
                await udpClient.SendAsync(binaryResponse, receiveResult.RemoteEndPoint);
            }
            else
            {
                Console.WriteLine("Unknown command");
            }
        }
        
        udpClient.Close();
    }*/

    

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
                Console.WriteLine("Unknown command");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message + "\n" + ex.StackTrace);
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