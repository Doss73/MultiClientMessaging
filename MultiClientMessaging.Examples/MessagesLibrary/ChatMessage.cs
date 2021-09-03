using Meta.Lib.Modules.PubSub;

namespace MessagesLibrary
{
    public class ChatMessage : PubSubMessageBase
    {
        public ChatMessage(string text)
        {
            Text = text;
        }

        public string Text { get; }
    }
}
