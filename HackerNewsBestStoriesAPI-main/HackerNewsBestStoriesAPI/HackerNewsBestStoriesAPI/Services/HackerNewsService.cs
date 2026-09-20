using HackerNewsBestStories.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http;
using System.Text.Json;

namespace HackerNewsBestStories.Services
{
    public class HackerNewsService : IHackerNewsService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IMemoryCache _cache;
        private readonly ILogger<HackerNewsService> _logger;
        private readonly HackerNewsApiConfig _apiConfig;

        public HackerNewsService(
            IHttpClientFactory httpClientFactory,
            IMemoryCache cache,
            ILogger<HackerNewsService> logger,
            IOptions<HackerNewsApiConfig> apiConfig)
        {
            _httpClientFactory = httpClientFactory;
            _cache = cache;
            _logger = logger;
            _apiConfig = apiConfig.Value;
        }

        public async Task<List<Story>> GetBestStoriesAsync(int n)
        {
            try
            {
                if (_cache.TryGetValue("BestStories", out List<Story> cachedStories))
                {
                    return cachedStories.Take(n).ToList();
                }

                var client = _httpClientFactory.CreateClient();
                var bestStoriesResponse = await client.GetAsync($"{_apiConfig.BaseUrl}{_apiConfig.BestStoriesEndpoint}");

                if (!bestStoriesResponse.IsSuccessStatusCode)
                {
                    _logger.LogError("Failed to fetch best stories. Status code: {StatusCode}", bestStoriesResponse.StatusCode);
                    throw new Exception("Failed to fetch best stories.");
                }

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var storyIds = await JsonSerializer.DeserializeAsync<int[]>(await bestStoriesResponse.Content.ReadAsStreamAsync(), options);

                if (storyIds == null || storyIds.Length == 0)
                {
                    return new List<Story>();
                }

                var tasks = storyIds.Take(n).Select(async id =>
                {
                    var storyResponse = await client.GetAsync(string.Format($"{_apiConfig.BaseUrl}{_apiConfig.ItemEndpoint}", id));
                    if (storyResponse.IsSuccessStatusCode)
                    {
                        return await JsonSerializer.DeserializeAsync<Story>(await storyResponse.Content.ReadAsStreamAsync(), options);
                    }
                    return null;
                });

                var stories = await Task.WhenAll(tasks);
                var sortedStories = stories
                    .Where(story => story != null)
                    .OrderByDescending(story => story.Score)
                    .Take(n)
                    .ToList();

                _cache.Set("BestStories", sortedStories, TimeSpan.FromMinutes(5));
                return sortedStories;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching best stories.");
                throw;
            }
        }
    }
}