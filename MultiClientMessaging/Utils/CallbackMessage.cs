using System;
using System.IO;

namespace MultiClientMessaging.Client.Utils
{
    public class CallbackMessage
    {
        readonly string[] _parts;

        public bool IsMessageResponse { get; }
        public string PacketId { get; }

        //Properties for non-response
        public Type MessageType { get; }
        public string SerializedMessage { get; }

        //Properties for message response
        public bool IsOk { get; }
        public string SerializedException { get; }

        public CallbackMessage(string packet)
        {
            _parts = packet.Split('\t');

            if (_parts.Length < 3 || _parts.Length > 4)
                throw new InvalidDataException("Messsage has wrong format");

            if (_parts[0] == "m")
                IsMessageResponse = false;
            else if (_parts[0] == "r")
                IsMessageResponse = true;
            else
                throw new InvalidDataException("Messsage has wrong format");

            PacketId = _parts[1];

            if (!IsMessageResponse)
            {
                MessageType = Type.GetType(_parts[2]);
                SerializedMessage = _parts[3];
            }
            else
            {
                IsOk = _parts[2] == "ok";
                if (!IsOk)
                    SerializedException = _parts[3];
            }
        }
    }
}
