# Email Configuration Setup (Microsoft Graph API)

This document explains how to configure email notifications for consultation bookings using Microsoft Graph API.

## Overview

When a user confirms a consultation appointment, the system will automatically send a confirmation email using Microsoft Graph API containing:
- Appointment date and time
- User's contact information
- Next steps and reminders
- Professional branded email template

## Prerequisites

- Microsoft 365 Business or Enterprise subscription
- Azure AD tenant access
- Admin permissions to register applications in Azure Portal

## Configuration Steps

### 1. Register Application in Azure Portal

1. Go to [Azure Portal](https://portal.azure.com)
2. Navigate to **Azure Active Directory** → **App registrations**
3. Click **New registration**
4. Fill in the details:
   - **Name**: `CatalystSoftwareSolutions-EmailService` (or your preferred name)
   - **Supported account types**: Select "Accounts in this organizational directory only"
   - **Redirect URI**: Leave blank for now
5. Click **Register**

### 2. Grant API Permissions

After registration:

1. Go to **API permissions** in the left menu
2. Click **Add a permission**
3. Select **Microsoft Graph**
4. Choose **Application permissions** (not Delegated)
5. Search for and select:
   - `Mail.Send` - Send mail as any user
6. Click **Add permissions**
7. **Important**: Click **Grant admin consent** for your tenant (requires admin)

### 3. Create Client Secret

1. Go to **Certificates & secrets** in the left menu
2. Click **New client secret**
3. Add a description: `EmailService Secret`
4. Set expiration (recommended: 24 months or less for security)
5. Click **Add**
6. **IMPORTANT**: Copy the **Value** immediately - you won't be able to see it again!

### 4. Collect Configuration Values

From the Azure Portal, collect these values:

1. **Tenant ID**: 
   - Found on the **Overview** page of your App registration
   - Or in **Azure Active Directory** → **Overview** → **Tenant ID**

2. **Client ID (Application ID)**:
   - Found on the **Overview** page of your App registration

3. **Client Secret**:
   - The value you copied in step 3

### 5. Update appsettings.json

Open `appsettings.json` and add your Azure AD configuration:

```json
{
  "AzureAd": {
    "TenantId": "your-tenant-id-from-step-4",
    "ClientId": "your-client-id-from-step-4",
    "ClientSecret": "your-client-secret-from-step-3"
  },
  "Email": {
    "FromAddress": "chad_solomon@catalystsoftwaresolutions.com",
    "FromName": "Catalyst Software Solutions"
  }
}
```

**Note**: The `FromAddress` must be a valid mailbox in your Microsoft 365 tenant.

### 6. Production Security Best Practices

**NEVER commit secrets to source control!**

#### For Development - Use User Secrets:
```bash
cd WebAppTemplate1\WebAppTemplate1
dotnet user-secrets set "AzureAd:TenantId" "your-tenant-id"
dotnet user-secrets set "AzureAd:ClientId" "your-client-id"
dotnet user-secrets set "AzureAd:ClientSecret" "your-client-secret"
```

#### For Production - Use Environment Variables:
```bash
AzureAd__TenantId=your-tenant-id
AzureAd__ClientId=your-client-id
AzureAd__ClientSecret=your-client-secret
```

#### For Azure - Use Azure Key Vault:
1. Store secrets in Azure Key Vault
2. Grant your App Service managed identity access to Key Vault
3. Reference secrets in configuration

### 7. Testing

To test the email functionality:

1. Ensure Azure AD app is configured correctly
2. Verify permissions are granted
3. Run the application
4. Navigate to `/consultation`
5. Complete the consultation booking wizard
6. Check the recipient's email inbox for the confirmation

## Email Template

The confirmation email includes:
- Professional branded header with gradient
- Complete appointment details
- What's next section with reminders
- Contact information footer
- Mobile-responsive design

## Troubleshooting

**Email not sending:**
- Verify Azure AD app has `Mail.Send` application permission
- Confirm admin consent was granted for the permission
- Check that Client Secret hasn't expired
- Verify the `FromAddress` is a valid mailbox in your tenant
- Check application logs for detailed error messages
- Ensure Tenant ID, Client ID, and Client Secret are correct

**Permission errors:**
- Make sure you selected **Application permissions**, not Delegated permissions
- Ensure admin consent was granted (check in Azure Portal → App registrations → API permissions)
- The user account specified in `FromAddress` must exist in your Microsoft 365 tenant

**Authentication errors:**
- Verify Client Secret is correct and hasn't expired
- Check Tenant ID and Client ID are accurate
- Ensure the Azure AD app is enabled (not disabled)

## Architecture

The email functionality consists of:

1. **EmailService** (`Services/EmailService.cs`): Uses Microsoft Graph API to send emails
2. **GraphServiceClient**: Microsoft Graph SDK client with Azure AD authentication
3. **ConsultationController** (`Controllers/ConsultationController.cs`): API endpoint for booking
4. **ConsultationBooking Model** (`Models/ConsultationBooking.cs`): Data model
5. **Consultation Page** (`Client/Pages/Consultation.razor`): Calls the API on confirmation

## Advantages of Microsoft Graph API

- **Modern & Secure**: Uses OAuth 2.0 client credentials flow
- **No SMTP Issues**: Bypasses SMTP AUTH restrictions
- **Better Management**: Centralized app permissions in Azure AD
- **Rich Features**: Access to calendar, contacts, and more
- **Scalable**: Designed for enterprise applications

## Support

For additional help configuring email:
- Email: info@catalystsoftware.com
- Phone: (555) 123-4567
- Microsoft Graph Documentation: https://docs.microsoft.com/graph/

