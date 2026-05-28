# Email Configuration Setup

This document explains how to configure email notifications for consultation bookings.

## Overview

When a user confirms a consultation appointment, the system will automatically send a confirmation email containing:
- Appointment date and time
- User's contact information
- Next steps and reminders
- Professional branded email template

## Configuration Steps

### 1. Update appsettings.json

Open `appsettings.json` and configure the email settings:

```json
"Email": {
  "SmtpHost": "smtp.gmail.com",
  "SmtpPort": "587",
  "FromAddress": "noreply@catalystsoftware.com",
  "FromName": "Catalyst Software Solutions",
  "Username": "your-email@gmail.com",
  "Password": "your-app-password"
}
```

### 2. For Gmail Users

If using Gmail, you'll need to:

1. Enable 2-factor authentication on your Google account
2. Generate an App Password:
   - Go to https://myaccount.google.com/security
   - Select "2-Step Verification"
   - Scroll to "App passwords"
   - Generate a new app password for "Mail"
   - Use this password in the `Password` field

### 3. For Other Email Providers

Update the SMTP settings according to your provider:

**Microsoft 365/Outlook:**
```json
"SmtpHost": "smtp.office365.com",
"SmtpPort": "587"
```

**SendGrid:**
```json
"SmtpHost": "smtp.sendgrid.net",
"SmtpPort": "587",
"Username": "apikey",
"Password": "your-sendgrid-api-key"
```

**AWS SES:**
```json
"SmtpHost": "email-smtp.us-east-1.amazonaws.com",
"SmtpPort": "587"
```

### 4. Production Considerations

For production environments:

1. **Use User Secrets** (Development):
   ```bash
   dotnet user-secrets set "Email:Username" "your-email@gmail.com"
   dotnet user-secrets set "Email:Password" "your-app-password"
   ```

2. **Use Environment Variables** (Production):
   ```bash
   Email__Username=your-email@gmail.com
   Email__Password=your-password
   ```

3. **Use Azure Key Vault or similar** for secure credential storage

### 5. Testing

To test the email functionality:

1. Ensure email credentials are configured
2. Run the application
3. Navigate to `/consultation`
4. Complete the consultation booking wizard
5. Check the provided email address for the confirmation

## Email Template

The confirmation email includes:
- Professional branded header with gradient
- Complete appointment details
- What's next section with reminders
- Contact information footer
- Mobile-responsive design

## Troubleshooting

**Email not sending:**
- Check SMTP credentials are correct
- Verify firewall/network allows SMTP connections
- Check application logs for detailed error messages
- Ensure the recipient email is valid

**Gmail specific issues:**
- Verify 2FA is enabled
- Confirm you're using an App Password (not your regular password)
- Check "Less secure app access" is not required anymore (use App Passwords instead)

## Architecture

The email functionality consists of:

1. **EmailService** (`Services/EmailService.cs`): Handles SMTP connection and email sending
2. **ConsultationController** (`Controllers/ConsultationController.cs`): API endpoint for booking
3. **ConsultationBooking Model** (`Models/ConsultationBooking.cs`): Data model
4. **Consultation Page** (`Client/Pages/Consultation.razor`): Calls the API on confirmation

## Support

For additional help configuring email:
- Email: info@catalystsoftware.com
- Phone: (555) 123-4567
