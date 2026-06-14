namespace WebAppTemplate1.Client.Features.Portfolio;

/// <summary>
/// Client-side DTO for portfolio projects.
/// Matches server-side contract for seamless serialization.
/// </summary>
public sealed record PortfolioProject
{
    public string Id { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public IReadOnlyList<string> Images { get; init; } = Array.Empty<string>();
    public string? ProjectUrl { get; init; }
    public IReadOnlyList<string>? Technologies { get; init; }
}
