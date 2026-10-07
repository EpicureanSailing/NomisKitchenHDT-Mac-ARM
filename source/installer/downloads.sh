#!/bin/bash
# Sources pinned to versions and SHA-256, with no account or machine data.
NOMI_COMMIT=a61c0e744388131aff2eb7ae21f881e43cc4206c
fetch() {
    local name="$1" url="$2" expected="$3" destination="$4"
    if [[ -n "${NOMI_DOWNLOAD_CACHE:-}" && -f "$NOMI_DOWNLOAD_CACHE/$name" ]]; then
        /bin/cp "$NOMI_DOWNLOAD_CACHE/$name" "$destination"
    else
        /usr/bin/curl --fail --location --proto '=https' --tlsv1.2 --retry 2 \
            --connect-timeout 20 --max-time 300 "$url" --output "$destination"
    fi
    [[ "$(/usr/bin/shasum -a 256 "$destination" | /usr/bin/awk '{print $1}')" == "$expected" ]] || {
        echo "SHA-256 incorrect : $name" >&2; return 1;
    }
}
download_runtime() {
    local work="$1" output="$2"
    fetch bepinex.zip 'https://github.com/BepInEx/BepInEx/releases/download/v5.4.23.5/BepInEx_macos_universal_5.4.23.5.zip' \
        01c2ae782eb016dfd6c345a18dbd2dcafffb3d9d318449d6486689f426b4a323 "$work/bepinex.zip"
    fetch corlibs.zip 'https://unity.bepinex.dev/corlibs/6000.3.11.zip' \
        6c1e78b08585c2b294bcdf3f5f4a7657809fbe7ca86d44900bf53c8c15b914b0 "$work/corlibs.zip"
    fetch nomi.zip "https://codeload.github.com/RainWritesCode/NomisKitchenHDT/zip/$NOMI_COMMIT" \
        ee56518df1479b05f0550a7836d348628e203b02d983dd9ed4e76b7eb2db9bdd "$work/nomi.zip"
    /usr/bin/ditto -x -k "$work/bepinex.zip" "$work/bepinex"
    /usr/bin/ditto -x -k "$work/nomi.zip" "$work/nomi"
    /bin/mkdir -p "$output/BepInEx/core" "$output/BepInEx/plugins" \
        "$output/BepInEx/config" "$output/BepInEx/unity6000_corlibs"
    /bin/cp "$work/bepinex/BepInEx/core/"*.dll "$output/BepInEx/core/"
    /usr/bin/ditto -x -k "$work/corlibs.zip" "$output/BepInEx/unity6000_corlibs"
    local source="$work/nomi/NomisKitchenHDT-$NOMI_COMMIT"
    /bin/cp -R "$source/Installer/BepInEx/BepInEx/unstripped_corlib" "$output/BepInEx/"
    /bin/cp "$source/Resources/com.community.hs.NomiCantDance.dll" "$output/BepInEx/plugins/"
    [[ "$(/usr/bin/shasum -a 256 "$output/BepInEx/plugins/com.community.hs.NomiCantDance.dll" | /usr/bin/awk '{print $1}')" == \
        841a7747edaf2925a24728fe531f62dec05541ef3329c0204749b4041e60b1b1 ]]
}
