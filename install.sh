#!/usr/bin/env bash

set -euo pipefail

repository="https://github.com/devmobasa/QuickOTP"
source_branch="main"
install_mode="prebuilt"
plugin_mode="auto"
assume_yes=0
source_dir=""
dotnet_command=""
dotnet_was_installed=0

usage() {
  cat <<'USAGE'
Usage: install.sh [options]

Install QuickOTP Popup and Editor on Arch Linux.

Options:
  --prebuilt                 Download the latest release binaries (default)
  --build                    Test and build Native AOT binaries from source
  --with-omarchy-plugin      Install and enable the Omarchy bar widget
  --without-omarchy-plugin   Do not install the Omarchy bar widget
  --yes                      Accept installer confirmations
  -h, --help                 Show this help
USAGE
}

fail() {
  printf 'QuickOTP installer: %s\n' "$1" >&2
  exit 1
}

confirm() {
  local prompt="$1"
  local default_answer="${2:-no}"
  local reply=""

  (( assume_yes )) && return 0
  [[ -r /dev/tty ]] ||
    fail "confirmation required; rerun interactively or pass --yes"

  read -r -p "$prompt" reply </dev/tty || return 1
  case "$reply" in
  y | Y | yes | YES | Yes) return 0 ;;
  n | N | no | NO | No) return 1 ;;
  "") [[ "$default_answer" == "yes" ]] ;;
  *) return 1 ;;
  esac
}

while (( $# > 0 )); do
  case "$1" in
  --prebuilt)
    install_mode="prebuilt"
    ;;
  --build)
    install_mode="build"
    ;;
  --with-omarchy-plugin)
    plugin_mode="install"
    ;;
  --without-omarchy-plugin)
    plugin_mode="skip"
    ;;
  --yes | -y)
    assume_yes=1
    ;;
  -h | --help)
    usage
    exit 0
    ;;
  *)
    fail "unknown option: $1"
    ;;
  esac
  shift
done

[[ "$(uname -s)" == "Linux" && -f /etc/arch-release ]] ||
  fail "this installer supports Arch Linux only"
[[ "$(uname -m)" == "x86_64" ]] ||
  fail "the prebuilt release requires an x86_64 system"

for command_name in curl tar mktemp cp ln; do
  command -v "$command_name" >/dev/null 2>&1 ||
    fail "missing required command: $command_name"
done

install_dir="${QUICKOTP_INSTALL_DIR:-${XDG_DATA_HOME:-$HOME/.local/share}/QuickOTP}"
bin_dir="${QUICKOTP_BIN_DIR:-$HOME/.local/bin}"
dotnet_dir="${QUICKOTP_DOTNET_DIR:-$HOME/.dotnet}"
download_dir="$(mktemp -d)"
stage_dir="$download_dir/stage"

cleanup() {
  rm -rf -- "$download_dir"
}
trap cleanup EXIT

mkdir -p "$stage_dir" "$install_dir" "$bin_dir"

ensure_source_tree() {
  [[ -n "$source_dir" ]] && return 0

  local local_script="${BASH_SOURCE[0]:-}"
  if [[ -n "$local_script" && -f "$local_script" ]]; then
    local local_root
    local_root="$(cd -- "$(dirname -- "$local_script")" && pwd -P)"
    if [[ -f "$local_root/QuickOTP.slnx" && -d "$local_root/omarchy-plugin/community.quickotp" ]]; then
      source_dir="$local_root"
      return 0
    fi
  fi

  local source_archive="$download_dir/QuickOTP-$source_branch.tar.gz"
  local source_parent="$download_dir/source"
  printf 'Downloading QuickOTP source...\n'
  curl --fail --location --show-error --silent \
    "$repository/archive/refs/heads/$source_branch.tar.gz" \
    --output "$source_archive"
  mkdir -p "$source_parent"
  tar --extract --gzip --file "$source_archive" --directory "$source_parent"
  source_dir="$(find "$source_parent" -mindepth 1 -maxdepth 1 -type d -name 'QuickOTP-*' -print -quit)"
  [[ -n "$source_dir" && -f "$source_dir/QuickOTP.slnx" ]] ||
    fail "downloaded source archive has an unexpected layout"
}

