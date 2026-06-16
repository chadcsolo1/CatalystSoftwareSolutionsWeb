# Calendar Integration Setup Guide

This guide explains how to enable full Outlook calendar integration for the consultation booking system.

## Overview

The consultation booking system includes Outlook calendar integration with **graceful degradation**:

- **Without calendar permissions**: Bookings work, emails are sent, but calendar events are not created and all time slots are shown as available
- **With calendar permissions**: Full integration - calendar events are created automatically and availability is checked against your Outlook calendar

## Current Status

✅ **Working Locally & Production**: Email notifications and consultation bookings  
⚠️ **Requires Setup**: Calendar integration (optional but recommended)

## Azure AD App Permissions Setup

To enable full calendar integration, add these permissions to your Azure AD app registration:

### Step 1: Navigate to Azure Portal

1. Go to [Azure Portal](https://portal.azure.com)
2. Navigate to **Azure Active Directory**
3. Select **App registrations**
4. Find your app: **Client ID: `9ed7d596-ab6f-40d2-9f62-6c102fdc42d6`**

### Step 2: Add Calendar Permissions

1. Click on **API permissions** in the left menu
2. Click **+ Add a permission**
3. Select **Microsoft Graph**
4. Choose **Application permissions** (not Delegated)
5. Search for and add these permissions:
   - ✅ `Calendars.Read` - Read calendar availability
   - ✅ `Calendars.ReadWrite` - Create calendar events
6. Click **Add permissions**

### Step 3: Grant Admin Consent

**Important**: Application permissions require admin consent

1. Click **Grant admin consent for [Your Tenant Name]**
2. Confirm by clicking **Yes**
3. Wait for the status to show **Granted for [Your Tenant Name]** with a green checkmark

### Step 4: Verify Configuration

Your API permissions should now include:

| API / Permission Name | Type | Status |
|----------------------|------|--------|
| Microsoft Graph / Calendars.Read | Application | ✅ Granted |
| Microsoft Graph / Calendars.ReadWrite | Application | ✅ Granted |
| Microsoft Graph / Mail.Send | Application | ✅ Granted |
| Microsoft Graph / User.Read.All | Application | ✅ Granted |

## Configuration

The calendar owner email is configured in `appsettings.json`:

```json
{
  "Calendar": {
    "CalendarOwnerEmail": "chad_solomon@catalystsoftwaresolutions.com"
  }
}
```

This should match the mailbox whose calendar you want to check for availability and create events in.

## Testing Calendar Integration

### Local Testing

1. **Add Calendar Permissions** (follow steps above)
2. **Run the application** locally
3. **Navigate to Consultation page**
4. **Select a date** - You should see time slots filtered by your actual calendar availability
5. **Book a consultation** - A calendar event should be created in your Outlook calendar

### Verify It's Working

Check the application logs for these messages:

**Calendar Integration Enabled:**
```
Calendar event created successfully. Event ID: [event-id]
Found X available slots for [date]
```

**Calendar Integration Disabled (Graceful Degradation):**
```
Failed to create calendar event. Booking will proceed without calendar integration.
Calendar API access denied. Returning default time slots.
```

## Features

### With Calendar Permissions

- ✅ **Smart Availability**: Only shows time slots when you're actually available
- ✅ **Conflict Prevention**: Blocks times when you have existing meetings
- ✅ **Automatic Calendar Events**: Creates events in your Outlook calendar with client details
- ✅ **Buffer Time**: Ensures 10 minutes between consultations
- ✅ **Business Hours**: Mon, Tue, Fri 8:00 AM - 5:00 PM EST
- ✅ **Email Notifications**: Sends emails to both you and the client

### Without Calendar Permissions (Fallback)

- ✅ **Basic Booking**: Consultations can still be booked
- ✅ **Email Notifications**: Sends emails to both you and the client
- ⚠️ **All Slots Shown**: Shows all time slots as available (no conflict checking)
- ⚠️ **No Auto Calendar Events**: You must manually add to your calendar

## Troubleshooting

### "Access is denied" Error

**Cause**: Calendar permissions not configured or admin consent not granted

**Solution**: 
1. Verify permissions are added (Step 2 above)
2. Ensure admin consent is granted (Step 3 above)
3. Wait 5-10 minutes for permissions to propagate
4. Restart the application

### Calendar Events Not Creating

**Check these items**:
1. ✅ Permissions granted (`Calendars.ReadWrite`)
2. ✅ Admin consent approved
3. ✅ Calendar owner email is correct in `appsettings.json`
4. ✅ The calendar owner email exists in your Microsoft 365 tenant
5. ✅ Application has been restarted after permission changes

### Time Slots Not Filtering

**Check these items**:
1. ✅ Permissions granted (`Calendars.Read`)
2. ✅ Admin consent approved
3. ✅ Check application logs for "Calendar API access denied" warnings

## Production Deployment

The same Azure AD app and permissions work for both local development and production:

1. **Local Development**: Uses credentials from `appsettings.json`
2. **Production**: Uses credentials from `appsettings.json` (or Azure App Settings if overridden)

**Best Practice**: In production, override these settings using Azure App Service Configuration:
- `MicrosoftGraph:TenantId`
- `MicrosoftGraph:ClientId`
- `MicrosoftGraph:ClientSecret`
- `Calendar:CalendarOwnerEmail`

## Support

If you continue to experience issues:

1. Check the **Output window** in Visual Studio for detailed error messages
2. Verify all permissions are granted in Azure Portal
3. Ensure the calendar owner email is correct and exists in your tenant
4. Try clearing browser cache and restarting the application

## Architecture

- **Service**: `OutlookCalendarService.cs` - Handles all calendar operations
- **Interface**: `ICalendarService.cs` - Abstraction for calendar operations
- **Controller**: `ConsultationController.cs` - API endpoints with graceful degradation
- **Configuration**: `CalendarOptions.cs` - Calendar settings
