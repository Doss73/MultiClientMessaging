using System;
using System.Collections.Generic;
using System.Text;

namespace MultiClientMessaging.Client.Messenger
{
    public class AlreadyRegisteredException: Exception
    {
        public AlreadyRegisteredException():
            base("Client with such id already connected to server")
        {
        }
    }
}
