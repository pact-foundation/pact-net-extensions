#!/usr/bin/env bash
set -euo pipefail

root=$(cd "$(dirname "$0")/.." && pwd)
source "$(dirname "$0")/release.sh"
registry='[{"slug":"grpc","package":"Grpc"},{"slug":"avro","package":"Avro"}]'
selection=$(select_release avro/v2.3.4 "$registry")
[[ $(jq -r '.package' <<< "$selection") == Avro ]]
[[ $(jq -r '.version' <<< "$selection") == 2.3.4 ]]
selection=$(select_release grpc/v0.1.0-alpha.1 "$(jq -c . "$root/extensions.json")")
[[ $(jq -r '.package' <<< "$selection") == PactNet.Extensions.Grpc ]]
printf 'PASS: only the tagged wrapper is selected\n'

for tag in unknown/v1.0.0 v1.0.0 grpc/v01.0.0 grpc/v1.0 grpc/v1.0.0-01 'grpc/v1.0.0;echo bad' $'grpc/v1.0.0\n'; do
    if select_release "$tag" "$registry" >/dev/null 2>&1; then
        printf 'FAIL: accepted invalid tag %s\n' "$tag" >&2
        exit 1
    fi
done
printf 'PASS: unknown wrappers and malformed versions are rejected\n'

if bash "$root/scripts/release.sh" plan grpc/v9.9.9 >/dev/null 2>&1; then
    printf 'FAIL: accepted a mismatched package version\n' >&2
    exit 1
fi
printf 'PASS: tag must match the project version\n'

matrix=$(bash "$root/scripts/release.sh" list)
jq -e '[.include[].slug] == ["grpc", "avro"]' <<< "$matrix" >/dev/null
printf 'PASS: gRPC and Avro are included in CI discovery\n'