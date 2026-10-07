#!/bin/bash
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/.." && pwd -P)"
cd "$ROOT"
/usr/bin/arch -arm64 /usr/bin/true || { echo 'Build requires Apple Silicon.' >&2; exit 1; }
source installer/downloads.sh
mkdir -p build/downloads build/payload
download_runtime "$ROOT/build/downloads" "$ROOT/build/runtime"
clang -arch arm64 -mmacosx-version-min=11.0 -Os -Wall -Wextra \
    -ffile-prefix-map="$ROOT"=/_/nomi-arm64 src/relay.c -o build/payload/Hearthstone
clang -arch arm64 -mmacosx-version-min=11.0 -dynamiclib -Os \
    -ffile-prefix-map="$ROOT"=/_/nomi-arm64 -fmacro-prefix-map="$ROOT"=/_/nomi-arm64 \
    -I vendor/Doorstop/src vendor/Doorstop/src/*.c \
    vendor/Doorstop/src/config/*.c vendor/Doorstop/src/util/*.c \
    vendor/Doorstop/src/runtimes/*.c vendor/Doorstop/src/nix/*.c \
    vendor/Doorstop/src/nix/plthook/plthook_osx.c \
    -Wl,-install_name,@rpath/libdoorstop.dylib -o build/payload/libdoorstop.dylib
codesign --force --sign - build/payload/Hearthstone
codesign --force --sign - build/payload/libdoorstop.dylib
export DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_GENERATE_ASPNET_CERTIFICATE=false
"${DOTNET:-dotnet}" build src/Preloader.csproj -c Release --nologo
cp src/bin/Release/net35/BepInEx.Preloader.dll build/payload/
"${DOTNET:-dotnet}" build src/StatusOverlay.csproj -c Release --nologo \
    -p:UnityManagedDir="${UNITY_MANAGED_DIR:-/Applications/Hearthstone/Hearthstone.app/Contents/Resources/Data/Managed}" \
    -p:BaseIntermediateOutputPath=obj/Status/
cp src/bin/Release/net472/NomiDance.Status.dll build/payload/
(cd build/payload && shasum -a 256 Hearthstone libdoorstop.dylib BepInEx.Preloader.dll NomiDance.Status.dll > SHA256SUMS)
echo 'Payload built. Run scripts/package.py to produce GitHub artifacts.'
