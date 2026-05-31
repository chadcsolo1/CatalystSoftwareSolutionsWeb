# SendGrid Email Setup Guide

## Overview
SendGrid is now configured as your primary email provider for the Catalyst Software Solutions website. SendGrid provides excellent deliverability, scalability, and detailed analytics for transactional emails.

## Prerequisites
- SendGrid account (free tier available at https://sendgrid.com/pricing/)
- Verified sender email address

## Step 1: Create a SendGrid Account

1. Go to https://sendgrid.com/
2. Sign up for a free account (up to 100 emails/day free)
3. Verify your email address

## Step 2: Create an API Key

1. Log in to your SendGrid account
2. Navigate to **Settings** → **API Keys** (https://app.sendgrid.com/settings/api_keys)
3. Click **Create API Key**
4. Choose a descriptive name (e.g., "Catalyst Website Production")
5. Select **Full Access** or at minimum **Mail Send** permissions
6. Click **Create & View**
7. **IMPORTANT**: Copy the API key immediately (you won't be able to see it again!)

## Step 3: Configure the Application

### Option A: Using appsettings.json (Development Only - Not Recommended for Production)

Open `appsettings.json` and replace the placeholder:

```json
"SendGrid": {
  "ApiKey": "SG.your-actual-api-key-here"
}
```

**⚠️ WARNING**: Never commit API keys to source control!

### Option B: Using User Secrets (Recommended for Development)

1. Right-click the `WebAppTemplate1` project in Visual Studio
2. Select **Manage User Secrets**
3. Add the following to `secrets.json`:

```json
{
  "SendGrid": {
    "ApiKey": "SG.your-actual-api-key-here"
  }
}
```

### Option C: Using Environment Variables (Recommended for Production)

Set the environment variable:

**Windows PowerShell:**
```powershell
$env:SendGrid__ApiKey = "SG.your-actual-api-key-here"
```

**Linux/Mac:**
```bash
export SendGrid__ApiKey="SG.your-actual-api-key-here"
```

**Azure App Service:**
1. Go to your App Service in Azure Portal
2. Navigate to **Configuration** → **Application settings**
3. Add new setting:
   - Name: `SendGrid:ApiKey`
   - Value: `SG.your-actual-api-key-here`

## Step 4: Verify Sender Email Address

SendGrid requires you to verify the sender email address:

### Single Sender Verification (Quick Setup)

1. In SendGrid, go to **Settings** → **Sender Authentication**
2. Click **Verify a Single Sender**
3. Fill in your details:
   - From Name: `Catalyst Software Solutions`
   - From Email Address: `noreply@catalystsoftwaresolutions.com`
4. Check your email and click the verification link

### Domain Authentication (Recommended for Production)

For better deliverability, authenticate your entire domain:

1. Go to **Settings** → **Sender Authentication** → **Domain Authentication**
2. Select your DNS host
3. Follow the instructions to add DNS records (SPF, DKIM, DMARC)
4. Wait for verification (usually takes 24-48 hours)

## Step 5: Update Configuration (if needed)

The sender email is configured in `appsettings.json`:

```json
"EmailProvider": {
  "FromAddress": "noreply@catalystsoftwaresolutions.com",
  "FromName": "Catalyst Software Solutions"
}
```

**Make sure this matches your verified sender email in SendGrid!**

## Step 6: Test the Configuration

1. Start your application
2. Navigate to the Consultation page
3. Submit a test booking
4. Check SendGrid dashboard for email activity

## Switching Providers

Your application supports multiple email providers. To switch:

**Current Configuration:**
- **Primary**: SendGrid
- **Fallback**: Microsoft Graph
- **Fallback Enabled**: No

To enable automatic fallback to Microsoft Graph if SendGrid fails:

```json
"EmailProvider": {
  "Provider": "SendGrid",
  "FallbackProvider": "MicrosoftGraph",
  "EnableFallback": true
}
```

To switch to a different primary provider:

```json
"EmailProvider": {
  "Provider": "MicrosoftGraph",  // or "AzureCommunicationServices"
  "FallbackProvider": "SendGrid"
}
```

## SendGrid Dashboard Features

Access your SendGrid dashboard at https://app.sendgrid.com/

- **Activity Feed**: See all sent emails in real-time
- **Statistics**: Delivery rates, opens, clicks, bounces
- **Suppressions**: Manage bounces, spam reports, unsubscribes
- **Templates**: Create and manage email templates
- **Alerts**: Get notified of delivery issues

## Pricing

SendGrid offers multiple tiers:

- **Free**: 100 emails/day forever
- **Essentials**: Starting at $19.95/month (50,000 emails)
- **Pro**: Starting at $89.95/month (100,000 emails)

See https://sendgrid.com/pricing/ for current pricing.

## Troubleshooting

### Error: "SendGrid API key is not configured"
- Make sure you've added the API key using one of the methods in Step 3
- Restart your application after adding the key

### Error: "The from address does not match a verified Sender Identity"
- Verify your sender email in SendGrid (Step 4)
- Make sure the `FromAddress` in `appsettings.json` matches exactly

### Emails Not Being Delivered
1. Check the SendGrid Activity Feed for delivery status
2. Verify your domain authentication (Step 4)
3. Check recipient's spam folder
4. Review SendGrid suppression lists

### Rate Limiting
- Free tier: 100 emails/day
- If you exceed limits, emails will queue or fail
- Upgrade your plan if needed

## Security Best Practices

1. ✅ **Never commit API keys to Git**
2. ✅ **Use User Secrets for development**
3. ✅ **Use environment variables or Azure Key Vault for production**
4. ✅ **Rotate API keys periodically**
5. ✅ **Use restricted permissions (Mail Send only) when possible**
6. ✅ **Monitor SendGrid activity for unusual patterns**

## Support

- SendGrid Documentation: https://docs.sendgrid.com/
- SendGrid Support: https://support.sendgrid.com/
- Application Email Architecture: See `EMAIL_ARCHITECTURE.md` in project root

## Next Steps

1. [ ] Create SendGrid account
2. [ ] Generate API key
3. [ ] Configure User Secrets with API key
4. [ ] Verify sender email address
5. [ ] Test consultation booking email
6. [ ] Set up domain authentication (for production)
7. [ ] Configure production environment variables
