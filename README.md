# PactNet Extensions

Independently versioned .NET wrappers for the [Pact plugin framework](https://github.com/pact-foundation/pact-plugins).
The first wrapper is `PactNet.Extensions.Grpc`, for unary gRPC consumer contract tests
using the protobuf plugin. This repository contains wrappers, not plugin drivers
or native plugins. PactNet owns mock servers, verification, logging, pact writing,
and native resource cleanup.

## Development Status

The wrapper requires the synchronous plugin API from pending
[PactNet #551](https://github.com/pact-foundation/pact-net/pull/551).
Local builds use a pinned checkout to produce development packages. A compatible
published PactNet dependency is required before releasing the wrapper; publishing
is blocked during bootstrap.

## Local Setup

Prerequisites: .NET SDK 8.0.4xx, Git, Bash, jq, curl, gzip, tar, and SHA-256
tooling. Windows commands use Git Bash, with jq on PATH. Linux ARM64 may also require
`protoc` on `PATH`; CI installs `protobuf-compiler`. Bootstrap downloads the
upstream FFI 0.4.27 libraries and verifies their checksums.

```shell
bash scripts/bootstrap-pactnet.sh
bash scripts/install-grpc-plugin.sh
bash scripts/test-release.sh
bash scripts/release.sh test grpc
bash scripts/release.sh pack-local grpc
```

All downloaded tools, upstream checkouts, local packages, and generated pacts live
under ignored `artifacts/`. The bootstrap does not modify PactNet source or publish
anything. An existing checkout at the pinned commit can be supplied with
`PACTNET_SOURCE_DIR`. Native plugins are installed only in `artifacts/plugins`,
not your home directory.

The local `.nupkg` and `.snupkg` are in `artifacts/release/grpc/` and depend on
`5.0.0-plugin.551.local`. They are development artifacts, not publishable releases.

## gRPC Usage

```csharp
using PactNet;
using PactNet.Extensions.Grpc;

using var pact = Pact.V4("Greeter Client", "Greeter Service", new PactConfig())
    .WithGrpcInteractions(pluginVersion: "0.8.0");

pact.UponReceiving("a greeting")
    .Given("the greeter is available")
    .WithRequest(protoPath, "Greeter", "SayHello",
        new { name = "matching(equalTo, 'foo')" })
    .WillRespond()
    .WithBody(new { message = "matching(equalTo, 'Hello foo')" });

await pact.VerifyAsync(async context =>
{
    var client = new GreeterClientWrapper(context.MockServerUri.AbsoluteUri);
    var greeting = await client.SayHello("foo");
    Assert.Equal("Hello foo", greeting);
});
```

`protoPath` is an absolute path to your service definition. The client in this
example is in the [Greeter sample](samples/Grpc/README.md). `WithBody` completes
the interaction; use a new `UponReceiving` for another interaction.

Omit `pluginVersion` to delegate version selection to the plugin framework. This
does not guarantee a specific version or an offline installation: install the
plugin and pin its version in reproducible CI. The wrapper version and native
plugin version are independent. Provider verification uses the normal PactNet
`PactVerifier`, not a separate verifier in the extension.

Only synchronous unary interactions are covered here. Async messaging and gRPC
streaming are not provided by this wrapper. The protobuf mock server tracks calls
by service method, so tests with multiple interactions on one method must explicitly
exercise each request rather than relying solely on verification success.

## Contributing And Releases

- [Developer testing](docs/developer-testing.md): native tests, act, and local NuGet publication.
- [Wrapper-author workshop](docs/writing-a-wrapper.md): raw API, thin fluent wrapper, tests, and registration.
- [Independent releases](docs/releases.md): package-specific versions, tags, gates, and adding wrappers.

The design follows [Adam Rodger's success criteria](https://github.com/pact-foundation/pact-net/pull/548#issuecomment-3315100623):
external wrappers without changes to PactNet internals, a raw API when no wrapper
exists, and documentation plus executable samples for wrapper authors.