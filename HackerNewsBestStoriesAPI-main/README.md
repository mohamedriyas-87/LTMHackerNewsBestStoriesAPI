# HackerNewsBestStoriesAPI

## Description
HackerNewsBestStoriesAPI is a RESTful API built with ASP.NET Core that retrieves the top `n` best stories from the Hacker News API, sorted by score in descending order. The API is designed to efficiently handle large numbers of requests while minimizing the load on the Hacker News API through caching and rate limiting.

## Features
- Fetch the top `n` best stories from Hacker News.
- Stories are sorted by score in descending order.
- Caching to reduce redundant API calls.
- Rate limiting to prevent overloading the Hacker News API.
- Unit tests for core functionality.

## API Endpoints
### Get Best Stories
**GET** `/api/hackernews/beststories?n={number_of_stories}`

#### Query Parameters:
- `n` (optional): The number of top stories to retrieve. Defaults to 10.

#### Response:
