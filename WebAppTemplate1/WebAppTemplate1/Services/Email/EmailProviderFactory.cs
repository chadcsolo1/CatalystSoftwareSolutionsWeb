using Microsoft.Extensions.Options;

namespace WebAppTemplate1.Services.Email
{
    /// <summary>
    /// Factory for creating email provider instances.
    /// Implements Factory Pattern and adheres to Open/Closed Principle.
    /// </summary>
    public interface IEmailProviderFactory
    {
        IEmailProvider GetProvider(string? providerName = null);
    }

    public class EmailProviderFactory : IEmailProviderFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly EmailProviderOptions _options;
        private readonly ILogger<EmailProviderFactory> _logger;

        public EmailProviderFactory(
            IServiceProvider serviceProvider,
            IOptions<EmailProviderOptions> options,
            ILogger<EmailProviderFactory> logger)
        {
            _serviceProvider = serviceProvider;
            _options = options.Value;
            _logger = logger;
        }

        public IEmailProvider GetProvider(string? providerName = null)
        {
            var selectedProvider = providerName ?? _options.Provider;

            _logger.LogDebug("Creating email provider: {Provider}", selectedProvider);

            return selectedProvider.ToLowerInvariant() switch
            {
                "microsoftgraph" or "graph" => 
                    _serviceProvider.GetRequiredService<Providers.MicrosoftGraphEmailProvider>(),

                "azurecommunicationservices" or "acs" => 
                    _serviceProvider.GetRequiredService<Providers.AzureCommunicationServicesEmailProvider>(),

                "sendgrid" => 
                    _serviceProvider.GetRequiredService<Providers.SendGridEmailProvider>(),

                _ => throw new InvalidOperationException(
                    $"Unknown email provider: {selectedProvider}. " +
                    $"Valid options are: MicrosoftGraph, AzureCommunicationServices, SendGrid")
            };
        }
    }
}
