using System.Net.Sockets;
using System.Text.Json;
using MessengerCoreLibrary.Infrastructure;
using MessengerCoreLibrary.Models;

namespace Tester;

public class Client
{
    public async Task Run()
    {
        try
        {
            using TcpClient tcpClient = new TcpClient();
            await tcpClient.ConnectAsync("127.0.0.1", 8888);
            NetworkStream networkStream = tcpClient.GetStream();
            Console.WriteLine("Client has connected to server");
            
            StreamWriter writer = new StreamWriter(networkStream);
            string rawLine = JsonSerializer.Serialize(CreateTestMessage());
            Console.WriteLine("Raw test message string: " + rawLine);

            Query query = new Query(QueryHeader.PostMessage, rawLine);
            await writer.WriteAsync(query.ToString());
            await writer.FlushAsync();
            Console.WriteLine("The message was sent");
        
            StreamReader reader = new StreamReader(networkStream);
            rawLine = await reader.ReadLineAsync();
            Console.WriteLine("Raw messages list string: " + rawLine);
            
            Response response = Response.FromRawLine(rawLine);
            List<Message> chat = JsonSerializer.Deserialize<List<Message>>(response.JsonDataString);
            
            Console.WriteLine("Received messages: ");
            foreach (Message message in chat)
            {
                Console.WriteLine(message);
            }
            
            tcpClient.Close();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }
    
    private static Message CreateTestMessage()
    {
        return new Message("Nick", "Mike", "Just a test message.", DateTime.UtcNow);
    }
}