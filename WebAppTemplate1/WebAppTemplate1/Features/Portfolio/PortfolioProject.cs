namespace WebAppTemplate1.Features.Portfolio;

/// <summary>
/// Represents a portfolio project with associated metadata and images.
/// Immutable DTO for transferring portfolio data between server and client.
/// </summary>
public sealed record PortfolioProject
{
    /// <summary>
    /// Unique identifier for the project.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Display title of the project.
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// Category/tags for filtering (e.g., "Web Design • Food & Beverage").
    /// </summary>
    public required string Category { get; init; }

    /// <summary>
    /// Detailed description of the project.
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// Collection of image URLs for the project gallery.
    /// First image serves as the thumbnail.
    /// </summary>
    public required IReadOnlyList<string> Images { get; init; }

    /// <summary>
    /// URL to the live project (optional).
    /// </summary>
    public string? ProjectUrl { get; init; }

    /// <summary>
    /// Technologies used in the project (optional).
    /// </summary>
    public IReadOnlyList<string>? Technologies { get; init; }
}
