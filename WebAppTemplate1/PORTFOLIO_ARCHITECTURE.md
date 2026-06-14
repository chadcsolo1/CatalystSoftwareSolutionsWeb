# Portfolio Feature - Architecture Documentation

## Overview
This document explains the architecture, design decisions, and SOLID principles applied to the Portfolio feature.

---

## Architectural Style: Vertical Slice Architecture

The Portfolio feature is organized by **feature**, not by technical layer:

```
WebAppTemplate1/
└── Features/
    └── Portfolio/
        ├── PortfolioProject.cs       (Domain model/DTO)
        ├── IPortfolioService.cs       (Service interface)
        ├── PortfolioService.cs        (Service implementation)
        └── PortfolioController.cs     (API endpoint)

WebAppTemplate1.Client/
├── Features/
│   └── Portfolio/
│       └── PortfolioProject.cs       (Client DTO)
└── Pages/
    └── Portfolio.razor                (UI component)
```

**Why vertical slices?**
- **Feature isolation**: All portfolio logic is in one place
- **Easy to extend**: Add new portfolio features without touching other code
- **Clear ownership**: Portfolio team owns this entire slice
- **Testable**: Each slice can be tested independently

---

## SOLID Principles Applied

### 1. **Single Responsibility Principle (SRP)**

Each class has ONE reason to change:

#### `PortfolioProject` (DTO)
**Responsibility**: Represent portfolio data
- Changes only if portfolio data structure changes
- Immutable `record` type (thread-safe, no side effects)

#### `IPortfolioService`
**Responsibility**: Define portfolio operations contract
- Changes only if business requirements for portfolio retrieval change

#### `PortfolioService`
**Responsibility**: Implement portfolio data logic
- Changes only if data source changes (e.g., move to database)
- Encapsulates initialization and filtering logic

#### `PortfolioController`
**Responsibility**: Handle HTTP concerns
- Changes only if API contract changes
- Maps HTTP → service → HTTP
- No business logic

#### `Portfolio.razor`
**Responsibility**: Render portfolio UI
- Changes only if UI/UX changes
- No business logic
- Delegates data fetching to API

---

### 2. **Open/Closed Principle (OCP)**

**Open for extension, closed for modification:**

#### Current Design
```csharp
public interface IPortfolioService
{
    Task<IReadOnlyList<PortfolioProject>> GetAllProjectsAsync(...);
    Task<IReadOnlyList<PortfolioProject>> GetProjectsByCategoryAsync(...);
}
```

#### Future Extension (WITHOUT modifying existing code)
```csharp
// Add database-backed implementation
public class DatabasePortfolioService : IPortfolioService
{
    private readonly ApplicationDbContext _db;

    public async Task<IReadOnlyList<PortfolioProject>> GetAllProjectsAsync(...)
    {
        return await _db.Projects.ToListAsync();
    }
}

// Register in Program.cs
builder.Services.AddScoped<IPortfolioService, DatabasePortfolioService>();
```

**No changes to:**
- `PortfolioController`
- `Portfolio.razor`
- API contract

---

### 3. **Liskov Substitution Principle (LSP)**

Any implementation of `IPortfolioService` can be substituted without breaking code:

```csharp
// In-memory (current)
IPortfolioService service = new PortfolioService();

// Database (future)
IPortfolioService service = new DatabasePortfolioService();

// CMS (future)
IPortfolioService service = new CMSPortfolioService();

// Mock (testing)
IPortfolioService service = new MockPortfolioService();
```

All implementations honor the contract:
- Return `IReadOnlyList<PortfolioProject>`
- Support cancellation tokens
- Handle errors consistently

---

### 4. **Interface Segregation Principle (ISP)**

**Focused interface** - clients depend only on what they need:

```csharp
public interface IPortfolioService
{
    Task<IReadOnlyList<PortfolioProject>> GetAllProjectsAsync(...);
    Task<IReadOnlyList<PortfolioProject>> GetProjectsByCategoryAsync(...);
}
```

**Why not a "god interface"?**
```csharp
// ❌ BAD: Fat interface with unrelated concerns
public interface IPortfolioService
{
    Task<IReadOnlyList<PortfolioProject>> GetAllProjectsAsync(...);
    Task SaveProjectAsync(...);              // Write operation
    Task DeleteProjectAsync(...);            // Delete operation
    Task<byte[]> ExportToExcelAsync(...);   // Export concern
    Task SendNewsletterAsync(...);           // Email concern (unrelated!)
}
```

Our interface is **focused** on **retrieval only** - clients reading portfolios don't need to know about writes, exports, or emails.

---

### 5. **Dependency Inversion Principle (DIP)**

**High-level modules depend on abstractions, not implementations:**

