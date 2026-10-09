#!/usr/bin/env bash
set -euo pipefail

root=$(cd "$(dirname "$0")/.." && pwd)
cd "$root"
source_dir=${PACTNET_SOURCE_DIR:-"$root/artifacts/pact-net"}
revision=8b6d35e46998aa3b4b583968942d22f258ce8217
version=5.0.0-plugin.551.local

mkdir -p "$root/artifacts/packages"
if [[ ! -d "$source_dir/.git" ]]; then
    git clone --filter=blob:none --no-checkout https://github.com/pact-foundation/pact-net.git "$source_dir"
    git -C "$source_dir" fetch origin "$revision"
    git -C "$source_dir" checkout --detach "$revision"
fi
if [[ $(git -C "$source_dir" rev-parse HEAD) != "$revision" ]]; then
    printf 'Expected PactNet commit %s in %s\n' "$revision" "$source_dir" >&2
    exit 1
fi
source_dir=$(cd "$source_dir" && pwd -P)

bash "$source_dir/build/download-native-libs.sh"
for package in PactNet.Abstractions PactNet PactNet.Output.Xunit; do
    dotnet pack "$source_dir/src/$package/$package.csproj" \
        --configuration Release --output "$root/artifacts/packages" \
        -p:Version="$version" -p:VersionPrefix="$version" \
        -p:ImportDirectoryBuildProps=false -p:ImportDirectoryBuildTargets=false
done