using HackerNewsBestStories.Models;

namespace HackerNewsBestStories.Services
{
    public interface IHackerNewsService
    {
        Task<List<Story>> GetBestStoriesAsync(int n);
    }
}