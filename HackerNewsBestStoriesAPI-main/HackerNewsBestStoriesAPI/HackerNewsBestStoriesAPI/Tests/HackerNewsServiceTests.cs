using HackerNewsBestStories.Models;
using HackerNewsBestStories.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

public class HackerNewsServiceTests
{
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly IMemoryCache _cache;
    private readonly Mock<ILogger<HackerNewsService>> _loggerMock;
    private readonly Mock<IOptions<HackerNewsApiConfig>> _apiConfigMock;
    private readonly HackerNewsService _service;

    public HackerNewsServiceTests()
    {
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _cache = new MemoryCache(new MemoryCacheOptions());
        _loggerMock = new Mock<ILogger<HackerNewsService>>();
        _apiConfigMock = new Mock<IOptions<HackerNewsApiConfig>>();

        // Setup the configuration
        _apiConfigMock.Setup(config => config.Value).Returns(new HackerNewsApiConfig
        {
            BaseUrl = "https://hacker-news.firebaseio.com/v0",
            BestStoriesEndpoint = "/beststories.json",
            ItemEndpoint = "/item/{0}.json"
        });

        _service = new HackerNewsService(
            _httpClientFactoryMock.Object,
            _cache,
            _loggerMock.Object,
            _apiConfigMock.Object
        );
    }

    [Fact]
    public async Task GetBestStoriesAsync_ReturnsStories()
    {
        // Arrange
        var storyIds = new[] { 1, 2 };
        var stories = new List<Story>
        {
            new Story { Title = "Story 1", Url = "http://example.com/1", By = "Author1", Time = 1234567890, Score = 100, Descendants = 10 },
            new Story { Title = "Story 2", Url = "http://example.com/2", By = "Author2", Time = 1234567891, Score = 90, Descendants = 8 }
        };

        // Mock the HttpClient responses
        var httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        httpMessageHandlerMock
            .Protected()
            .SetupSequence<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(storyIds))
            })
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(stories[0]))
            })
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(stories[1]))
            });

        var httpClient = new HttpClient(httpMessageHandlerMock.Object);
        _httpClientFactoryMock.Setup(factory => factory.CreateClient(It.IsAny<string>())).Returns(httpClient);

        // Act
        var result = await _service.GetBestStoriesAsync(2);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal("Story 1", result[0].Title);
        Assert.Equal("Story 2", result[1].Title);
        Assert.Equal(100, result[0].Score);
        Assert.Equal(90, result[1].Score);
    }

    [Fact]
    public async Task GetBestStoriesAsync_ReturnsEmptyList_WhenNoStories()
    {
        // Arrange
        var storyIds = new int[0]; // No story IDs

        var httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(storyIds))
            });

        var httpClient = new HttpClient(httpMessageHandlerMock.Object);
        _httpClientFactoryMock.Setup(factory => factory.CreateClient(It.IsAny<string>())).Returns(httpClient);

        // Act
        var result = await _service.GetBestStoriesAsync(5);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetBestStoriesAsync_ThrowsException_OnApiFailure()
    {
        // Arrange
        var httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.InternalServerError
            });

        var httpClient = new HttpClient(httpMessageHandlerMock.Object);
        _httpClientFactoryMock.Setup(factory => factory.CreateClient(It.IsAny<string>())).Returns(httpClient);

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _service.GetBestStoriesAsync(5));
    }

    [Fact]
    public async Task GetBestStoriesAsync_ReturnsFromCache_WhenAvailable()
    {
        // Arrange
        var cachedStories = new List<Story>
        {
            new Story { Title = "Cached Story", Url = "http://example.com/cached", By = "CachedAuthor", Time = 1234567890, Score = 50, Descendants = 5 }
        };

        _cache.Set("BestStories", cachedStories);

        // Act
        var result = await _service.GetBestStoriesAsync(5);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("Cached Story", result[0].Title);
    }
}