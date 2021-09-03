using Newtonsoft.Json;
using System;

namespace MultiClientMessaging.Client.Utils
{
    public static class ClientResponseUtils
    {
        public static string GetOkResponse(string packetId)
        {
            return $"r\t{packetId}\tok";
        }

        public static string GetErrorResponse(string packetId, Exception exception)
        {
            if (exception == null)
                throw new ArgumentNullException("Exception can't be null");

            if (!(exception is AggregateException))
                exception = new AggregateException(exception);

            var settings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All };
            var exceptionStr = JsonConvert.SerializeObject(exception, exception.GetType(), settings);
            return $"r\t{packetId}\terror\t{exceptionStr}";
        }
    }
}
