using System.Net.Sockets;
using MessengerCoreLibrary.Infrastructure;

namespace MessengerServer.Server.Network;

public class NetworkAdaptor
{
    private readonly StreamReader _reader;
    private readonly StreamWriter _writer;

    public NetworkAdaptor(NetworkStream stream)
    {
        _reader = new StreamReader(stream);
        _writer = new StreamWriter(stream);
    }
    
    public async Task SendResponseAsync(Response response)
    {
        await _writer.WriteAsync(response.ToString());
        await _writer.FlushAsync();
    }
    
    public async Task<Query> ReceiveQueryAsync()
    {
        string rawLine = await _reader.ReadLineAsync();
        return Query.FromRawLine(rawLine);
    }
}