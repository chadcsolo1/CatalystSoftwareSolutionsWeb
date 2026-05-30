namespace WebAppTemplate1.Services.Email
{
    /// <summary>
    /// Represents the result of an email send operation.
    /// Encapsulates success/failure state and error information.
    /// </summary>
    public class EmailResult
    {
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }
        public string? MessageId { get; set; }
        public string? ProviderUsed { get; set; }

        public static EmailResult Success(string? messageId = null, string? provider = null)
        {
            return new EmailResult
            {
                IsSuccess = true,
                MessageId = messageId,
                ProviderUsed = provider
            };
        }

        public static EmailResult Failure(string errorMessage, string? provider = null)
        {
            return new EmailResult
            {
                IsSuccess = false,
                ErrorMessage = errorMessage,
                ProviderUsed = provider
            };
        }
    }
}
