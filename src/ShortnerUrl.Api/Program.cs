using Dapper;
using Microsoft.Extensions.Caching.Distributed;
using Npgsql;
using ShortenerUrl.Api.Extension;
using System.Data;

var builder = WebApplication.CreateBuilder(args);

var key = "url-";

var cacheOption = new DistributedCacheEntryOptions() { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10) };

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();

ArgumentNullException.ThrowIfNullOrWhiteSpace(builder.Configuration.GetConnectionString("Redis"), "RedisConnectionstring");
ArgumentNullException.ThrowIfNullOrWhiteSpace(builder.Configuration.GetConnectionString("Postgre"), "PostgreConnectionstring");

builder.Services.AddTransient<IDbConnection>((sp) => new NpgsqlConnection(builder.Configuration.GetConnectionString("Postgre")));

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapPost("/api/url", async (CreateUrlRequest param, HttpContext context, IDbConnection _connection, IDistributedCache _cache, CancellationToken cancellationToken) =>
{
    if (!(Uri.TryCreate(param.url, UriKind.Absolute, out var uriResult) && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps)))
        return Results.BadRequest("Invalid Url");

    var exist = "SELECT Code FROM Url WHERE OriginalUrl = @OriginalUrl";

    var code = await _connection.ExecuteScalarAsync<string>(exist, new { OriginalUrl = param.url });

    if (!string.IsNullOrWhiteSpace(code))
    {
        await _cache.SetStringAsync($"{key}{code}", param.url, cacheOption, cancellationToken);

        return Results.CreatedAtRoute("get-url", new { code }, code);
    }

    var extension = new StringExtension();

    code = extension.BuildUniqueCode();

    while (true)
    {
        var result = await _connection.ExecuteScalarAsync<int>(@"SELECT 1 FROM Url WHERE Code = @code", new { code });

        if(result != 1)
        {
            var sql = @"
                INSERT INTO Url (Id, Code, OriginalUrl, CreatedAt) 
                VALUES (@Id, @Code, @OriginalUrl, @CreatedAt)";

            var newUrl = new
            {
                Id = Guid.NewGuid(),
                code,
                OriginalUrl = param.url,
                CreatedAt = DateTime.Now
            };
            
            await _connection.ExecuteAsync(sql, newUrl);

            await _cache.SetStringAsync($"{key}{newUrl.code}", param.url, cacheOption, cancellationToken);

            return Results.CreatedAtRoute("get-url", new { code }, code);
        }

        code = extension.BuildUniqueCode();
    }
})
    .WithName("post-url")
    .WithOpenApi();

app.MapGet("/api/url/{code}", async (string code, IDbConnection _connection, IDistributedCache _cache, CancellationToken cancellationToken) =>
{
    var cache = await _cache.GetStringAsync($"{key}{code}");

    if(!string.IsNullOrWhiteSpace(cache))
        return Results.Redirect(cache);


    var sql = "SELECT OriginalUrl FROM Url WHERE Code = @Code";

    var url = await _connection.ExecuteScalarAsync<string>(sql, new { code });

    if (url == null)
        return Results.NotFound();

    return Results.Redirect(url);

})
    .WithName("get-url")
    .WithOpenApi();

app.MapHealthChecks("/api/health");

app.Run();

internal record CreateUrlRequest(string url);