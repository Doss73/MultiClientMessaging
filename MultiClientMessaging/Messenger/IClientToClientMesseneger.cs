using Meta.Lib.Modules.PubSub;
using System;
using System.Threading.Tasks;

namespace MultiClientMessaging.Client.Messenger
{
    public interface IClientToClientMesseneger
    {
        event EventHandler Connected;
        event EventHandler<Exception> Disconnected;

        bool IsConnected { get; }

        Task ConnectToServer(string address, string clientId);
        Task DisconnectFromServer();
        void Subscribe<TMessage>(Func<TMessage, Task> handler, Predicate<TMessage> predicate = null) where TMessage : class, IPubSubMessage;
        void Unsubscribe<TMessage>(Func<TMessage, Task> handler) where TMessage : class, IPubSubMessage;
        Task<bool> SendToClient(string receiverId, IPubSubMessage message);
    }
}
