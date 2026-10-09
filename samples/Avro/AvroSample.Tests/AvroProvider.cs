using System;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace AvroSample.Tests;

internal sealed class AvroProvider(WebApplication app) : IAsyncDisposable
{
    internal Uri Address => new(app.Urls.Single());

    internal static async Task<AvroProvider> StartAsync(string username)
    {
        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        var app = builder.Build();
        app.MapGet("/avro", () => Results.Bytes(AvroCodec.Encode(1, username), "avro/binary;record=User"));
        app.MapPost("/messages", (HttpContext context) =>
        {
            context.Response.Headers["Pact-Message-Metadata"] = Convert.ToBase64String(Encoding.UTF8.GetBytes(
                JsonSerializer.Serialize(new { topic = "users", contentType = "avro/binary;record=User" })));
            return Results.Bytes(AvroCodec.Encode(1, username), "avro/binary;record=User");
        });
        await app.StartAsync();
        return new AvroProvider(app);
    }

    public ValueTask DisposeAsync() => app.DisposeAsync();
}