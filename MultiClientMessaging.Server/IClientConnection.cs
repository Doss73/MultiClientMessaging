using System.Threading.Tasks;

namespace MultiClientMessaging.Server
{
    public interface IClientConnection
    {
        Task SendMessageCallback(string message, string publisherId);
    }
}
