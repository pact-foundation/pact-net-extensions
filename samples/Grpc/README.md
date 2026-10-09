# gRPC Greeter Sample

This sample includes a protobuf service, a real .NET gRPC client, a consumer pact
test, and provider verification against an in-process ASP.NET Core gRPC service.

From the repository root:

```shell
bash scripts/bootstrap-pactnet.sh
bash scripts/install-grpc-plugin.sh
bash scripts/release.sh test grpc
```

The command runs unit tests, consumer tests, then provider tests in order. It sets
`PACT_PLUGIN_DIR` to the isolated plugin directory and `PACT_GRPC_PACT_FILE` to
the pact generated under `artifacts/pacts/`. The provider fixture starts and stops
its own server on port 5000, which must be available. No separately running server
is needed for the test command.

The consumer includes a negative test for an unexercised request. The provider
includes a negative test for an unavailable endpoint and a successful verification.
Plugin version `0.8.0` is explicit in the consumer. gRPC packages use `2.84.0` to
include Linux ARM64 fixes discussed in PactNet #551.

To run consumer tests alone, set `PACT_PLUGIN_DIR` and use `dotnet test` on
`GrpcGreeterClient.Tests/GrpcGreeterClient.Tests.csproj`. To verify the newly
generated pact, also set `PACT_GRPC_PACT_FILE` to its absolute path before running
`GrpcGreeter.Tests/GrpcGreeter.Tests.csproj`. Without that variable the provider
tests use the committed example under `pacts/`.

The committed fixture was generated with protobuf 0.8.0 and FFI 0.4.27. Refresh it
only by copying a successfully generated and verified artifact, not by editing its
embedded protobuf descriptors or plugin version. Do not merge new fixtures into
old-version fixtures: stale plugin metadata can survive PactNet's merge.