# Consultation Availability Filtering

## Overview
The consultation booking system now filters out time slots that are already booked in your Outlook calendar. When a user selects a date, only genuinely available times will be displayed.

## How It Works

### Server-Side (OutlookCalendarService)
1. **Fetches Calendar Events**: When a date is selected, the system queries your Outlook calendar using Microsoft Graph API
2. **Checks Business Hours**: Only shows times within configured business hours (Mon/Tue/Fri, 8 AM - 5 PM EST)
3. **Filters Busy Slots**: Compares each potential time slot against existing calendar events
4. **Respects Buffer Time**: Includes a 10-minute buffer between consultations to prevent back-to-back bookings

### Client-Side (Consultation.razor)
1. **Dynamic Loading**: When a user clicks a date, the UI calls `/api/consultation/available-times?date={date}`
2. **Loading State**: Shows "Loading available times..." while fetching from the server
3. **Real-Time Availability**: Displays only the times returned by the server
4. **No Slots Message**: If no times are available, shows a helpful message to select another date

## Key Changes Made

### OutlookCalendarService.cs
- Fixed time slot parsing with new `ParseTimeSlot()` method to correctly parse "HH:MM AM/PM" format
- Improved overlap detection logic to catch all overlap scenarios including partial overlaps
- Added debug logging to track which events conflict with requested time slots

### Consultation.razor
- Changed from hardcoded time slots to dynamic API calls
- Added `LoadAvailableTimesAsync()` method to fetch times when a date is selected
- Added loading indicator and empty state messages
- Changed `SelectDate()` to async and automatically loads times
- Clears selected time when date changes to prevent invalid combinations

### ConsultationController.cs
- Already had the `/api/consultation/available-times` endpoint configured
- Returns proper JSON response with `availableSlots` array
- Gracefully degrades to default slots if calendar permissions are missing

## Testing the Feature

1. **Run the application locally**
2. **Navigate to the consultation page**
3. **Select a date** - You should see "Loading available times..."
4. **View available times** - Only times not blocked by your Outlook calendar will appear

## Calendar Permission Requirements
To enable full availability filtering, ensure your Azure AD app registration has:
- `Calendars.Read` - To check existing events
- `Calendars.ReadWrite` - To create new consultation events

See `CALENDAR_SETUP.md` for detailed permission setup instructions.

## Business Rules
- **Business Days**: Monday, Tuesday, Friday only
- **Business Hours**: 8:00 AM to 5:00 PM EST
- **Consultation Duration**: 30 minutes
- **Buffer Between Consultations**: 10 minutes
- **Overlap Detection**: Any event that overlaps with a time slot (including buffer) will hide that slot

## Benefits
1. **No Double Bookings**: Users cannot select times when you're already booked
2. **Real-Time Accuracy**: Reflects current state of your Outlook calendar
3. **Better User Experience**: Users only see genuinely available times
4. **Automatic Synchronization**: No manual management of availability needed
