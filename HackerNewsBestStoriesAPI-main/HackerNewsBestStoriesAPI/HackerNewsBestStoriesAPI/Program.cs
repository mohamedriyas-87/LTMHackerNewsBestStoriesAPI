using HackerNewsBestStories.Models;
using HackerNewsBestStories.Services;
using AspNetCoreRateLimit;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.Configure<HackerNewsApiConfig>(builder.Configuration.GetSection("HackerNewsApi"));

builder.Services.AddControllers();
builder.Services.AddHttpClient(); // Register IHttpClientFactory
builder.Services.AddMemoryCache(); // Register IMemoryCache
builder.Services.AddScoped<IHackerNewsService, HackerNewsService>(); // Register the service

// Add IP rate limiting services
builder.Services.AddOptions();
builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
builder.Services.Configure<IpRateLimitPolicies>(builder.Configuration.GetSection("IpRateLimitPolicies"));
builder.Services.AddInMemoryRateLimiting();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Add IP rate limiting middleware
app.UseIpRateLimiting();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();