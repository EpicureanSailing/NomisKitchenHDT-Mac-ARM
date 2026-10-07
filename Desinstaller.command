#!/bin/bash
BASE="$(cd "$(dirname "$0")" && pwd -P)"
echo 'Fermer Hearthstone avant la désinstallation.'
printf 'Appuyer sur Entrée, ou saisir le chemin complet vers Hearthstone.app : '
IFS= read -r app
/usr/bin/arch -arm64 /bin/bash "$BASE/manage.sh" uninstall "${app:-/Applications/Hearthstone/Hearthstone.app}"
result=$?
printf '\nAppuyer sur Entrée pour fermer. '
IFS= read -r answer
exit "$result"
