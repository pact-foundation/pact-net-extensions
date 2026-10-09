using System;
using System.Linq;
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
        await app.StartAsync();
        return new AvroProvider(app);
    }

    public ValueTask DisposeAsync() => app.DisposeAsync();
}