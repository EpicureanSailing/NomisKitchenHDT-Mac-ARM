#!/bin/bash
set -euo pipefail
BASE="$(cd "$(dirname "$0")" && pwd -P)"
source "$BASE/downloads.sh"
fail() { echo "Erreur : $*" >&2; exit 1; }
sha() { /usr/bin/shasum -a 256 "$1" | /usr/bin/awk '{print $1}'; }
require_game_closed() {
    local result
    # macOS may expose the saved executable name, or a truncated comm name.
    if /usr/bin/pgrep -x 'Hearthstone([.]nom.*)?' >/dev/null; then
        fail 'Fermer Hearthstone avant de continuer.'
    else
        result=$?; [[ "$result" == 1 ]] || fail 'Impossible de vérifier les processus.'
    fi
    if /usr/bin/pgrep -f '[/]Contents/MacOS/Hearthstone([.]nomi-original)?([[:space:]]|$)' >/dev/null; then
        fail 'Fermer Hearthstone avant de continuer.'
    else
        result=$?; [[ "$result" == 1 ]] || fail 'Impossible de vérifier les processus.'
    fi
}
mode="${1:-}"
[[ "$mode" == install || "$mode" == uninstall ]] || fail 'Mode attendu : install ou uninstall'
/usr/bin/arch -arm64 /usr/bin/true || fail 'Ce paquet nécessite un Mac Apple Silicon (ARM64).'
app="${2:-/Applications/Hearthstone/Hearthstone.app}"
[[ -d "$app/Contents/MacOS" && ! -L "$app" ]] || fail 'Hearthstone.app introuvable ou lien symbolique non pris en charge.'
app="$(cd "$app" && pwd -P)"
[[ "$(/usr/libexec/PlistBuddy -c 'Print :CFBundleExecutable' "$app/Contents/Info.plist")" == Hearthstone ]] || fail 'Application inattendue.'
game="$app/Contents/MacOS/Hearthstone"
backup="$game.nomi-original"
root="$(dirname "$app")"
runtime="$root/.nomi-dance-arm64"
[[ -f "$game" && ! -L "$game" ]] || fail 'Exécutable absent ou lien symbolique.'
require_game_closed
[[ -w "$root" && -w "$(dirname "$game")" ]] || fail 'Dossier du jeu non accessible en écriture. Ne pas lancer avec sudo ; vérifier les permissions dans Finder.'
work="$(/usr/bin/mktemp -d "${TMPDIR:-/tmp}/nomi-arm64.XXXXXXXX")"
staged=""; moved_original=0; installed_runtime=0
cleanup() {
    result=$?
    if [[ "$result" != 0 && "$moved_original" == 1 && -f "$backup" ]]; then
        /bin/mv -f "$backup" "$game"
        if [[ "$installed_runtime" == 1 ]]; then /bin/rm -rf "$runtime"; fi
    fi
    if [[ -n "$staged" && -d "$staged" ]]; then /bin/rm -rf "$staged"; fi
    /bin/rm -rf "$work"
    exit "$result"
}
trap cleanup EXIT
if [[ "$mode" == uninstall ]]; then
    [[ -d "$runtime" && ! -L "$runtime" && -f "$runtime/managed-by-nomi-arm64" && -f "$backup" && ! -L "$backup" ]] || fail 'Installation gérée introuvable. Aucun fichier modifié.'
    [[ "$(sha "$backup")" == "$(cat "$runtime/original.sha256")" ]] || fail 'Sauvegarde différente. Aucun fichier modifié.'
    current="$(sha "$game")"
    if [[ "$current" == "$(cat "$runtime/relay.sha256")" ]]; then
        /bin/mv -f "$backup" "$game"
    elif [[ "$current" == "$(cat "$runtime/original.sha256")" ]]; then
        # A repair or manual restore already put the exact original back.
        # Leave it untouched and remove only the verified duplicate backup.
        /bin/rm "$backup"
    else
        fail 'Battle.net a peut-être mis à jour le jeu. Restauration arrêtée pour ne pas écraser cette version.'
    fi
    /bin/rm -rf "$runtime"
    echo 'Hearthstone original restauré. Runtime, configuration et journaux du module supprimés.'
    exit 0
fi
[[ ! -e "$backup" && ! -e "$runtime" && ! -L "$runtime" ]] || fail 'Un relais ou une installation existe déjà. Le désinstaller avant de continuer.'
case "$(/usr/bin/file -b "$game")" in
    *arm64*) ;;
    *) fail 'Hearthstone ne contient pas de version ARM64.' ;;
esac
LC_ALL=C /usr/bin/grep -a -q -F '6000.3.11f1' "$app/Contents/Frameworks/UnityPlayer.dylib" || fail 'Version Unity non validée : paquet prévu pour 6000.3.11f1.'
[[ -d "$BASE/payload" ]] || fail 'Payload absent : utiliser le ZIP de release complet.'
(cd "$BASE/payload" && /usr/bin/shasum -a 256 -c SHA256SUMS) || fail 'Payload endommagé.'
echo 'Téléchargement des composants amont et vérification SHA-256…'
download_runtime "$work" "$work/runtime"
/bin/cp "$BASE/payload/BepInEx.Preloader.dll" "$work/runtime/BepInEx/core/"
/bin/cp "$BASE/payload/libdoorstop.dylib" "$work/runtime/"
/bin/cp "$BASE/payload/NomiDance.Status.dll" "$work/runtime/BepInEx/plugins/"
/bin/cp -R "$BASE/licenses" "$work/runtime/"
printf '[Fix]\nEnabled = true\nLogFixes = false\n' > "$work/runtime/BepInEx/config/com.community.hs.NomiCantDance.cfg"
printf '[Preloader]\nApplyRuntimePatches = true\n[Logging.Console]\nEnabled = false\n' > "$work/runtime/BepInEx/config/BepInEx.cfg"
sha "$game" > "$work/runtime/original.sha256"
sha "$BASE/payload/Hearthstone" > "$work/runtime/relay.sha256"
printf '0.2.0\n' > "$work/runtime/managed-by-nomi-arm64"
staged="$(/usr/bin/mktemp -d "$root/.nomi-arm64-stage.XXXXXXXX")"
/bin/cp -R "$work/runtime" "$staged/runtime"
/bin/cp "$BASE/payload/Hearthstone" "$staged/Hearthstone"
/bin/chmod 755 "$staged/Hearthstone"
# Recheck after downloads: the user or Battle.net may have started/updated the game.
require_game_closed
[[ ! -e "$backup" && ! -e "$runtime" && "$(sha "$game")" == "$(cat "$staged/runtime/original.sha256")" ]] || fail 'Le jeu a changé pendant la préparation. Installation annulée.'
/bin/mv "$game" "$backup"
moved_original=1
/bin/mv "$staged/runtime" "$runtime"
installed_runtime=1
/bin/mv "$staged/Hearthstone" "$game"
moved_original=0
echo 'Installation terminée. Lancer Hearthstone avec Jouer dans Battle.net.'
echo 'Merci à Nomi’s Kitchen : https://github.com/RainWritesCode/NomisKitchenHDT'
