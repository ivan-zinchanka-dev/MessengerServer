﻿using MessengerCoreLibrary.Services;
using MessengerCoreLibrary.Services.FileLogging;
using MessengerServer.Server.Database;
using MessengerServer.Server.Network;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MessengerServer;

public static class Program
{
    private const string AppConfigFileName = "app_config.ini";
    private const string ShutdownCommand = "shutdown";
    
    private static AppServer _appServer;
    private static AppServerOptions _serverOptions;
    private static IniService _iniService;
    
    public static async Task Main(string[] args)
    {
        string appConfigPath = Path.Combine(Directory.GetCurrentDirectory(), AppConfigFileName);
        _iniService = new IniService(appConfigPath);

        InitAppServerOptions();
        
        IHost host = Host.CreateDefaultBuilder()
            .ConfigureLogging(loggingBuilder =>
            {
                loggingBuilder
                    .AddConsole()
                    .AddFile(GetLogsFileName())
                    .SetMinimumLevel(LogLevel.Information);
            })
            .ConfigureServices(services =>
            {
                services
                    .AddSingleton<AppServerOptions>(_serverOptions)
                    .AddSingleton<AppServer>()
                    .AddTransient<DatabaseContext>();
            })
            .Build();

        _appServer = host.Services.GetRequiredService<AppServer>();
        
        try
        {
            _appServer.StartAsync();
        
            Console.WriteLine($"Your control loop is running in a thread {Thread.CurrentThread.ManagedThreadId}.");
            
            while (true)
            {
                Console.WriteLine($"Input \"{ShutdownCommand}\" to shutdown the server.");
            
                string command = Console.ReadLine();

                if (command == ShutdownCommand)
                {
                    break;
                }
            }
        
            await _appServer.DisposeAsync();

        }
        catch (Exception ex)
        {
            _appServer.Logger.LogCritical(ex, "An unhandled exception occurred.");
        }
    }
    
    private static string GetLogsFileName()
    {
        return _iniService.GetString("Logging", "LogsFileName");
    }
    
    private static void InitAppServerOptions()
    {
        const string serverSection = "Server";
        
        string clientPortString = _iniService.GetString(serverSection, nameof(_serverOptions.ClientPort));
        string servicePortString = _iniService.GetString(serverSection, nameof(_serverOptions.ClientServicePort));

        _serverOptions = new AppServerOptions(Convert.ToInt32(clientPortString), Convert.ToInt32(servicePortString));
    }
    
}