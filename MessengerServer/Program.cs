using MessengerServer.Server;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MessengerServer;

public static class Program
{
    private const string ShutdownCommand = "shutdown";
    private static AppServer _appServer;
    
    public static async Task Main(string[] args)
    {
        IHost host = Host.CreateDefaultBuilder()
            .ConfigureLogging(loggingBuilder =>
            {
                loggingBuilder
                    .AddConsole()
                    .SetMinimumLevel(LogLevel.Information);
            })
            .ConfigureServices(services =>
            {
                services
                    .AddSingleton<AppServer>()
                    .AddTransient<DatabaseContext>();
            })
            .Build();
        
        _appServer = host.Services.GetRequiredService<AppServer>();
        _appServer.StartAsync();
        
        _appServer.Logger.LogInformation($"Your control loop started on thread {Thread.CurrentThread.ManagedThreadId}");
        
        while (true)
        {
            _appServer.Logger.LogInformation($"Input \"{ShutdownCommand}\" to shutdown the server");
            
            string command = Console.ReadLine();

            if (command == ShutdownCommand)
            {
                break;
            }
        }
        
        await _appServer.DisposeAsync();
    }
    
}