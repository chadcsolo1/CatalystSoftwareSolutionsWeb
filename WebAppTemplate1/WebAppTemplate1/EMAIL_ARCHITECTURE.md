# Email Service Architecture

## Overview

The email service has been redesigned using **SOLID principles** and **design patterns** to provide a flexible, scalable, and maintainable solution for sending emails through multiple providers.

## Architecture Components

### 1. Core Abstractions

#### `IEmailProvider`
- **Purpose**: Defines the contract for all email provider implementations
- **Pattern**: Strategy Pattern
- **Location**: `Services/Email/IEmailProvider.cs`

#### `IEmailProviderFactory`
- **Purpose**: Creates instances of email providers
- **Pattern**: Factory Pattern
- **Location**: `Services/Email/EmailProviderFactory.cs`

#### `IEmailService`
- **Purpose**: Application-level email service interface
- **Pattern**: Facade Pattern
- **Location**: `Services/IEmailService.cs`

### 2. Data Models

#### `EmailMessage`
- Encapsulates email data (to, subject, body, attachments)
- Provides validation logic
- **Location**: `Services/Email/EmailMessage.cs`

#### `EmailResult`
- Encapsulates the result of email send operations
- Contains success/failure state and metadata
- **Location**: `Services/Email/EmailResult.cs`

#### `EmailAttachment`
- Represents file attachments
- **Location**: `Services/Email/EmailMessage.cs`

### 3. Email Provider Implementations

#### **MicrosoftGraphEmailProvider**
- Uses Microsoft 365 mailbox via Graph API
- **Best for**: Internal communications, corporate emails
- **Limitations**: Subject to Microsoft 365 IP reputation/restrictions
- **Location**: `Services/Email/Providers/MicrosoftGraphEmailProvider.cs`

#### **AzureCommunicationServicesEmailProvider**
- Uses Azure Communication Services Email API
- **Best for**: Transactional emails, external recipients, high volume
- **Advantages**: Better deliverability, no IP reputation issues
- **Location**: `Services/Email/Providers/AzureCommunicationServicesEmailProvider.cs`

#### **SendGridEmailProvider**
- Placeholder for SendGrid implementation
- **Best for**: Third-party email service alternative
- **Location**: `Services/Email/Providers/SendGridEmailProvider.cs`

### 4. Configuration

All email configuration is centralized in `appsettings.json`:

```json
{
  "EmailProvider": {
    "Provider": "MicrosoftGraph",           // Primary provider
    "FallbackProvider": "AzureCommunicationServices",  // Optional fallback
    "EnableFallback": false,                 // Enable automatic fallback
    "FromAddress": "your-email@domain.com",
    "FromName": "Your Company Name"
  },
  "MicrosoftGraph": {
    "TenantId": "your-tenant-id",
    "ClientId": "your-client-id",
    "ClientSecret": "your-client-secret"
  },
  "AzureCommunicationServices": {
    "ConnectionString": "your-acs-connection-string"
  },
  "SendGrid": {
    "ApiKey": "your-sendgrid-api-key"
  }
}
```

## SOLID Principles Applied

### Single Responsibility Principle (SRP)
- Each provider handles only its specific email sending logic
- `EmailMessage` only manages email data
- `EmailResult` only manages operation results

### Open/Closed Principle (OCP)
- Add new providers without modifying existing code
- Factory pattern allows extension through new implementations

### Liskov Substitution Principle (LSP)
- All providers implement `IEmailProvider` and are interchangeable
- Any provider can be swapped without breaking the application

### Interface Segregation Principle (ISP)
- `IEmailProvider` is focused and minimal
- No client is forced to depend on methods it doesn't use

### Dependency Inversion Principle (DIP)
- High-level `EmailService` depends on `IEmailProvider` abstraction
- Concrete providers are injected via DI

## Design Patterns

### Strategy Pattern
- Different email sending strategies (Graph, ACS, SendGrid)
- Selected at runtime via configuration

### Factory Pattern
- `EmailProviderFactory` creates the appropriate provider
- Encapsulates provider instantiation logic

### Facade Pattern
- `EmailService` provides a simple interface to complex subsystem
- Hides provider complexity from consumers

## Usage Examples

### Basic Email Sending

```csharp
public class MyController : ControllerBase
{
    private readonly IEmailService _emailService;

    public MyController(IEmailService emailService)
    {
        _emailService = emailService;
    }

    public async Task<IActionResult> SendEmail()
    {
        var message = new EmailMessage
        {
            ToEmail = "recipient@example.com",
            ToName = "John Doe",
            Subject = "Welcome!",
            HtmlBody = "<h1>Welcome to our service!</h1>"
        };

        var result = await _emailService.SendEmailAsync(message);

        if (result.IsSuccess)
        {
            return Ok($"Email sent via {result.ProviderUsed}");
        }

        return BadRequest(result.ErrorMessage);
    }
}
```

### Domain-Specific Email (Consultation Booking)

```csharp
var booking = new ConsultationBooking
{
    FullName = "Jane Smith",
    Email = "jane@example.com",
    ConsultationDate = DateTime.Now.AddDays(7),
    ConsultationTime = "2:00 PM"
};

await _emailService.SendConsultationConfirmationAsync(booking);
```

