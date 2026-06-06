namespace WebAppTemplate1.Features.Portfolio;

/// <summary>
/// Service responsible for providing portfolio project data.
/// Single Responsibility: Manages portfolio data retrieval and filtering.
/// Future: Can be extended to load from database, CMS, or external API.
/// </summary>
public interface IPortfolioService
{
    /// <summary>
    /// Retrieves all portfolio projects.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>Collection of all portfolio projects.</returns>
    Task<IReadOnlyList<PortfolioProject>> GetAllProjectsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves portfolio projects filtered by category.
    /// </summary>
    /// <param name="category">Category to filter by (case-insensitive).</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>Filtered collection of portfolio projects.</returns>
    Task<IReadOnlyList<PortfolioProject>> GetProjectsByCategoryAsync(string category, CancellationToken cancellationToken = default);
}
