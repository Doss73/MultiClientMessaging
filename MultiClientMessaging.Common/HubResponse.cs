namespace MultiClientMessaging.Common
{
    public class HubResponse
    {
        public static HubResponse Ok = new HubResponse();

        public bool IsSuccessful { get; set; }

        public string ExceptionMessage { get; set; }

        public HubResponse(string exceptionMessage = null)
        {
            if (string.IsNullOrEmpty(exceptionMessage))
                IsSuccessful = true;
            else
            {
                IsSuccessful = false;
                ExceptionMessage = exceptionMessage;
            }
        }
    }
}
