#!/usr/bin/env bash
set -euo pipefail

root=$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)

select_extension() {
    jq -ce --arg slug "$1" 'first(.[] | select(.slug == $slug)) // error("Unknown extension: " + $slug)' <<< "$2"
}

select_release() {
    local tag=$1 registry=$2
    local numeric='(0|[1-9][0-9]*)'
    local identifier='(0|[1-9][0-9]*|[0-9]*[A-Za-z-][0-9A-Za-z-]*)'
    local version_pattern="${numeric}[.]${numeric}[.]${numeric}(-${identifier}([.]${identifier})*)?"
    if [[ ! "$tag" =~ ^([a-z][a-z0-9-]*)/v($version_pattern)$ ]]; then
        printf 'Expected a tag like grpc/v0.1.0-alpha.1\n' >&2
        return 1
    fi
    local slug=${BASH_REMATCH[1]} version=${BASH_REMATCH[2]} extension
    extension=$(select_extension "$slug" "$registry") || return 1
    jq -c --arg version "$version" '. + {version: $version}' <<< "$extension"
}

run_tests() {
    local extension=$1 project name value
    local environment=("PACT_PLUGIN_DIR=$root/artifacts/plugins")
    while IFS=$'\t' read -r name value; do
        value=${value%$'\r'}
        environment+=("$name=$root/$value")
    done < <(jq -r '.environment // {} | to_entries[] | [.key, .value] | @tsv' <<< "$extension")
    while IFS= read -r project; do
        project=${project%$'\r'}
        project_path="$root/$project"
        if command -v cygpath >/dev/null; then
            project_path=$(cygpath -w "$project_path")
        fi
        env "${environment[@]}" dotnet test "$project_path" --configuration Release \
            --framework net8.0 --verbosity minimal
    done < <(jq -r '.tests[]' <<< "$extension")
}

main() {
    cd "$root"
    local command=${1:-} target=${2:-} registry extension project version slug matrix
    registry=$(jq -c . extensions.json)
    case "$command" in
        list)
            matrix=$(jq -c --argjson includeLocal "${INCLUDE_LOCAL_EXTENSIONS:-false}" \
                '{include: [.[] | select(.localOnly != true or $includeLocal)]}' <<< "$registry")
            printf '%s\n' "$matrix"
            if [[ "$target" == --github-output ]]; then
                printf 'matrix=%s\n' "$matrix" >> "${GITHUB_OUTPUT:?GITHUB_OUTPUT is required}"
            fi
            return
            ;;
        plan|pack)
            extension=$(select_release "$target" "$registry")
            project=$(jq -r '.project' <<< "$extension")
            version=$(dotnet msbuild "$project" -getProperty:Version)
            if [[ "$version" != "$(jq -r '.version' <<< "$extension")" ]]; then
                printf 'Tag version does not match project version %s\n' "$version" >&2
                return 1
            fi
            ;;
        test|pack-local)
            extension=$(select_extension "$target" "$registry")
            ;;
        *)
            printf 'Usage: release.sh list | plan <tag> | test <slug> | pack-local <slug> | pack <tag>\n' >&2
            return 1
            ;;
    esac
    if [[ "$command" == plan ]]; then
        printf '%s\n' "$extension"
        if [[ ${3:-} == --github-output ]]; then
            {
                printf 'slug=%s\n' "$(jq -r '.slug' <<< "$extension")"
                printf 'install=%s\n' "$(jq -r '.installPlugin // ""' <<< "$extension")"
            } >> "${GITHUB_OUTPUT:?GITHUB_OUTPUT is required}"
        fi
        return
    fi
    run_tests "$extension"
    if [[ "$command" == pack || "$command" == pack-local ]]; then
        project=$(jq -r '.project' <<< "$extension")
        project=${project%$'\r'}
        project_path="$root/$project"
        if command -v cygpath >/dev/null; then
            project_path=$(cygpath -w "$project_path")
        fi
        slug=$(jq -r '.slug' <<< "$extension")
        local release_build=false
        if [[ "$command" == pack ]]; then release_build=true; fi
        dotnet pack "$project_path" --configuration Release --no-build \
            --output "$root/artifacts/release/$slug" -p:ReleaseBuild="$release_build"
    fi
}

if [[ "${BASH_SOURCE[0]}" == "$0" ]]; then
    main "$@"
fi