```csharp
// ✅ GOOD: Controller depends on abstraction
public class PortfolioController
{
    private readonly IPortfolioService _service;  // Abstraction

    public PortfolioController(IPortfolioService service)
    {
        _service = service;
    }
}

// ❌ BAD: Controller depends on concrete implementation
public class PortfolioController
{
    private readonly PortfolioService _service;  // Concrete class

    public PortfolioController()
    {
        _service = new PortfolioService();  // Hard dependency!
    }
}
```

**Benefits:**
- Easy to swap implementations
- Testable (inject mocks)
- Decoupled from implementation details

---

## Separation of Concerns: Client vs Server

### **Server-Side Responsibilities**
Located in: `WebAppTemplate1/Features/Portfolio/`

✅ **Portfolio business logic**
- `PortfolioService` encapsulates data retrieval
- Filtering logic lives on server
- Single source of truth for portfolio data

✅ **API endpoint**
- `PortfolioController` exposes HTTP API
- Input validation (never trust client)
- Error handling and logging

✅ **Static asset hosting**
- Images stored in `wwwroot/images/Savoria/`
- Server controls which images are exposed

**Why server?**
- **Security**: Business logic not exposed to client
- **Performance**: Server can optimize data before sending
- **Scalability**: Easy to add database later
- **Maintainability**: Single place to update data

---

### **Client-Side Responsibilities**
Located in: `WebAppTemplate1.Client/Pages/Portfolio.razor`

✅ **UI rendering**
- Blazor component renders portfolio cards
- Modal gallery for project details

✅ **User interactions**
- Filter buttons
- Modal open/close
- Image navigation

✅ **API consumption**
- `HttpClient` fetches data from server API
- Handles loading and error states

✅ **Presentation logic only**
- No business rules
- No database access
- No sensitive operations

**Why client?**
- **Interactive UI**: Blazor WebAssembly for rich UX
- **Responsiveness**: Client-side filtering is instant
- **Separation**: Client doesn't know HOW data is stored

---

## Performance Considerations

### 1. **Avoid Unnecessary Allocations**

```csharp
// ✅ GOOD: Cache projects (initialized once)
private readonly IReadOnlyList<PortfolioProject> _projects;

public PortfolioService()
{
    _projects = InitializeProjects();  // One-time allocation
}

public Task<IReadOnlyList<PortfolioProject>> GetAllProjectsAsync(...)
{
    return Task.FromResult(_projects);  // No allocation
}
```

```csharp
// ❌ BAD: Recreate projects on every call
public Task<IReadOnlyList<PortfolioProject>> GetAllProjectsAsync(...)
{
    var projects = new List<PortfolioProject> { /* recreate */ };
    return Task.FromResult(projects);  // Allocates every time
}
```

---

### 2. **Async/Await (Non-Blocking)**

```csharp
// ✅ GOOD: Async all the way
protected override async Task OnInitializedAsync()
{
    await LoadProjectsAsync();  // Non-blocking
}

private async Task LoadProjectsAsync()
{
    var projects = await Http.GetFromJsonAsync<List<PortfolioProject>>("api/portfolio");
}
```

---

### 3. **Lazy Loading Images**

```html
<img src="@project.Images.First()" alt="@project.Title" loading="lazy" />
```

**Why?**
- Images load only when scrolled into view
- Reduces initial page load time
- Better perceived performance

---

### 4. **Minimal API Payloads**

```csharp
// Only send necessary data
public sealed record PortfolioProject
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public required string Category { get; init; }
    public required string Description { get; init; }
    public required IReadOnlyList<string> Images { get; init; }
    // No unnecessary fields
}
```

---

## Security Considerations

### 1. **Server-Side Input Validation**

```csharp
[HttpGet("category/{category}")]
public async Task<ActionResult<IReadOnlyList<PortfolioProject>>> GetProjectsByCategory(
    string category,
    CancellationToken cancellationToken)
{
    // ✅ VALIDATE: Never trust client input
    if (string.IsNullOrWhiteSpace(category))
    {
        return BadRequest("Category cannot be empty.");
    }

    // Proceed...
}
```

**Why?**
- Client-side validation can be bypassed
- Attacker can craft malicious requests
- Server is the last line of defense

---

### 2. **No Sensitive Data Exposed**

```csharp
// ✅ GOOD: DTO contains only public data
public sealed record PortfolioProject
{
    public required string Title { get; init; }
    public required IReadOnlyList<string> Images { get; init; }
    // No internal IDs, costs, client info, etc.
}
```

---

### 3. **Proper Error Handling**