install_prebuilt() {
  local app archive
  for app in Popup Editor; do
    archive="QuickOTP.${app}-linux-x64.tar.gz"

    printf 'Downloading %s...\n' "$archive"
    curl --fail --location --show-error --silent \
      "$repository/releases/latest/download/$archive" \
      --output "$download_dir/$archive"
    tar --extract --gzip \
      --file "$download_dir/$archive" \
      --directory "$stage_dir"

    [[ -x "$stage_dir/QuickOTP.${app}/QuickOTP.${app}" ]] ||
      fail "$archive does not contain the expected executable"
  done
}

dotnet_has_sdk_10() {
  local candidate="$1"
  "$candidate" --list-sdks 2>/dev/null | awk '{ print $1 }' | grep -Eq '^10[.]'
}

select_dotnet() {
  local candidate=""
  if command -v dotnet >/dev/null 2>&1; then
    candidate="$(command -v dotnet)"
    if dotnet_has_sdk_10 "$candidate"; then
      dotnet_command="$candidate"
      return 0
    fi
  fi

  candidate="$dotnet_dir/dotnet"
  if [[ -x "$candidate" ]] && dotnet_has_sdk_10 "$candidate"; then
    dotnet_command="$candidate"
    return 0
  fi

  return 1
}

ensure_dotnet_10() {
  select_dotnet && return 0

  confirm ".NET 10 SDK was not found. Download Microsoft's dotnet-install.sh and install it under $dotnet_dir? [y/N] " ||
    fail ".NET 10 SDK is required for --build"

  if ! command -v wget >/dev/null 2>&1; then
    confirm "wget is required to download dotnet-install.sh. Install wget with pacman? [y/N] " ||
      fail "wget is required to install .NET"
    command -v sudo >/dev/null 2>&1 || fail "sudo is required to install wget"
    sudo pacman -S --needed --noconfirm wget
  fi

  (
    cd "$download_dir"
    wget https://dot.net/v1/dotnet-install.sh -O dotnet-install.sh
    bash dotnet-install.sh --channel 10.0 --install-dir "$dotnet_dir"
  )

  export DOTNET_ROOT="$dotnet_dir"
  export PATH="$dotnet_dir:$PATH"
  dotnet_was_installed=1
  select_dotnet || fail "dotnet-install.sh completed without installing a .NET 10 SDK"
}

