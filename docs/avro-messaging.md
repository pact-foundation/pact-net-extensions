# Avro Messaging

## Supported Modes

The hominux Avro plugin 0.1.3 is a content matcher, not a broker or transport
server. The local PactNet contribution supports binary Avro HTTP bodies and
**asynchronous messages**. It does not yet offer a working Avro synchronous
request/response messaging contract.

Async tests abstract Kafka, RabbitMQ or another broker away. They exercise your
real record handler with a generated payload and metadata; they do not prove
broker delivery, acknowledgement, ordering or connection behavior.

## Asynchronous Example

```csharp
using var pact = Pact.V4("AvroMessageConsumer", "AvroMessageProvider", config)
    .WithAvroMessages(pluginVersion: "0.1.3");

await pact.ExpectsToReceive("an Avro user message")
    .WithMetadata("topic", "users")
    .WithAvroContent(schemaPath, "User", new Dictionary<string, object>
    {
        ["id"] = "matching(integer, 1)",
        ["username"] = "matching(equalTo, 'alice')"
    })
    .VerifyAsync(message => HandleUserAsync(message.Contents, message.Metadata));
```

`Contents` is a `byte[]` ready for Apache Avro's binary decoder, not an Avro record
serialized to JSON. `ContentType` carries the record-qualified media type.
Metadata values are cloned `JsonElement` objects, preserving their JSON types.
The handler is your application code. A successful callback writes a
`Asynchronous/Messages` pact; a failed callback throws and does not write it.
Dispose the pact to release the plugin resources.

The [native example](../samples/Avro/AvroSample.Tests/AvroMessageTests.cs) decodes
the binary record, checks metadata and pact type, verifies a producer through an
HTTP message adapter, rejects a mismatching producer, and checks failed-consumer
behavior. That adapter supplies producer messages to the verifier; it does not
turn the contract into an HTTP pact.

```shell
bash scripts/bootstrap-http-plugin.sh
bash scripts/install-avro-plugin.sh
bash scripts/release.sh pack-local avro
bash scripts/test-publish.sh avro
```

The existing sibling contribution worktree now includes both HTTP and async APIs.
Bootstrap packages use `5.0.0-plugin.messages.1.local` to distinguish them from
the earlier HTTP-only prototype. Avro remains local-only until the public APIs
are contributed and published.

## PactNet API Changes

- `WithAsynchronousPluginInteractions` loads a content plugin and creates native
  async message interactions without starting a mock transport server.
- Single-shot `WithContent` returns a configured verifier; provider states and
  user metadata are configured before it.
- `PluginMessage` exposes binary contents, media type and metadata to synchronous
  or asynchronous consumer callbacks.
- PactNet decodes the native V4 base64 envelope, copies metadata, frees the reified
  native string, writes the pact on success, and owns plugin disposal.
- User metadata is applied after plugin configuration, which otherwise replaces
  it. Unit and native tests cover this ordering.

Before upstream merge, broaden coverage for multiple messages, nullable/default
fields, generators and all supported platforms, and review the public metadata
types and failed-configuration lifecycle. Existing JSON message APIs stay intact.

## Synchronous Messaging

With FFI 0.4.27 and Avro plugin 0.1.3, configuring one Avro record on a sync
interaction produced:

```json
{
  "type": "Synchronous/Messages",
  "request": {},
  "response": []
}
```

The plugin returns a single unlabelled interaction rather than parts labelled
`request` and `response`. Merely using `WithSynchronousPluginInteractions`, or
specifying a transport called `avro`, does not provide a working sync example.

To reproduce the opt-in native capability check from this repository root:

```shell
AVRO_SCHEMA_PATH="$PWD/samples/Avro/Schemas/user.avsc" \
PACT_PLUGIN_DIR="$PWD/artifacts/plugins" \
dotnet test ../pact-net-http-plugins/tests/PactNet.Tests/PactNet.Tests.csproj \
  --framework net8.0 \
  --filter FullyQualifiedName~AvroSynchronousCapabilityTests \
  --logger 'console;verbosity=detailed'
```

It asserts the current limitation, not successful sync consumption, and is skipped
when the explicit schema environment variable is absent.

Working sync messaging needs two contributions:

1. **Avro plugin:** accept distinct schema/record/field configurations for a request
   and one or more responses; emit correctly labelled parts and preserve each
   side's schema/matchers. Keep the existing single-record async API compatible.
2. **PactNet:** add transport-free sync binary reification and a callback that
   passes the generated request to the real consumer/client, collects its responses,
   and checks them with native plugin matching before writing the pact. Preserve
   metadata, provider states, response counts, generators and cleanup.

Add native positive and negative consumer/provider examples only after both sides
work. The existing transport-based sync builder remains appropriate for gRPC;
implementing a pretend HTTP/broker transport for Avro would not close this gap.