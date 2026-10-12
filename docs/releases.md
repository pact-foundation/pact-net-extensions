# Independent Extension Releases

Each wrapper owns its version in its `.csproj`. There is no repository-wide
release version. `extensions.json` maps stable lowercase slugs to package projects,
ordered tests, and optional plugin installers. Shared targets contain metadata,
not a shared version. Native plugin versions are separately selected at runtime.

For gRPC, the release tag is `grpc/v0.1.0-alpha.1`. A future wrapper could use
`avro/v2.3.4` without changing the gRPC package. Only registered slugs are accepted;
the tag must match the selected project's version exactly. Build metadata in tags
is intentionally unsupported. Release-routing tests include a second synthetic
wrapper to check isolation, but no Avro package exists here yet.

## Local Validation

```shell
bash scripts/test-release.sh
bash scripts/release.sh plan grpc/v0.1.0-alpha.1
bash scripts/release.sh pack-local grpc
```

These commands do not create tags, commits, releases, or push packages. `pack-local`
runs only the chosen wrapper's registered tests and creates development packages
under `artifacts/release/<slug>/`.

Run `bash scripts/test-publish.sh <slug>` to publish those packages to the isolated
local NuGet registry and verify a fresh consumer restore/build. See
[developer testing](developer-testing.md) for the act command and registry checks.

Release tooling uses Bash, jq for structured JSON parsing, and dotnet/MSBuild.
GitHub-hosted runner images provide jq; local Windows users need it on Git Bash's
PATH. The `list --github-output` and `plan <tag> --github-output` commands write
discovery and selection outputs directly to `GITHUB_OUTPUT`, without a separate
runtime or generated plan file. Scripts are checked out with LF line endings.

## Before Publishing

1. Wait for PactNet #551 to merge and for a compatible package to be published.
2. Replace `PactNetVersion` in `Directory.Build.props` with that verified version.
   Use the real compatible version of `PactNet.Output.Xunit` in sample projects;
   its independent published version will not necessarily match PactNet's version.
3. Remove the local feed and bootstrap steps from normal CI/release workflows once
   published dependencies work. Retain any bootstrap guidance only for development.
4. Validate all supported platforms, including Linux ARM64. CI currently targets
   Linux x64/ARM64, macOS ARM64, and Windows x64 on .NET 8. Windows .NET Framework
   targets in the imported unit project are not run by this matrix.
5. Set the selected wrapper's version, update its README, run its tests and local
   pack, and verify package dependencies. Keep sibling versions unchanged.
6. Configure the GitHub `nuget` environment with required reviewer approval and a
   `NUGET_API_KEY` environment secret. Restrict release tags using repository rules.
7. Only after approval, create and push the wrapper-specific tag.

The release workflow checks the tag, runs the selected wrapper's tests, and packs
only its project. `ReleaseBuild=true` explicitly rejects the current local #551
dependency before producing a release package. The publication job runs only
after successful packing and environment approval. Native plugins and bootstrap
PactNet packages are never publication artifacts. `.snupkg` files accompany NuGet
packages; NuGet's push command discovers the adjacent symbols package.

Adding another wrapper means adding its projects, README, tests/sample and registry
entry, not adding a second global version or copying the release workflow.