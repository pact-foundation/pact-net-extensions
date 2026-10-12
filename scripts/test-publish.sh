#!/usr/bin/env bash
set -euo pipefail

source "$(dirname "$0")/release.sh"
cd "$root"
extension=$(select_extension "${1:?Usage: test-publish.sh <slug>}" "$(jq -c . extensions.json)")
slug=$(jq -r '.slug' <<< "$extension")
package=$(jq -r '.package' <<< "$extension")
version=$(dotnet msbuild "$(jq -r '.project' <<< "$extension")" -getProperty:Version)
archive="$root/artifacts/release/$slug/$package.$version.nupkg"
if [[ ! -f "$archive" ]]; then
    printf 'Run bash scripts/release.sh pack-local %s before testing publication\n' "$slug" >&2
    exit 1
fi

registry="$root/artifacts/nuget-registry"
mkdir -p "$registry" "$root/artifacts/publish-validation"
for dependency in "$root"/artifacts/packages/*.nupkg; do
    if [[ -f "$dependency" ]]; then
        dotnet nuget push "$dependency" --source "$registry" --no-symbols
    fi
done
dotnet nuget push "$archive" --source "$registry" --no-symbols

validation_dir=$(mktemp -d "$root/artifacts/publish-validation/$slug.XXXXXX")
consumer="$validation_dir/consumer/LocalPublishConsumer.csproj"
dotnet new classlib --name LocalPublishConsumer --framework net8.0 \
    --output "$validation_dir/consumer" --no-restore
dotnet add "$consumer" package "$package" --version "$version" --no-restore
restore_consumer=$consumer
restore_config="$registry/NuGet.Config"
restore_packages="$validation_dir/packages"
if command -v cygpath >/dev/null; then
    restore_consumer=$(cygpath -m "$restore_consumer")
        restore_config=$(cygpath -m "$restore_config")
    restore_packages=$(cygpath -m "$restore_packages")
fi
cat > "$registry/NuGet.Config" <<'EOF'
<?xml version="1.0" encoding="utf-8"?>
<configuration>
    <packageSources>
        <clear />
        <add key="local" value="." />
        <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
    </packageSources>
</configuration>
EOF
dotnet restore "$restore_consumer" --configfile "$restore_config" \
        --packages "$restore_packages" --force --no-cache
dotnet build "$consumer" --configuration Release --no-restore

package_id=$(printf '%s' "$package" | tr '[:upper:]' '[:lower:]')
registry_source=$registry
if command -v cygpath >/dev/null; then
    registry_source=$(cygpath -m "$registry")
fi
jq -e --arg source "$registry_source" \
    '.source | gsub("\\\\"; "/") == $source' \
    "$validation_dir/packages/$package_id/$version/.nupkg.metadata"
printf 'PASS: published and restored %s %s from %s\n' "$package" "$version" "$registry"