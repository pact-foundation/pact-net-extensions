using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using PactNet;
using PactNet.Exceptions;
using PactNet.Extensions.Avro;
using PactNet.Verifier;
using Xunit;

namespace AvroSample.Tests;

public class AvroContractTests
{
    private static Dictionary<string, object> UserFields() => new()
    {
        ["id"] = "matching(integer, 1)",
        ["username"] = "matching(equalTo, 'alice')"
    };

    private static string PactDirectory => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,
        "../../../../../../artifacts/avro-pacts"));

    private static IHttpPluginPactBuilderV4 CreatePact(string consumer)
        => Pact.V4(consumer, "AvroProvider", new PactConfig { PactDir = PactDirectory })
            .WithAvroInteractions(pluginVersion: "0.1.3");

    [Fact]
    public async Task ConsumerAndProvider_VerifyAnAvroUserRecord()
    {
        using var pact = CreatePact("AvroConsumer");
        pact.UponReceiving("a request for an Avro user")
            .WithRequest(HttpMethod.Get, "/avro")
            .WillRespondWithStatus(200)
            .WithHeader("Content-Type", "avro/binary;record=User")
            .WithAvroBody(AvroCodec.SchemaPath, "User", UserFields());

        await pact.VerifyAsync(async context =>
        {
            using var client = new HttpClient { BaseAddress = context.MockServerUri };
            var record = AvroCodec.Decode(await client.GetByteArrayAsync("/avro"));
            Assert.Equal(1L, record["id"]);
            Assert.Equal("alice", record["username"]);
        });

        var pactFile = new FileInfo(Path.Combine(PactDirectory, "AvroConsumer-AvroProvider.json"));
        await using var provider = await AvroProvider.StartAsync("alice");
        using (var verifier = new PactVerifier("AvroProvider", new PactVerifierConfig()))
        {
            verifier.WithHttpEndpoint(provider.Address).WithFileSource(pactFile).Verify();
        }

        await using var mismatchingProvider = await AvroProvider.StartAsync("bob");
        using var mismatchVerifier = new PactVerifier("AvroProvider", new PactVerifierConfig());
        Assert.Throws<PactVerificationFailedException>(() => mismatchVerifier
            .WithHttpEndpoint(mismatchingProvider.Address).WithFileSource(pactFile).Verify());
    }

    [Fact]
    public void Consumer_RejectsAnUnexercisedRequest()
    {
        using var pact = CreatePact("AvroUnusedConsumer");
        pact.UponReceiving("an unused Avro request").WithRequest(HttpMethod.Get, "/avro")
            .WillRespondWithStatus(200).WithAvroBody(AvroCodec.SchemaPath, "User", UserFields());
        Assert.Throws<PactFailureException>(() => pact.Verify(_ => { }));
    }

    [Fact]
    public async Task RequestAndResponse_UseRealAvroBinary()
    {
        using var pact = CreatePact("AvroPostConsumer");
        pact.UponReceiving("an Avro user upload")
            .WithRequest(HttpMethod.Post, "/avro")
            .WithHeader("Content-Type", "avro/binary;record=User")
            .WithAvroBody(AvroCodec.SchemaPath, "User", UserFields())
            .WillRespondWithStatus(201)
            .WithHeader("Content-Type", "avro/binary;record=User")
            .WithAvroBody(AvroCodec.SchemaPath, "User", UserFields());

        await pact.VerifyAsync(async context =>
        {
            using var client = new HttpClient { BaseAddress = context.MockServerUri };
            using var body = new ByteArrayContent(AvroCodec.Encode(1, "alice"));
            body.Headers.ContentType = MediaTypeHeaderValue.Parse("avro/binary;record=User");
            using var response = await client.PostAsync("/avro", body);
            response.EnsureSuccessStatusCode();
            Assert.Equal("alice", AvroCodec.Decode(await response.Content.ReadAsByteArrayAsync())["username"]);
        });
    }
}