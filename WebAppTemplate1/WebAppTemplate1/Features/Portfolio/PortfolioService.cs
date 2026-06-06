namespace WebAppTemplate1.Features.Portfolio;

/// <summary>
/// In-memory implementation of portfolio service.
/// Encapsulates portfolio data and provides filtering logic.
/// Future: Replace with database-backed implementation.
/// </summary>
public sealed class PortfolioService : IPortfolioService
{
    // Cached portfolio projects (immutable after initialization)
    private readonly IReadOnlyList<PortfolioProject> _projects;

    public PortfolioService()
    {
        // Initialize with hardcoded data
        // Future: Load from configuration, database, or CMS
        _projects = InitializeProjects();
    }

    public Task<IReadOnlyList<PortfolioProject>> GetAllProjectsAsync(CancellationToken cancellationToken = default)
    {
        // Return cached projects (no allocation)
        return Task.FromResult(_projects);
    }

    public Task<IReadOnlyList<PortfolioProject>> GetProjectsByCategoryAsync(
        string category,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(category) || category.Equals("All", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(_projects);
        }

        // Filter projects by category (case-insensitive contains)
        var filtered = _projects
            .Where(p => p.Category.Contains(category, StringComparison.OrdinalIgnoreCase))
            .ToList()
            .AsReadOnly();

        return Task.FromResult<IReadOnlyList<PortfolioProject>>(filtered);
    }

    private static IReadOnlyList<PortfolioProject> InitializeProjects()
    {
        return new List<PortfolioProject>
        {
            new PortfolioProject
            {
                Id = "savoria-restaurant",
                Title = "Savoria Restaurant Website",
                Category = "Web Design • Food & Beverage",
                Description = "A modern, responsive website for Savoria Restaurant featuring an elegant design, online menu, and reservation system. Built with .NET and Blazor for a seamless user experience.",
                Images = new List<string>
                {
                    "/images/Savoria/Hero.png",
                    "/images/Savoria/Menu-hero.png",
                    "/images/Savoria/Menu-Entre.png",
                    "/images/Savoria/About-Hero.png",
                    "/images/Savoria/About-Team.png",
                    "/images/Savoria/Reserve-hero.png",
                    "/images/Savoria/Contact-hero.png"
                }.AsReadOnly(),
                Technologies = new List<string> { ".NET", "Blazor", "CSS3", "HTML5" }.AsReadOnly(),
                ProjectUrl = null // No live URL yet
            },
            new PortfolioProject
            {
                Id = "ecommerce-platform",
                Title = "E-Commerce Platform",
                Category = "E-Commerce • Web Design",
                Description = "A full-featured e-commerce solution with inventory management, payment processing, and customer analytics.",
                Images = new List<string>().AsReadOnly(),
                Technologies = new List<string> { ".NET", "SQL Server", "Stripe" }.AsReadOnly()
            },
            new PortfolioProject
            {
                Id = "fitness-app",
                Title = "Fitness Tracking App",
                Category = "Mobile Apps • Health",
                Description = "Mobile application for tracking workouts, nutrition, and fitness goals with real-time data synchronization.",
                Images = new List<string>().AsReadOnly(),
                Technologies = new List<string> { ".NET MAUI", "Azure", "SignalR" }.AsReadOnly()
            },
            new PortfolioProject
            {
                Id = "brand-identity",
                Title = "Brand Identity Design",
                Category = "Branding • Design",
                Description = "Complete brand identity package including logo design, color palette, typography, and brand guidelines.",
                Images = new List<string>().AsReadOnly()
            },
            new PortfolioProject
            {
                Id = "video-streaming",
                Title = "Video Streaming Service",
                Category = "Web Design • Entertainment",
                Description = "Custom video streaming platform with content management, user subscriptions, and adaptive streaming.",
                Images = new List<string>().AsReadOnly(),
                Technologies = new List<string> { ".NET", "Azure Media Services", "CDN" }.AsReadOnly()
            },
            new PortfolioProject
            {
                Id = "corporate-website",
                Title = "Corporate Website",
                Category = "Web Design • Corporate",
                Description = "Professional corporate website with CMS integration, multilingual support, and analytics dashboard.",
                Images = new List<string>().AsReadOnly(),
                Technologies = new List<string> { ".NET", "Blazor", "CMS" }.AsReadOnly()
            }
        }.AsReadOnly();
    }
}