```csharp
try
{
    var projects = await _portfolioService.GetAllProjectsAsync(cancellationToken);
    return Ok(projects);
}
catch (Exception ex)
{
    // ✅ LOG: Record error for investigation
    _logger.LogError(ex, "Failed to retrieve portfolio projects");

    // ✅ SAFE: Don't leak internal details to client
    return StatusCode(500, "An error occurred while retrieving projects.");
}
```

---

## Scalability Path

### **Current State: In-Memory**
```csharp
public class PortfolioService : IPortfolioService
{
    private readonly IReadOnlyList<PortfolioProject> _projects;

    public PortfolioService()
    {
        _projects = InitializeProjects();  // Hardcoded data
    }
}
```

**Limitations:**
- Data lost on restart
- No admin UI to add projects
- Not suitable for 1000+ projects

---

### **Future: Database-Backed**
```csharp
public class DatabasePortfolioService : IPortfolioService
{
    private readonly ApplicationDbContext _db;
    private readonly IMemoryCache _cache;

    public async Task<IReadOnlyList<PortfolioProject>> GetAllProjectsAsync(...)
    {
        // Cache for 5 minutes
        return await _cache.GetOrCreateAsync("all-projects", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
            return await _db.Projects.AsNoTracking().ToListAsync(cancellationToken);
        });
    }
}
```

**Benefits:**
- Persistent storage
- Admin UI to manage projects
- Caching for performance
- Pagination for large datasets

**No changes needed to:**
- API contract (`PortfolioController`)
- Client UI (`Portfolio.razor`)
- Deployment

---

## Testing Strategy

### **Unit Tests: Service Logic**
```csharp
public class PortfolioServiceTests
{
    [Fact]
    public async Task GetAllProjectsAsync_ReturnsAllProjects()
    {
        // Arrange
        var service = new PortfolioService();

        // Act
        var result = await service.GetAllProjectsAsync();

        // Assert
        Assert.NotEmpty(result);
        Assert.Contains(result, p => p.Title == "Savoria Restaurant Website");
    }

    [Fact]
    public async Task GetProjectsByCategoryAsync_FiltersCorrectly()
    {
        // Arrange
        var service = new PortfolioService();

        // Act
        var result = await service.GetProjectsByCategoryAsync("Web");

        // Assert
        Assert.All(result, p => Assert.Contains("Web", p.Category));
    }
}
```

---

### **Integration Tests: API Endpoint**
```csharp
public class PortfolioControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public PortfolioControllerTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetAllProjects_ReturnsOk()
    {
        // Act
        var response = await _client.GetAsync("/api/portfolio");

        // Assert
        response.EnsureSuccessStatusCode();
        var projects = await response.Content.ReadFromJsonAsync<List<PortfolioProject>>();
        Assert.NotNull(projects);
    }
}
```

---

## Maintainability

### **Clean Code Principles Applied**

✅ **Meaningful names**
```csharp
// ✅ GOOD
public Task<IReadOnlyList<PortfolioProject>> GetProjectsByCategoryAsync(string category, ...)

// ❌ BAD
public Task<IReadOnlyList<PortfolioProject>> GetProjs(string cat, ...)
```

✅ **Small, focused methods**
```csharp
// Each method does ONE thing
private void FilterProjects(string filter) { }
private void OpenModal(PortfolioProject project) { }
private void CloseModal() { }
```

✅ **Immutable data structures**
```csharp
// ✅ GOOD: Immutable record
public sealed record PortfolioProject { ... }

// ❌ BAD: Mutable class with public setters
public class PortfolioProject
{
    public string Title { get; set; }  // Can be changed
}
```

✅ **Self-documenting code**
```csharp
/// <summary>
/// Retrieves all portfolio projects.
/// </summary>
/// <param name="cancellationToken">Cancellation token for async operation.</param>
/// <returns>Collection of all portfolio projects.</returns>
Task<IReadOnlyList<PortfolioProject>> GetAllProjectsAsync(CancellationToken cancellationToken = default);
```

---

## Summary

This Portfolio feature demonstrates **production-grade architecture**:

1. ✅ **Vertical Slice Architecture** - Feature-focused organization
2. ✅ **SOLID Principles** - Every principle applied correctly
3. ✅ **Clean Separation** - Client UI, server logic, API layer
4. ✅ **Performance** - Cached data, lazy loading, minimal payloads
5. ✅ **Security** - Input validation, no sensitive data exposed
6. ✅ **Scalability** - Easy to migrate to database
7. ✅ **Maintainability** - Clear, testable, self-documenting code

**Future Enhancements:**
- Database integration (`DatabasePortfolioService`)
- Admin UI for managing projects
- Image optimization/resizing service
- Search functionality
- Pagination for large datasets

---

**Last Updated:** 2025
**Author:** Senior Software Engineer
**Framework:** .NET 10, Blazor WebAssembly
