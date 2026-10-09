# Avro HTTP Example

This example is based on [YOU54F's Avro sample](https://github.com/YOU54F/pact-dotnet-ffi-plugins/tree/main/Avro)
and uses the current [hominux Avro plugin](https://github.com/hominux/pact-avro-plugin).
It keeps the original `User` schema (`id` and `username`) and HTTP `/avro` flow,
but uses Apache.Avro 1.12.0 and plugin 0.1.3 instead of the old direct-FFI example.

## Run Locally

The pinned PactNet #551 API does not expose HTTP plugin contents. Build the
public HTTP plugin contribution from a local PactNet checkout first; see
[the contribution guide](../../docs/http-plugin-contribution.md). The default
checkout location is the sibling `pact-net-http-plugins` worktree:

```shell
bash scripts/bootstrap-http-plugin.sh
bash scripts/install-avro-plugin.sh
bash scripts/release.sh pack-local avro
bash scripts/test-publish.sh avro
```

Set `PACTNET_HTTP_SOURCE_DIR` if the contribution checkout is elsewhere. The
bootstrap builds separate `5.0.0-plugin.http.local` packages; it does not replace
the gRPC prerequisite packages or expose native handles to this extension.

Six wrapper unit tests and three executable sample tests run. They cover a GET
returning a real binary Avro record, POST request/response matching, an unused
request failure, successful provider verification, and rejection of a mismatching
provider. The provider uses an ephemeral HTTP port and stops automatically.
Generated pacts are under `artifacts/avro-pacts/`.

## Wrapper Usage

```csharp
using var pact = Pact.V4("AvroConsumer", "AvroProvider", config)
    .WithAvroInteractions(pluginVersion: "0.1.3");

pact.UponReceiving("a request for a user")
    .WithRequest(HttpMethod.Get, "/avro")
    .WillRespondWithStatus(200)
    .WithHeader("Content-Type", "avro/binary;record=User")
    .WithAvroBody(schemaPath, "User", new Dictionary<string, object>
    {
        ["id"] = "matching(integer, 1)",
        ["username"] = "matching(equalTo, 'alice')"
    });
```

Use `PactNet.Extensions.Avro`, `System.Net.Http`, and `System.Collections.Generic`.
`WithAvroBody` on the response is terminal; request contents are configured before
`WillRespondWithStatus`. `VerifyAsync` executes your real client against PactNet's
HTTP mock server. Provider verification uses the normal `PactVerifier`.

The wrapper normalizes schema paths, preserves field matchers and rejects reserved
`pact:` field keys. Native plugin version selection is optional; omit it to delegate
selection to the framework, or pin it for repeatable CI.

This is an **HTTP content-plugin** example, not an asynchronous Avro message
consumer. It is registered with `localOnly: true` until the public HTTP API is
available upstream; normal CI discovery excludes it. Development packages are
not releasable. [The contribution guide](../../docs/http-plugin-contribution.md)
describes the missing async interaction API separately.