/**
 * Get the client's timezone using the Intl API
 * Returns the IANA timezone identifier (e.g., "America/New_York", "Europe/London")
 */
export function getClientTimezone() {
    try {
        return Intl.DateTimeFormat().resolvedOptions().timeZone;
    } catch (error) {
        console.warn('Failed to get client timezone:', error);
        return null;
    }
}

/**
 * Get the current UTC offset in minutes
 */
export function getUTCOffset() {
    return new Date().getTimezoneOffset();
}
