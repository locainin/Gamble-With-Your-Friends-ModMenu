#!/usr/bin/env bash

set -Eeuo pipefail
IFS=$'\n\t'

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd -P)"
readonly SCRIPT_DIR
SCRIPT_PATH="${SCRIPT_DIR}/$(basename -- "${BASH_SOURCE[0]}")"
readonly SCRIPT_PATH
readonly GAME_NAME="Gamble With Your Friends"
readonly GAME_EXECUTABLE="${GAME_NAME}.exe"
readonly MOD_PROJECT="${SCRIPT_DIR}/ModMenu/ModMenu.csproj"
readonly MOD_OUTPUT="${SCRIPT_DIR}/ModMenu/bin/Release/netstandard2.1/ModMenu.dll"
readonly MOD_FILE_NAME="ModMenu.dll"
readonly BEPINEX_RELEASE_API="https://api.github.com/repos/BepInEx/BepInEx/releases/latest"
readonly PROTON_LAUNCH_OPTIONS='WINEDLLOVERRIDES="winhttp=n,b" %command%'

TEMP_ROOT=""

if [[ -t 1 && -z "${NO_COLOR:-}" ]]; then
    readonly COLOR_BLUE=$'\033[1;34m'
    readonly COLOR_GREEN=$'\033[1;32m'
    readonly COLOR_RED=$'\033[1;31m'
    readonly COLOR_YELLOW=$'\033[1;33m'
    readonly COLOR_RESET=$'\033[0m'
else
    readonly COLOR_BLUE=""
    readonly COLOR_GREEN=""
    readonly COLOR_RED=""
    readonly COLOR_YELLOW=""
    readonly COLOR_RESET=""
fi

# Writes a normal installer status line
print_info() {
    printf '%s[INFO]%s %s\n' "$COLOR_BLUE" "$COLOR_RESET" "$*"
}

# Writes a completed installer status line
print_success() {
    printf '%s[OK]%s %s\n' "$COLOR_GREEN" "$COLOR_RESET" "$*"
}

# Writes a recoverable warning to standard error
print_warning() {
    printf '%s[WARN]%s %s\n' "$COLOR_YELLOW" "$COLOR_RESET" "$*" >&2
}

# Stops the installer with a visible error
fail() {
    printf '%s[ERROR]%s %s\n' "$COLOR_RED" "$COLOR_RESET" "$*" >&2
    exit 1
}

# Removes temporary download and extraction data
cleanup() {
    if [[ -n "$TEMP_ROOT" && -d "$TEMP_ROOT" ]]; then
        rm -rf -- "$TEMP_ROOT"
    fi

}

trap cleanup EXIT
trap 'exit 130' INT
trap 'exit 143' TERM

# Verifies one external command before its first use
require_command() {
    local command_name="$1"

    if ! command -v "$command_name" >/dev/null 2>&1; then
        fail "Required command not found: ${command_name}"
    fi
}

# Checks the minimum files required for a valid game directory
is_game_directory() {
    local candidate="$1"

    [[ -f "${candidate}/${GAME_EXECUTABLE}" &&
        -d "${candidate}/${GAME_NAME}_Data/Managed" ]]
}

# Searches parent directories when the repository lives inside the game tree
find_game_in_ancestors() {
    local candidate="$SCRIPT_DIR"

    while [[ "$candidate" != "/" ]]; do
        if is_game_directory "$candidate"; then
            printf '%s\n' "$candidate"
            return 0
        fi

        candidate="$(dirname -- "$candidate")"
    done

    return 1
}

# Searches default and custom Steam libraries from Valve metadata
find_game_in_steam_libraries() {
    local steam_root
    local library_file
    local library_path
    local candidate
    local -a steam_roots=(
        "${HOME}/.local/share/Steam"
        "${HOME}/.steam/steam"
    )

    for steam_root in "${steam_roots[@]}"; do
        candidate="${steam_root}/steamapps/common/${GAME_NAME}"
        if is_game_directory "$candidate"; then
            printf '%s\n' "$candidate"
            return 0
        fi

        library_file="${steam_root}/steamapps/libraryfolders.vdf"
        [[ -f "$library_file" ]] || continue

        while IFS= read -r library_path; do
            candidate="${library_path}/steamapps/common/${GAME_NAME}"
            if is_game_directory "$candidate"; then
                printf '%s\n' "$candidate"
                return 0
            fi
        done < <(
            awk -F'"' '$2 == "path" { gsub(/\\\\/, "/", $4); print $4 }' "$library_file"
        )
    done

    return 1
}

