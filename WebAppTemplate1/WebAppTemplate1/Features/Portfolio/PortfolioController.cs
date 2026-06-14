using Microsoft.AspNetCore.Mvc;

namespace WebAppTemplate1.Features.Portfolio;

/// <summary>
/// API controller for portfolio operations.
/// Separation of Concerns: Handles HTTP concerns, delegates business logic to service.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public sealed class PortfolioController : ControllerBase
{
    private readonly IPortfolioService _portfolioService;
    private readonly ILogger<PortfolioController> _logger;

    public PortfolioController(
        IPortfolioService portfolioService,
        ILogger<PortfolioController> logger)
    {
        _portfolioService = portfolioService ?? throw new ArgumentNullException(nameof(portfolioService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Retrieves all portfolio projects.
    /// GET /api/portfolio
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of portfolio projects.</returns>
    [HttpGet]
    [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Any)] // Cache for 5 minutes
    [ProducesResponseType(typeof(IReadOnlyList<PortfolioProject>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IReadOnlyList<PortfolioProject>>> GetAllProjects(
        CancellationToken cancellationToken)
    {
        try
        {
            var projects = await _portfolioService.GetAllProjectsAsync(cancellationToken);
            return Ok(projects);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve portfolio projects");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving projects.");
        }
    }

    /// <summary>
    /// Retrieves portfolio projects filtered by category.
    /// GET /api/portfolio/category/{category}
    /// </summary>
    /// <param name="category">Category to filter by.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Filtered collection of portfolio projects.</returns>
    [HttpGet("category/{category}")]
    [ProducesResponseType(typeof(IReadOnlyList<PortfolioProject>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IReadOnlyList<PortfolioProject>>> GetProjectsByCategory(
        string category,
        CancellationToken cancellationToken)
    {
        // Input validation (server-side, never trust client)
        if (string.IsNullOrWhiteSpace(category))
        {
            return BadRequest("Category cannot be empty.");
        }

        try
        {
            var projects = await _portfolioService.GetProjectsByCategoryAsync(category, cancellationToken);
            return Ok(projects);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve portfolio projects for category: {Category}", category);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving projects.");
        }
    }
}
