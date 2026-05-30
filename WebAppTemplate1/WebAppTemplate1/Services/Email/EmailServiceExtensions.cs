using WebAppTemplate1.Services.Email.Providers;

namespace WebAppTemplate1.Services.Email
{
    /// <summary>
    /// Extension methods for configuring email services.
    /// Follows Dependency Inversion Principle - depends on abstractions.
    /// </summary>
    public static class EmailServiceExtensions
    {
        public static IServiceCollection AddEmailServices(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // Register configuration options
            services.Configure<EmailProviderOptions>(
                configuration.GetSection(EmailProviderOptions.SectionName));

            services.Configure<MicrosoftGraphOptions>(
                configuration.GetSection(MicrosoftGraphOptions.SectionName));

            services.Configure<AzureCommunicationServicesOptions>(
                configuration.GetSection(AzureCommunicationServicesOptions.SectionName));

            services.Configure<SendGridOptions>(
                configuration.GetSection(SendGridOptions.SectionName));

            // Register all email providers
            services.AddScoped<MicrosoftGraphEmailProvider>();
            services.AddScoped<AzureCommunicationServicesEmailProvider>();
            services.AddScoped<SendGridEmailProvider>();

            // Register factory and main service
            services.AddScoped<IEmailProviderFactory, EmailProviderFactory>();
            services.AddScoped<Services.IEmailService, EmailServiceImpl>();

            return services;
        }
    }
}
