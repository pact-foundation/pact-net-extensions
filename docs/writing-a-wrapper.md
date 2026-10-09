# Workshop: Write A PactNet Plugin Wrapper

## Goal And Boundary

Build a friendly protocol-specific API outside PactNet, with no `InternalsVisibleTo`
from PactNet and no direct FFI calls. This workshop uses the executable gRPC sample
as the reference implementation. It requires the public API from PR #551; see the
root README for its pinned bootstrap and publication restriction.

PactNet and its native plugin driver own server startup, matching, logs, pact
writing, plugin discovery, and disposal. A .NET wrapper translates domain inputs
into a plugin contents dictionary. Writing the actual native plugin is a different
task; see the [plugin-writing guide](https://github.com/pact-foundation/pact-plugins/blob/main/docs/writing-plugin-guide.md).

## 1. Run The Reference

Run the setup and `bash scripts/release.sh test grpc` from the root README.
Inspect the request and response in `artifacts/pacts/grpc-greeter-client-grpc-greeter.json`.
Confirm that the metadata identifies the protobuf plugin and V4 pact specification.
The exercise is complete when the consumer writes a pact and the provider verifies it.

## 2. Use The Raw API First

No wrapper is required to use a new synchronous plugin. Replace the fluent gRPC
interaction in the consumer test with this equivalent raw interaction:

```csharp
using var pact = Pact.V4("Greeter Client", "Greeter Service", config)
    .WithSynchronousPluginInteractions("protobuf", "0.8.0", transport: "grpc");

pact.UponReceiving("a greeting")
    .Given("the greeter is available")
    .WithContent("application/grpc", new Dictionary<string, object>
    {
        ["pact:proto"] = protoPath,
        ["pact:proto-service"] = "Greeter/SayHello",
        ["pact:content-type"] = "application/protobuf",
        ["request"] = new { name = "matching(equalTo, 'foo')" },
        ["response"] = new { message = "matching(equalTo, 'Hello foo')" }
    });
```

Run `VerifyAsync` with the same client callback. For another plugin, take the
content type, transport, and contents schema from that plugin's documentation.
Do not assume protobuf's keys apply to other plugins.

`Given` may repeat; `WithContent` is terminal and returns void. It must be called
once per interaction: another contents call can replace the request and append
responses at the FFI level. Dispose the pact builder even when a test fails.

## 3. Add A Thin Fluent API

Trace the reference implementation in `src/PactNet.Extensions.Grpc/`:

- `GrpcExtensions` selects `protobuf` and `grpc` using the public raw API. The
  optional version is passed through, not hard-coded to a native plugin release.
- `GrpcPactBuilder` holds `ISynchronousPluginPactBuilderV4` and delegates
  `Verify`, `VerifyAsync`, and `Dispose` unchanged.
- `GrpcRequestBuilder` delegates provider states and accumulates plugin contents.
  It rejects a second request and a second response stage.
- `GrpcResponseBuilder.WithBody` completes the dictionary and calls raw
  `WithContent` once. It returns void and rejects reuse.

For a new wrapper, create its own project under `src/PactNet.Extensions.<Name>/`,
reference a compatible **PactNet package**, and import `../NuGet.targets` for
shared packaging metadata. Set its own version and description in its project.
Do not copy PactNet's internal drivers, implement native handles, or inherit its
internal `AbstractPactBuilder`. Avoid depending on another extension unless the
protocol genuinely requires it.

## 4. Prove The Boundary

Use the existing xUnit/Moq conventions under `tests/`. Mock the public synchronous
builder interfaces, not static extension methods. Assert dictionary contents,
provider-state delegation, verification/disposal delegation, terminal completion,
and rejection of invalid ordering or repeated completion.

Add an executable consumer/provider sample with the native plugin installed in
both environments. Exercise a mismatch and a successful interaction. Unit mocks
alone cannot validate native transport, plugin installation, or generated pact
compatibility. Streaming and async interactions need their own upstream support
and tests; do not infer them from a unary sample.

For an experiment, change the expected greeting so consumer verification fails,
then restore it. Next change the provider's greeting and confirm provider
verification fails. When multiple interactions use one gRPC method, explicitly
call each distinct request: the mock server's method-level call tracking is not
proof that all such interactions ran.

## 5. Register And Document

Add a slug, package project, ordered test projects, optional plugin installer,
and sample environment paths to `extensions.json`. CI discovers this registry;
release routing selects one slug. Add package and sample READMEs documenting
installation, supported interaction modes, version selection, and limitations.
Follow [the release guide](releases.md) for independent package tags.

Completion criteria: a clean checkout can build a separate wrapper using public
PactNet packages, raw consumers work without it, the sample verifies end to end,
and the wrapper has an independently selectable version and release. While #551
is unreleased, the pinned local packages prove the public assembly boundary but
do not satisfy the published-package release prerequisite.