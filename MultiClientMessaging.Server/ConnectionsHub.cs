using Microsoft.AspNetCore.SignalR;
using MultiClientMessaging.Common;
using System;
using System.Threading.Tasks;

namespace MultiClientMessaging.Server
{
    public class ConnectionsHub : Hub<IClientConnection>
    {
        readonly IConnectionsService _connectionsService;

        public ConnectionsHub(IConnectionsService connectionsService)
        {
            _connectionsService = connectionsService;
        }

        public override Task OnConnectedAsync()
        {
            try
            {
                var id = GetClientId();

                _connectionsService.OnClientConnected(id, Clients.Caller);
            }
            catch
            {
                Context.Abort();
            }
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception exception)
        {
            var id = GetClientId();

            _connectionsService.OnClientDisconnected(id);
            return base.OnDisconnectedAsync(exception);
        }

        public async Task<HubResponse> SendMessage(string receiverId, string message)
        {
            var receiver = _connectionsService.FindConnection(receiverId);

            if (receiver != null)
            {
                try
                {
                    await receiver.SendMessageCallback(message, GetClientId());
                    return HubResponse.Ok;
                }
                catch (Exception ex)
                {
                    return new HubResponse(ex.Message);
                }
            }
            else
                return new HubResponse("Connection with such id is unavailable");
        }

        string GetClientId()
        {
            var httpContext = Context.GetHttpContext();
            string clientId = httpContext.Request.Headers["ClientId"].ToString();

            if (string.IsNullOrWhiteSpace(clientId))
                throw new Exception($"ConnectionsHub.OnConnectedAsync - httpContext.Request.Headers does not contain ClientId");

            return clientId;
        }
    }
}
