using WebAppTemplate1.Client.Pages;
using WebAppTemplate1.Components;
using WebAppTemplate1.Services;
using WebAppTemplate1.Services.Email;
using WebAppTemplate1.Services.Calendar;

var builder = WebApplication.CreateBuilder(args);

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
builder.Services.AddScoped<ICalendarService, OutlookCalendarService>();

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
