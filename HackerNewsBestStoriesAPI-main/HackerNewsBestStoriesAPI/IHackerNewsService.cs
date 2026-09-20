using HackerNewsBestStoriesAPI.Models;

namespace HackerNewsBestStoriesAPI.Services
{
    public interface IHackerNewsService
    {
        Task<List<Story>> GetBestStoriesAsync(int n);
    }
}