# Resolves and normalizes the selected game directory
resolve_game_directory() {
    local candidate

    if [[ -n "${CASINO_MENU_GAME_DIR:-}" ]]; then
        candidate="$CASINO_MENU_GAME_DIR"
        is_game_directory "$candidate" ||
            fail "CASINO_MENU_GAME_DIR does not contain ${GAME_EXECUTABLE}"
        cd -- "$candidate"
        pwd -P
        return 0
    fi

    if candidate="$(find_game_in_ancestors)"; then
        printf '%s\n' "$candidate"
        return 0
    fi

    if candidate="$(find_game_in_steam_libraries)"; then
        printf '%s\n' "$candidate"
        return 0
    fi

    fail "Game installation not found. Set CASINO_MENU_GAME_DIR and run the installer again"
}

# Prevents concurrent installs from replacing the same plugin
acquire_install_lock() {
    local game_directory="$1"

    require_command flock
    # Lock the directory descriptor without leaving installer files in the game folder
    exec 9<"$game_directory"
    if ! flock -n 9; then
        fail "Another Casino Menu install or uninstall is already running"
    fi
}

# Checks whether the required BepInEx loader files already exist
has_bepinex() {
    local game_directory="$1"

    [[ -f "${game_directory}/winhttp.dll" &&
        -f "${game_directory}/doorstop_config.ini" &&
        -f "${game_directory}/BepInEx/core/BepInEx.dll" ]]
}

# Creates one private temporary workspace for the current process
create_temp_root() {
    if [[ -z "$TEMP_ROOT" ]]; then
        TEMP_ROOT="$(mktemp -d -t casino-menu.XXXXXXXX)"
    fi
}

# Downloads or copies a verified BepInEx archive into the temporary workspace
download_bepinex_archive() {
    local archive_path="$1"
    local metadata
    local asset_url
    local expected_digest
    local actual_digest

    if [[ -n "${CASINO_MENU_BEPINEX_ARCHIVE:-}" ]]; then
        [[ -f "$CASINO_MENU_BEPINEX_ARCHIVE" ]] ||
            fail "CASINO_MENU_BEPINEX_ARCHIVE does not exist"
        cp -- "$CASINO_MENU_BEPINEX_ARCHIVE" "$archive_path"
        return 0
    fi

    print_info "Resolving the latest official BepInEx x64 release"
    metadata="$(
        curl --proto '=https' --tlsv1.2 --fail --silent --show-error --location \
            "$BEPINEX_RELEASE_API" |
            python3 -c '
import json
import re
import sys

release = json.load(sys.stdin)
assets = [
    asset for asset in release.get("assets", [])
    if re.fullmatch(r"BepInEx_win_x64_[0-9.]+\.zip", asset.get("name", ""))
]
if len(assets) != 1:
    raise SystemExit("official x64 release asset was not uniquely identified")
asset = assets[0]
digest = asset.get("digest", "")
if not re.fullmatch(r"sha256:[0-9a-fA-F]{64}", digest):
    raise SystemExit("official release does not provide a SHA-256 digest")
print(asset["browser_download_url"])
print(digest.removeprefix("sha256:").lower())
'
    )"

    asset_url="${metadata%%$'\n'*}"
    expected_digest="${metadata##*$'\n'}"
    [[ -n "$asset_url" && -n "$expected_digest" ]] ||
        fail "BepInEx release metadata was incomplete"

    print_info "Downloading $(basename -- "$asset_url")"
    curl --proto '=https' --tlsv1.2 --fail --silent --show-error --location \
        --output "$archive_path" "$asset_url"

    actual_digest="$(sha256sum "$archive_path")"
    actual_digest="${actual_digest%% *}"
    [[ "$actual_digest" == "$expected_digest" ]] ||
        fail "BepInEx archive checksum did not match the official release"
}

