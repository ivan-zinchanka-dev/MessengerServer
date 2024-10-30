namespace Tester;

public static class Program
{
    private static Client _client;
    
    public static async Task Main(string[] args)
    {
        _client = new Client();
        await _client.Run();
    }
}