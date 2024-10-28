namespace MessengerServer.Server.Network;

public class AppServerOptions
{
    public int ClientPort { get; private set; }
    public int ClientServicePort { get; private set; }

    public AppServerOptions(int clientPort, int clientServicePort)
    {
        ClientPort = clientPort;
        ClientServicePort = clientServicePort;
    }
}