ensure_native_build_dependencies() {
  local missing_packages=()
  pacman -Q clang >/dev/null 2>&1 || missing_packages+=(clang)
  pacman -Q zlib >/dev/null 2>&1 || missing_packages+=(zlib)
  (( ${#missing_packages[@]} == 0 )) && return 0

  confirm "Install Native AOT build packages with pacman: ${missing_packages[*]}? [y/N] " ||
    fail "Native AOT build packages are required for --build"
  command -v sudo >/dev/null 2>&1 || fail "sudo is required to install build packages"
  sudo pacman -S --needed --noconfirm "${missing_packages[@]}"
}

build_from_source() {
  ensure_dotnet_10
  ensure_native_build_dependencies
  ensure_source_tree

  printf 'Running QuickOTP tests...\n'
  (
    cd "$source_dir"
    "$dotnet_command" test QuickOTP.Tests/QuickOTP.Tests.csproj \
      -c Release --nologo
  )

  local app app_output
  for app in Popup Editor; do
    app_output="$stage_dir/QuickOTP.${app}"
    printf 'Publishing QuickOTP.%s as Native AOT...\n' "$app"
    (
      cd "$source_dir"
      "$dotnet_command" publish "QuickOTP.${app}/QuickOTP.${app}.csproj" \
        -c Release \
        -r linux-x64 \
        --self-contained true \
        -o "$app_output"
    )
    find "$app_output" -maxdepth 1 -type f \( -name '*.dbg' -o -name '*.pdb' \) -delete
    [[ -x "$app_output/QuickOTP.${app}" ]] ||
      fail "QuickOTP.$app publish did not produce the expected executable"
  done
}

install_apps() {
  local app app_dir
  for app in Popup Editor; do
    app_dir="$install_dir/QuickOTP.${app}"
    mkdir -p "$app_dir"
    cp -a "$stage_dir/QuickOTP.${app}/." "$app_dir/"
  done

  ln -sfn "$install_dir/QuickOTP.Popup/QuickOTP.Popup" "$bin_dir/quickotp-popup"
  ln -sfn "$install_dir/QuickOTP.Editor/QuickOTP.Editor" "$bin_dir/quickotp-editor"
}

install_omarchy_plugin() {
  [[ "$plugin_mode" != "skip" ]] || return 0

  if ! command -v omarchy >/dev/null 2>&1; then
    [[ "$plugin_mode" != "install" ]] ||
      fail "--with-omarchy-plugin requires Omarchy"
    return 0
  fi

  if [[ "$plugin_mode" == "auto" ]]; then
    confirm "Omarchy detected. Install and enable the QuickOTP bar widget? [Y/n] " "yes" || return 0
  fi

  local plugin_source plugins_dir plugin_target plugin_stage plugin_backup
  ensure_source_tree
  plugin_source="$source_dir/omarchy-plugin/community.quickotp"
  plugins_dir="${XDG_CONFIG_HOME:-$HOME/.config}/omarchy/plugins"
  plugin_target="$plugins_dir/community.quickotp"
  plugin_stage="$plugins_dir/.community.quickotp.install.$$"
  plugin_backup="$plugins_dir/.community.quickotp.backup.$$"

  [[ -f "$plugin_source/manifest.json" ]] || fail "QuickOTP plugin source is missing"
  omarchy plugin validate "$plugin_source"
  mkdir -p "$plugins_dir"

  if [[ -e "$plugin_target" && ! -f "$plugin_target/.quickotp-installer-managed" ]]; then
    fail "refusing to overwrite unmanaged plugin at $plugin_target"
  fi

  rm -rf -- "$plugin_stage" "$plugin_backup"
  cp -a "$plugin_source" "$plugin_stage"
  : >"$plugin_stage/.quickotp-installer-managed"
  omarchy plugin validate "$plugin_stage"

  if [[ -e "$plugin_target" ]]; then
    mv "$plugin_target" "$plugin_backup"
  fi
  if ! mv "$plugin_stage" "$plugin_target"; then
    [[ ! -e "$plugin_backup" ]] || mv "$plugin_backup" "$plugin_target"
    fail "could not install the Omarchy plugin"
  fi
  rm -rf -- "$plugin_backup"

  omarchy shell shell rescanPlugins >/dev/null
  if ! omarchy plugin list --json | jq -e \
    'any(.[]; .id == "community.quickotp" and .enabled == true)' >/dev/null; then
    omarchy plugin enable community.quickotp --section right
  fi
  printf 'Installed Omarchy plugin: community.quickotp\n'
}

if [[ "$install_mode" == "build" ]]; then
  build_from_source
else
  install_prebuilt
fi

install_apps
install_omarchy_plugin

printf '\nQuickOTP Popup and Editor are installed.\n'
printf 'Run: %s/quickotp-popup\n' "$bin_dir"
printf 'Run: %s/quickotp-editor\n' "$bin_dir"

case ":$PATH:" in
*":$bin_dir:"*) ;;
*) printf 'Add %s to PATH to use the commands from any terminal.\n' "$bin_dir" ;;
esac

if (( dotnet_was_installed )); then
  printf 'The .NET SDK was installed under %s. Add it to PATH for future builds.\n' "$dotnet_dir"
fi
