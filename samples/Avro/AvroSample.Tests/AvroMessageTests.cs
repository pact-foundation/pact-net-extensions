using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Linq;
using PactNet;
using PactNet.Exceptions;
using PactNet.Extensions.Avro;
using PactNet.Output.Xunit;
using PactNet.Verifier;
using Xunit;
using Xunit.Abstractions;

namespace AvroSample.Tests;

public class AvroMessageTests(ITestOutputHelper output)
{
    private static string PactDirectory => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,
        "../../../../../../artifacts/avro-message-pacts"));

    private static IAsynchronousPluginPactBuilderV4 CreatePact(string consumer)
        => Pact.V4(consumer, "AvroMessageProvider", new PactConfig { PactDir = PactDirectory })
            .WithAvroMessages(pluginVersion: "0.1.3");

    private static IConfiguredPluginMessageVerifier Configure(IAsynchronousPluginPactBuilderV4 pact)
        => pact.ExpectsToReceive("an Avro user message")
            .WithMetadata("topic", "users")
            .WithAvroContent(AvroCodec.SchemaPath, "User", new Dictionary<string, object>
            {
                ["id"] = "matching(integer, 1)",
                ["username"] = "matching(equalTo, 'alice')"
            });

    private void WritePluginLogsOnFailure()
    {
        if (Environment.GetEnvironmentVariable("PACT_DUMP_PLUGIN_LOGS") != "1")
            return;

        string outputDirectory = Environment.GetEnvironmentVariable("PACT_OUTPUT_DIR");
        string logDirectory = outputDirectory != null
            ? Path.Combine(outputDirectory, "logs")
            : Path.Combine(Environment.GetEnvironmentVariable("PACT_PLUGIN_DIR")
                ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".pact", "plugins"), "logs");
        if (!Directory.Exists(logDirectory))
        {
            output.WriteLine($"Avro plugin log directory was not found: {logDirectory}");
            return;
        }

        foreach (string logFile in Directory.GetFiles(logDirectory, "pact-plugin-avro-*.log").Order())
        {
            output.WriteLine($"Avro plugin log: {logFile}");
            output.WriteLine(File.ReadAllText(logFile));
        }
    }

    [Fact]
    public async Task AsyncConsumerAndProvider_VerifyBinaryContentsAndMetadata()
    {
        FileInfo pactFile;
        using (var pact = CreatePact("AvroMessageConsumer"))
        {
            await Configure(pact).VerifyAsync(message =>
            {
                var record = AvroCodec.Decode(message.Contents);
                Assert.Equal("alice", record["username"]);
                Assert.Equal(1L, record["id"]);
                Assert.Equal("avro/binary;record=User", message.ContentType);
                Assert.Equal("users", ((JsonElement)message.Metadata["topic"]).GetString());
                return Task.CompletedTask;
            });

            pactFile = new FileInfo(Path.Combine(PactDirectory, "AvroMessageConsumer-AvroMessageProvider.json"));
            using var document = JsonDocument.Parse(await File.ReadAllTextAsync(pactFile.FullName));
            Assert.Equal("Asynchronous/Messages", document.RootElement.GetProperty("interactions")[0].GetProperty("type").GetString());
        }

        await using var provider = await AvroProvider.StartAsync("alice");
        output.WriteLine($"Verifying Avro message provider at {new Uri(provider.Address, "/messages")}");
        try
        {
            using var verifier = new PactVerifier("AvroMessageProvider", new PactVerifierConfig
            {
                LogLevel = PactLogLevel.Debug,
                Outputters = new[] { new XunitOutput(output) }
            });
            verifier.WithHttpEndpoint(new Uri(provider.Address, "/messages")).WithFileSource(pactFile).Verify();
        }
        catch
        {
            this.WritePluginLogsOnFailure();
            throw;
        }
        await using var mismatch = await AvroProvider.StartAsync("bob");
        using var mismatchVerifier = new PactVerifier("AvroMessageProvider", new PactVerifierConfig
        {
            LogLevel = PactLogLevel.Debug,
            Outputters = new[] { new XunitOutput(output) }
        });
        Assert.Throws<PactVerificationFailedException>(() => mismatchVerifier
            .WithHttpEndpoint(new Uri(mismatch.Address, "/messages")).WithFileSource(pactFile).Verify());
    }

    [Fact]
    public void FailedConsumer_DoesNotWriteAnAsyncPact()
    {
        using var pact = CreatePact("AvroRejectedMessageConsumer");
        Assert.Throws<PactMessageConsumerVerificationException>(() => Configure(pact)
            .Verify(_ => throw new InvalidOperationException("consumer rejected the message")));
        Assert.False(File.Exists(Path.Combine(PactDirectory, "AvroRejectedMessageConsumer-AvroMessageProvider.json")));
    }
}