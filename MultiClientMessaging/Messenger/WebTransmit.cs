using Meta.Lib.Modules.PubSub;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;

namespace MultiClientMessaging.Client.Messenger
{
    public class WebTransmit
    {
        public string Id { get; } = Guid.NewGuid().ToString();

        public string Packet { get; }
        public int Timeout { get; }

        public TaskCompletionSource<bool> Tcs { get; } = new TaskCompletionSource<bool>();

        public WebTransmit(IPubSubMessage message, WebMessageType webMessageType)
        {
            Timeout = message.ResponseTimeout;
            var serializedMessage = JsonConvert.SerializeObject(message);
            Packet = $"{(char)webMessageType}\t{Id}\t{message.GetType().AssemblyQualifiedName}\t{serializedMessage}";
        }

        public WebTransmit(string message, WebMessageType webMessageType, int millisecondsTimeout)
        {
            Timeout = millisecondsTimeout;
            Packet = $"{(char)webMessageType}\t{Id}\t{message}";
        }
    }
}
