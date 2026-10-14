using System.Text;
using System.IO;
using WebAppTemplate1.Client.Pages;
using WebAppTemplate1.Components;
using WebAppTemplate1.Services;
using WebAppTemplate1.Services.Email;
using WebAppTemplate1.Services.Calendar;

// Load optional .env file from the current working directory and set environment variables.
// This allows storing local secrets in a .env file during development. Keys should use
// the double-underscore convention for nested configuration (e.g. GoogleCalendar__ServiceAccountJsonBase64).
var envFile = Path.Combine(Directory.GetCurrentDirectory(), ".env");
if (File.Exists(envFile))
{
    foreach (var raw in File.ReadAllLines(envFile))
    {
        var line = raw.Trim();
        if (string.IsNullOrEmpty(line) || line.StartsWith("#"))
            continue;

        var idx = line.IndexOf('=');
        if (idx <= 0)
            continue;

        var key = line.Substring(0, idx).Trim();
        var val = line.Substring(idx + 1).Trim();
        // Remove optional surrounding quotes
        if ((val.StartsWith("\"") && val.EndsWith("\"")) || (val.StartsWith("'") && val.EndsWith("'")))
        {
            val = val.Substring(1, val.Length - 2);
        }

        Environment.SetEnvironmentVariable(key, val);
    }
}

var builder = WebApplication.CreateBuilder(args);

// Configure data protection with a consistent key for container environments.
// In DigitalOcean App Platform, the key is stored as ASPNETCORE_DataProtection_Key environment variable
// to ensure antiforgery tokens remain valid across container restarts.
var dpKey = Environment.GetEnvironmentVariable("ASPNETCORE_DataProtection_Key");
if (!string.IsNullOrEmpty(dpKey))
{
    try
    {
        // Decode the base64 key and create a deterministic data protection setup
        var keyBytes = Convert.FromBase64String(dpKey);
        builder.Services.AddDataProtection()
            .SetApplicationName("CatalystSoftwareSolutionsWeb")
            .PersistKeysToFileSystem(new DirectoryInfo("/root/.aspnet/DataProtection-Keys"));
    }
    catch
    {
        // If key is invalid, fall back to default data protection (ephemeral keys)
        builder.Services.AddDataProtection()
            .SetApplicationName("CatalystSoftwareSolutionsWeb");
    }
}
else
{
    // Development/local: use default data protection
    builder.Services.AddDataProtection()
        .SetApplicationName("CatalystSoftwareSolutionsWeb");
}

// If a base64-encoded service account JSON was provided via .env or environment, decode it
// and set the regular GoogleCalendar__ServiceAccountJson env var so options binding works.
var base64Secret = Environment.GetEnvironmentVariable("GoogleCalendar__ServiceAccountJsonBase64")
                   ?? builder.Configuration["GoogleCalendar:ServiceAccountJsonBase64"];
if (!string.IsNullOrEmpty(base64Secret))
{
    try
    {
        var json = Encoding.UTF8.GetString(Convert.FromBase64String(base64Secret));
        // Set the decoded JSON into the application's configuration so the options binder
        // sees it. Environment.SetEnvironmentVariable after the ConfigurationRoot is built
        // won't update builder.Configuration, so write into configuration directly.
        builder.Configuration["GoogleCalendar:ServiceAccountJson"] = json;
    }
    catch
    {
        // ignore decode errors here; the app will surface configuration problems when binding
    }
}

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents();

// Add controllers for API endpoints
builder.Services.AddControllers();

// Register HttpClient for server-side pre-rendering
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri("https://localhost:7018/") });

// Register email services with all providers
builder.Services.AddEmailServices(builder.Configuration);

// Register calendar services
builder.Services.Configure<CalendarOptions>(
    builder.Configuration.GetSection(CalendarOptions.SectionName));
// Configure Google calendar options and register GoogleCalendarService as the calendar implementation.
builder.Services.Configure<GoogleCalendarOptions>(
    builder.Configuration.GetSection(GoogleCalendarOptions.SectionName));
builder.Services.AddScoped<ICalendarService, GoogleCalendarService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapControllers(); // Map API controllers
app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(WebAppTemplate1.Client._Imports).Assembly);

app.Run();
