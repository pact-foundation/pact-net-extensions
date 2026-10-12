#!/usr/bin/env bash
set -euo pipefail

root=$(cd "$(dirname "$0")/.." && pwd)
version=0.1.3
case "$(uname -s)" in
    Darwin) platform=osx ;;
    Linux) platform=linux ;;
    MINGW*|MSYS*) platform=windows ;;
    *) printf 'Unsupported plugin platform\n' >&2; exit 1 ;;
esac
case "$(uname -m)" in
    arm64|aarch64) architecture=aarch64 ;;
    x86_64|amd64) architecture=x86_64 ;;
    *) printf 'Unsupported plugin architecture\n' >&2; exit 1 ;;
esac
binary=pact-avro-plugin
asset="pact-avro-plugin-$platform-$architecture"
if [[ "$platform" == windows ]]; then binary="$binary.exe"; asset="$asset.exe"; fi
base="https://github.com/hominux/pact-avro-plugin/releases/download/v$version"
plugin_dir="$root/artifacts/plugins/avro-$version"
mkdir -p "$plugin_dir"
cd "$plugin_dir"
curl --fail --silent --show-error --location "$base/$asset.gz" --output "$asset.gz"
curl --fail --silent --show-error --location "$base/$asset.gz.sha256" --output "$asset.gz.sha256"
expected=$(awk '{print $1}' "$asset.gz.sha256")
if command -v sha256sum >/dev/null; then
    actual=$(sha256sum "$asset.gz" | awk '{print $1}')
else
    actual=$(shasum -a 256 "$asset.gz" | awk '{print $1}')
fi
if [[ ! "$expected" =~ ^[0-9a-f]{64}$ || "$actual" != "$expected" ]]; then
    printf 'Plugin checksum mismatch\n' >&2
    exit 1
fi
gunzip -f "$asset.gz"
mv "$asset" "$binary"
chmod +x "$binary"
curl --fail --silent --show-error --location "$base/pact-plugin.json" --output pact-plugin.json
printf 'Plugin installed under %s/artifacts/plugins\n' "$root"