namespace MultiClientMessaging.Server
{
    public interface IConnectionsService
    {
        void OnClientConnected(string id, IClientConnection connection);
        void OnClientDisconnected(string id);
        IClientConnection FindConnection(string id);
    }
}