# Installs BepInEx only when the loader is incomplete
install_bepinex() {
    local game_directory="$1"
    local archive_path
    local staging_directory

    if has_bepinex "$game_directory"; then
        print_success "BepInEx is already installed"
        return 0
    fi

    create_temp_root
    archive_path="${TEMP_ROOT}/bepinex.zip"
    staging_directory="${TEMP_ROOT}/bepinex"
    mkdir -p -- "$staging_directory"

    download_bepinex_archive "$archive_path"
    unzip -tq "$archive_path" >/dev/null ||
        fail "BepInEx archive failed its integrity check"
    unzip -q "$archive_path" -d "$staging_directory"

    [[ -f "${staging_directory}/winhttp.dll" &&
        -f "${staging_directory}/doorstop_config.ini" &&
        -f "${staging_directory}/BepInEx/core/BepInEx.dll" ]] ||
        fail "BepInEx archive did not contain the expected x64 files"

    print_info "Installing BepInEx into the game directory"
    cp -a -- "${staging_directory}/." "${game_directory}/"
    has_bepinex "$game_directory" ||
        fail "BepInEx installation did not complete"
    print_success "BepInEx installed"
}

# Restores and builds the plugin against the selected game assemblies
build_mod() {
    local game_directory="$1"

    [[ -f "$MOD_PROJECT" ]] ||
        fail "Mod project not found at ${MOD_PROJECT}"

    print_info "Restoring project dependencies"
    (
        cd -- "$SCRIPT_DIR"
        dotnet restore "$MOD_PROJECT" \
            -p:AuditPipeline=true \
            -p:GameInstallDir="$game_directory"
    )

    print_info "Building Casino Menu in Release mode"
    (
        cd -- "$SCRIPT_DIR"
        dotnet build "$MOD_PROJECT" \
            --configuration Release \
            --no-restore \
            --warnaserror \
            -p:GameInstallDir="$game_directory"
    )

    [[ -f "$MOD_OUTPUT" ]] ||
        fail "Build completed without producing ${MOD_FILE_NAME}"
}

# Installs or updates the complete plugin
install_mod() {
    local game_directory
    local plugins_directory
    local temporary_plugin

    require_command awk
    require_command cp
    require_command curl
    require_command dotnet
    require_command flock
    require_command mktemp
    require_command python3
    require_command sha256sum
    require_command unzip

    game_directory="$(resolve_game_directory)"
    acquire_install_lock "$game_directory"
    print_info "Game directory: ${game_directory}"

    install_bepinex "$game_directory"
    build_mod "$game_directory"

    plugins_directory="${game_directory}/BepInEx/plugins"
    temporary_plugin="${plugins_directory}/.${MOD_FILE_NAME}.new"
    mkdir -p -- "$plugins_directory"

    # A same-filesystem rename prevents BepInEx from observing a partial DLL
    install -m 0644 -- "$MOD_OUTPUT" "$temporary_plugin"
    mv -f -- "$temporary_plugin" "${plugins_directory}/${MOD_FILE_NAME}"

    print_success "Casino Menu installed"
    printf '\nSteam launch options:\n  %s\n\n' "$PROTON_LAUNCH_OPTIONS"
    printf 'Start the game and press Insert to toggle the menu\n'
}

# Removes plugin-owned files while preserving the shared mod loader
uninstall_mod() {
    local game_directory
    local plugins_directory
    local removed_any=false

    game_directory="$(resolve_game_directory)"
    acquire_install_lock "$game_directory"
    plugins_directory="${game_directory}/BepInEx/plugins"

    if [[ -f "${plugins_directory}/${MOD_FILE_NAME}" ]]; then
        rm -f -- "${plugins_directory}/${MOD_FILE_NAME}"
        removed_any=true
    fi

    if [[ "$removed_any" == true ]]; then
        print_success "Casino Menu removed"
    else
        print_warning "Casino Menu was not installed"
    fi

    # BepInEx may be shared by other mods, so uninstall leaves the loader intact
    print_info "BepInEx was left installed for other mods"
    print_info "Removing installer: ${SCRIPT_PATH}"
    rm -f -- "$SCRIPT_PATH" ||
        fail "Casino Menu was removed, but the installer could not delete itself"
}

# Displays the requested interactive action menu
print_menu() {
    printf '%sCasino Menu Linux Installer%s\n' "$COLOR_BLUE" "$COLOR_RESET"
    printf '  1) Install or update Casino Menu\n'
    printf '  2) Uninstall Casino Menu and remove this installer\n'
}

# Routes interactive choices and automation-friendly subcommands
main() {
    local action="${1:-}"

    if [[ -z "$action" ]]; then
        print_menu
        read -r -p "Select an option [1-2]: " action
    fi

    case "$action" in
        1 | install)
            install_mod
            ;;
        2 | uninstall)
            uninstall_mod
            ;;
        *)
            fail "Invalid option: ${action}"
            ;;
    esac
}

main "$@"
