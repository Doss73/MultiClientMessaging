using System;
using System.Collections.Concurrent;

namespace MultiClientMessaging.Server
{
    public class ConnectionsService : IConnectionsService
    {
        static readonly ConcurrentDictionary<string, IClientConnection> _connections
                    = new ConcurrentDictionary<string, IClientConnection>();

        public IClientConnection FindConnection(string id)
        {
            return FindClientConnection(id);
        }

        public void OnClientConnected(string id, IClientConnection connection)
        {
            if(!_connections.TryAdd(id, connection))
            {
                throw new Exception("Client with such id already exists");
            }
        }

        public void OnClientDisconnected(string id)
        {
            _connections.TryRemove(id, out IClientConnection _);
        }

        static IClientConnection FindClientConnection(string clientId)
        {
            _connections.TryGetValue(clientId, out IClientConnection connection);
            return connection;
        }
    }
}
