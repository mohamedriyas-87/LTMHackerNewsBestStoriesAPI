using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Linq;
using HackerNewsBestStories.Services;

[ApiController]
[Route("api/[controller]")]
public class HackerNewsController : ControllerBase
{
    private readonly IHackerNewsService _hackerNewsService;

    public HackerNewsController(IHackerNewsService hackerNewsService)
    {
        _hackerNewsService = hackerNewsService;
    }

    /// <summary>
    /// Retrieves the best n stories from Hacker News.
    /// </summary>
    /// <param name="n">The number of top stories to retrieve.</param>
    /// <returns>A list of the best n stories.</returns>
    [HttpGet("beststories")]
    public async Task<IActionResult> GetBestStories([FromQuery] int n = 10)
    {
        if (n <= 0)
        {
            return BadRequest("The number of stories must be greater than 0.");
        }

        var stories = await _hackerNewsService.GetBestStoriesAsync(n);
        var formattedStories = stories.Select(story => new
        {
            title = story.Title,
            uri = story.Url,
            postedBy = story.By,
            time = story.GetPostedTime().ToString("o"), // ISO 8601 format
            score = story.Score,
            commentCount = story.Descendants
        });

        return Ok(formattedStories);
    }
};