## Switching Email Providers

### Option 1: Configuration Change (No Code Changes)

Edit `appsettings.json`:
```json
"EmailProvider": {
  "Provider": "AzureCommunicationServices"  // Changed from MicrosoftGraph
}
```

### Option 2: Enable Fallback

```json
"EmailProvider": {
  "Provider": "MicrosoftGraph",
  "FallbackProvider": "AzureCommunicationServices",
  "EnableFallback": true  // Auto-fallback on failure
}
```

### Option 3: Runtime Selection

```csharp
var provider = emailProviderFactory.GetProvider("AzureCommunicationServices");
var result = await provider.SendEmailAsync(message);
```

## Adding a New Provider

### Step 1: Create Provider Class

```csharp
public class MyCustomEmailProvider : IEmailProvider
{
    public string ProviderName => "MyCustomProvider";

    public async Task<EmailResult> SendEmailAsync(EmailMessage message)
    {
        // Your implementation
        return EmailResult.Success(provider: ProviderName);
    }
}
```

### Step 2: Register in Factory

Update `EmailProviderFactory.cs`:
```csharp
return selectedProvider.ToLowerInvariant() switch
{
    "microsoftgraph" => ...,
    "azurecommunicationservices" => ...,
    "mycustomprovider" => _serviceProvider.GetRequiredService<MyCustomEmailProvider>(),
    _ => throw new InvalidOperationException(...)
};
```

### Step 3: Register in DI

Update `EmailServiceExtensions.cs`:
```csharp
services.AddScoped<MyCustomEmailProvider>();
```

### Step 4: Add Configuration

Add to `appsettings.json`:
```json
"MyCustomProvider": {
  "ApiKey": "..."
}
```

## Current Setup Status

✅ **Architecture Implemented**
- All provider abstractions created
- Microsoft Graph provider fully implemented
- Azure Communication Services provider fully implemented
- SendGrid provider placeholder created

✅ **Configuration Ready**
- `appsettings.json` structured for all providers
- Microsoft Graph credentials configured
- Ready for ACS connection string

⏳ **Next Steps**
- Set up Azure Communication Services resource (optional)
- Add ACS connection string to configuration
- Test with external email recipients
- Optionally implement SendGrid provider

## Provider Comparison

| Feature | Microsoft Graph | Azure Communication Services | SendGrid |
|---------|----------------|------------------------------|----------|
| **Setup** | Azure AD app registration | ACS resource + connection string | SendGrid account + API key |
| **Best For** | Internal/corporate emails | Transactional/application emails | Third-party service |
| **Deliverability** | Subject to tenant restrictions | Excellent, purpose-built | Excellent |
| **Cost** | Included with M365 | Pay per email (free tier available) | Freemium model |
| **External Sending** | May have restrictions | Optimized for external | No restrictions |
| **Current Status** | ✅ Configured | ⚙️ Ready for setup | 📋 Placeholder |

## Testing

### Test with Current Setup (Microsoft Graph)

```json
"EmailProvider": {
  "Provider": "MicrosoftGraph"
}
```

Send test email to your Microsoft 365 mailbox (internal delivery works).

### Test with Azure Communication Services

1. Create ACS resource in Azure Portal
2. Get connection string
3. Update `appsettings.json`:
```json
"EmailProvider": {
  "Provider": "AzureCommunicationServices"
},
"AzureCommunicationServices": {
  "ConnectionString": "endpoint=https://...;accesskey=..."
}
```

## Security Best Practices

### Development
Use **user secrets** for sensitive configuration:
```bash
cd WebAppTemplate1\WebAppTemplate1
dotnet user-secrets set "MicrosoftGraph:ClientSecret" "your-secret"
dotnet user-secrets set "AzureCommunicationServices:ConnectionString" "your-connection-string"
```

### Production
- Use **Azure Key Vault** for secrets
- Use **Managed Identity** where possible
- Never commit secrets to source control

## Troubleshooting

### Email not sending?
1. Check logs for detailed error messages
2. Verify provider credentials in configuration
3. Ensure correct provider is selected
4. Try fallback provider if enabled

### External emails failing?
- Microsoft Graph: Check tenant sending restrictions
- Switch to Azure Communication Services for better external deliverability

### Multiple providers failing?
- Enable fallback in configuration
- Check network connectivity to provider endpoints
- Verify all required packages are installed

## Dependencies

- `Azure.Identity` (1.21.0) - For Microsoft Graph authentication
- `Microsoft.Graph` (6.1.0) - Microsoft Graph API client
- `Azure.Communication.Email` (1.1.0) - Azure Communication Services
- Future: `SendGrid` (when implemented)

## Summary

This architecture provides:
- ✅ **Flexibility**: Easy to switch providers
- ✅ **Scalability**: Add new providers without breaking changes
- ✅ **Maintainability**: Clean separation of concerns
- ✅ **Testability**: All components mockable via interfaces
- ✅ **Production-Ready**: Fallback support, error handling, logging
