namespace MultiClientMessaging.Client.Messenger
{
    public enum WebMessageType : byte
    {
        Message = (byte)'m',
        MessageResponse = (byte)'r'
    }
}
