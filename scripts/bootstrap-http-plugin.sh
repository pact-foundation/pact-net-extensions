#!/usr/bin/env bash
set -euo pipefail

root=$(cd "$(dirname "$0")/.." && pwd)
cd "$root"
repository=${PACTNET_HTTP_REPOSITORY:-"https://github.com/YOU54F/pact-net.git"}
ref=${PACTNET_HTTP_REF:-"avro-http"}
source_dir=${PACTNET_HTTP_SOURCE_DIR:-"$root/artifacts/pact-net-http-plugins-$ref"}
base=8b6d35e46998aa3b4b583968942d22f258ce8217
if [[ ! -f "$source_dir/src/PactNet/HttpPluginPactBuilder.cs" ]]; then
    if [[ -e "$source_dir" ]]; then
        printf 'PactNet source directory exists but has no HTTP plugin API: %s\n' "$source_dir" >&2
        exit 1
    fi
    mkdir -p "$(dirname "$source_dir")"
    git clone --filter=blob:none --branch "$ref" --single-branch "$repository" "$source_dir"
fi
source_dir=$(cd "$source_dir" && pwd -P)
git -C "$source_dir" merge-base --is-ancestor "$base" HEAD
version=$(dotnet msbuild src/PactNet.Extensions.Avro/PactNet.Extensions.Avro.csproj \
    -getProperty:AvroPactNetVersion | tail -n 1 | tr -d '\r')
mkdir -p "$root/artifacts/packages"
bash "$source_dir/build/download-native-libs.sh"
for package in PactNet.Abstractions PactNet PactNet.Output.Xunit; do
    dotnet pack "$source_dir/src/$package/$package.csproj" \
        --configuration Release --output "$root/artifacts/packages" \
        -p:Version="$version" -p:VersionPrefix="$version" \
        -p:ImportDirectoryBuildProps=false -p:ImportDirectoryBuildTargets=false
done