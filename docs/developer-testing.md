# Developer Testing

## Native Tests And Packages

Prerequisites: .NET SDK 8.0.4xx, Bash, jq, Git, curl, gzip, and SHA-256 tooling.
Windows commands use Git Bash with jq on PATH. From the repository root:

```shell
bash scripts/bootstrap-pactnet.sh
bash scripts/install-grpc-plugin.sh
bash scripts/test-release.sh
bash scripts/release.sh pack-local grpc
bash scripts/test-publish.sh grpc
```

The gRPC command runs 14 unit tests, two consumer tests, and two provider tests
in order, then creates the wrapper's `.nupkg` and `.snupkg`. The provider fixture
uses port 5000, which must be free. Plugins, generated pacts, bootstrap packages,
and other development output stay under ignored `artifacts/`.

## Avro HTTP And Async Prototype

The Avro wrapper uses the PactNet plugin API described in
[the contribution guide](http-plugin-contribution.md). The bootstrap clones the
matching public PactNet branch into ignored `artifacts/` on clean CI runners:
`avro-http`, `avro-async` or consolidated `avro`. For local work, override
`PACTNET_HTTP_SOURCE_DIR` to reuse a sibling checkout:

```shell
bash scripts/bootstrap-http-plugin.sh
bash scripts/install-avro-plugin.sh
bash scripts/release.sh pack-local avro
bash scripts/test-publish.sh avro
```

Set `PACTNET_HTTP_SOURCE_DIR` for another local source path. This runs seven
wrapper tests and five native HTTP/async sample tests, packages Avro independently,
and verifies local NuGet publication. The HTTP provider uses an ephemeral port.
Standard CI discovery includes both gRPC and Avro; the Avro package itself remains
unreleasable until its PactNet API dependency is published.

[Avro messaging](avro-messaging.md) describes the async consumer/provider example,
the native synchronous capability probe, and the remaining sync contributions.

## CI With Act

Install [act](https://github.com/nektos/act) and start Docker. Run from the
repository root on macOS with Docker Desktop:

```shell
ACTIONS_RUNTIME_URL=http://host.docker.internal:34567/ \
act workflow_dispatch -W .github/workflows/ci.yml \
  --container-architecture=linux/arm64 \
  --job test \
  --matrix os:ubuntu-24.04-arm \
  --artifact-server-path "$PWD/artifacts/act-artifacts" \
  --artifact-server-addr 0.0.0.0
```

The job is `test`, not `lint`. Its `discover` dependency uses `ubuntu-latest`, so
both runner labels are mapped. The matrix restriction runs Linux ARM64 only; it
does not validate the macOS, Windows, or Linux x64 jobs.

The artifact server stores uploaded packages locally. Docker Desktop needs
`host.docker.internal` to reach the host process, and the explicit runtime URL
avoids act selecting a VPN interface. Port 34567 must be free. Binding to
`0.0.0.0` exposes this temporary server on host interfaces; use a trusted local
network. Act stops it when the run exits. On Linux, use a host address reachable
from your chosen Docker network rather than copying the Docker Desktop URL.

The workflow bootstraps PactNet, installs the selected plugin, runs the wrapper
and sample tests, packs the wrapper, validates local publication, and uploads its
package artifacts. Both `discover` and `test` must finish successfully. No NuGet
credentials or remote publication are involved.

## Local NuGet Registry

`scripts/test-publish.sh <slug>` uses NuGet's supported folder-feed protocol to
create a registry at `artifacts/nuget-registry`. It publishes the bootstrap
dependencies and selected wrapper using `dotnet nuget push --no-symbols`.

Each invocation creates a new consumer project and an isolated package directory
under `artifacts/publish-validation`. It restores from the local registry and
nuget.org, builds the consumer, and checks NuGet's `.nupkg.metadata` to confirm
the wrapper was downloaded from the local registry, not a previously populated
global package cache. Nuget.org supplies ordinary public transitive dependencies.

Run `bash scripts/release.sh pack-local <slug>` first. Publication can be repeated
locally without changing repository package sources. These development packages
depend on the local #551 prerelease and must not be published remotely.

The check covers package push, restore, dependency resolution, and consumer build.
It does not test HTTP registry authentication or symbol-server publication;
`.snupkg` files remain in the package artifacts. Real release packing still
rejects the bootstrap dependency. See [the release guide](releases.md) for those
publication gates.