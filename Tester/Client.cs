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
        
            Console.WriteLine("Connected");

            
            StreamWriter writer = new StreamWriter(networkStream);
            
            string rawLine = JsonSerializer.Serialize(CreateTestMessage());
            
            Console.WriteLine("String: " + rawLine);

            Query query = new Query(QueryHeader.PostMessage, rawLine);
            
            await writer.WriteAsync(query.ToString());
            await writer.FlushAsync();
            
            
            Console.WriteLine("Wrote");
        
            StreamReader reader = new StreamReader(networkStream);
            Console.WriteLine("GetStream");
            
            rawLine = await reader.ReadLineAsync();
            Console.WriteLine("Read");
            
            Response response = Response.FromRawLine(rawLine);
            List<Message> chat = JsonSerializer.Deserialize<List<Message>>(response.JsonDataString);

            Console.WriteLine("Read");
        
            foreach (Message message in chat)
            {
                Console.WriteLine(message);
            }
            
            //tcpClient.Close();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }
    
    private Message CreateTestMessage()
    {
        return new Message()
        {
            SenderNickname = "Nick",
            ReceiverNickname = "Mike",
            Text = "Just a test message.",
            PostDateTime = DateTime.UtcNow,
        };
        
    }
}