# PactNet Plugin Interaction Contributions

## Existing API Boundary

PactNet #551 exposes synchronous-message plugin interactions. gRPC is a transport
plugin, so its request/response contents work through that public API. The Avro
plugin is a content matcher: it does not provide an Avro mock-server transport.

The referenced Avro example creates an **HTTP** interaction, loads the plugin,
and configures `avro/binary` on the response through FFI interaction contents.
Ordinary PactNet HTTP `WithBody` calls set literal bodies; they do not perform
plugin configuration. Treating the schema dictionary as an ordinary HTTP body
or a synchronous message does not reproduce this example.

## HTTP Content Plugin Contribution

A local contribution is prepared in the `pact-net-http-plugins` worktree of the
PactNet repository, based directly on the same #551 commit used for gRPC:
`8b6d35e46998aa3b4b583968942d22f258ce8217`. The original PactNet checkout and its
current work remain unchanged. This API is a prototype, not a published dependency.

The proposed public surface is:

- `IPactV4.WithHttpPluginInteractions(pluginName, pluginVersion, port, host)`.
- `IHttpPluginPactBuilderV4` with interaction creation, verification and disposal.
- Request methods for provider states, HTTP method/path, headers, query parameters,
  and single-shot plugin request contents.
- A response stage for status/headers and terminal, single-shot plugin contents.

```csharp
using var pact = Pact.V4("consumer", "provider", config)
    .WithHttpPluginInteractions("avro", "0.1.3");

pact.UponReceiving("an Avro response")
    .WithRequest(HttpMethod.Get, "/avro")
    .WillRespondWithStatus(200)
    .WithHeader("Content-Type", "avro/binary;record=User")
    .WithContent("avro/binary", new Dictionary<string, object>
    {
        ["pact:avro"] = schemaPath,
        ["pact:record-name"] = "User",
        ["id"] = "matching(integer, 1)",
        ["username"] = "matching(equalTo, 'alice')"
    });
```

Internally, it reuses `PactDriver.NewPluginPact` for loading, creates a normal HTTP
interaction, and composes the existing HTTP/plugin interaction drivers. Shared
plugin contents handling accepts an interaction part so request and response are
configured separately. No native handle or internal driver becomes public.
`AbstractPactBuilder` still owns server startup, mismatch checking, log output and
pact writing; the plugin driver owns disposal. Synchronous plugin behavior stays
unchanged.

The contribution includes tests for the correct content part, repeated contents,
invalid ordering/status, headers/query indexes, HTTP interaction creation and
disposal. The external Avro sample provides native round-trip and provider/mismatch
coverage. Before submitting upstream, review null/error paths and failed-content
state handling, mixed ordinary/plugin HTTP contents, multiple-plugin loading, and
whether the new surface should compose existing HTTP builders rather than define
separate stages. Validate all supported platforms, not only the local macOS run.

## Build The Prototype

```shell
PACTNET_HTTP_SOURCE_DIR=../pact-net-http-plugins \
  bash scripts/bootstrap-http-plugin.sh
bash scripts/install-avro-plugin.sh
bash scripts/release.sh pack-local avro
bash scripts/test-publish.sh avro
```

The source checkout must contain the HTTP contribution and descend from the pinned
#551 commit. Packages use `5.0.0-plugin.http.local`, separate from the gRPC bootstrap.
No patch files, native driver implementation, or source copies are carried in the
extension repository. The contribution is in PactNet itself.

`extensions.json` marks Avro `localOnly` because a clean remote CI runner cannot
fetch this unpublished prerequisite. `INCLUDE_LOCAL_EXTENSIONS=true` opts it into
matrix discovery only when the contribution checkout is supplied. Its entry chooses
the HTTP bootstrap and Avro installer. With act, mount that checkout into the job
container and set `PACTNET_HTTP_SOURCE_DIR` to its container path before opting in.
The existing release guard rejects this local dependency even when a tag matches.

Once the generic API is merged and published, update `AvroPactNetVersion` to the
compatible release, remove `localOnly` and the prototype bootstrap entry, and run
the full CI/publish gates before releasing `avro/v<version>`.

## Asynchronous Plugin Messages

The Avro plugin's own primary example uses asynchronous messages, with transport
abstracted away. That is another interaction type and is not implemented by this
HTTP contribution or the synchronous plugin API.

A follow-up PactNet contribution needs:

1. A public asynchronous plugin pact/message builder that loads plugins and creates
   native asynchronous message interactions, not HTTP or synchronous messages.
2. A single terminal contents call using each plugin's configuration schema.
3. A consumer callback exposing generated **binary** contents plus content-type and
   message metadata; JSON-only reification is insufficient for Avro decoding.
4. Native message reification, matching/generation, log/error handling and cleanup
   kept inside PactNet, with provider verification through its existing verifier.
5. Tests for Avro byte decoding, matchers, nullable fields/defaults, provider states,
   metadata, failed callbacks, multiple messages and plugin-resource disposal.

Such a wrapper must not simulate a broker, disguise messages as HTTP interactions,
or add a second FFI lifecycle in this repo. Use the Avro plugin's asynchronous
consumer/provider examples to validate that contribution independently of the
working HTTP example here.