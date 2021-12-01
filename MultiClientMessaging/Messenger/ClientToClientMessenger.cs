using Meta.Lib.Modules.Logger;
using Meta.Lib.Modules.PubSub;
using System;
using System.Threading.Tasks;

namespace MultiClientMessaging.Client.Messenger
{
    //TODO: Test issue
    public class ClientToClientMessenger: IClientToClientMesseneger
    {
        readonly SignalRConnection _signalRConnection;
        readonly MetaPubSub _hub;
        readonly IMetaLogger _logger;

        public event EventHandler Connected;
        public event EventHandler<Exception> Disconnected;

        public bool IsConnected => _signalRConnection.IsConnected;

        public ClientToClientMessenger(IMetaLogger logger = null)
        {
            _logger = logger ?? MetaLogger.Default;

            _hub = new MetaPubSub(_logger);
            _signalRConnection = new SignalRConnection(_hub, _logger);
            _signalRConnection.Connected += (o, e) => Connected?.Invoke(this, e);
            _signalRConnection.Disconnected += (o, e) => Disconnected?.Invoke(this, e);
        }

        public Task ConnectToServer(string address, string clientId)
        {
            return _signalRConnection.Connect(address, clientId);
        }

        public Task DisconnectFromServer()
        {
            return _signalRConnection.Stop();
        }

        public void Subscribe<TMessage>(Func<TMessage, Task> handler, Predicate<TMessage> predicate = null)
            where TMessage : class, IPubSubMessage
        {
            _hub.Subscribe(handler, predicate);
        }

        public void Unsubscribe<TMessage>(Func<TMessage, Task> handler)
            where TMessage : class, IPubSubMessage
        {
            _hub.Unsubscribe(handler);
        }

        public Task<bool> SendToClient(string receiverId, IPubSubMessage message)
        {
            return _signalRConnection.SendMessage(receiverId, message, WebMessageType.Message);
        }
    }
}
