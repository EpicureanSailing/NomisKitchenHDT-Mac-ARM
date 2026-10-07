#!/bin/bash
BASE="$(cd "$(dirname "$0")" && pwd -P)"
echo 'Nomi Dance — adaptation macOS ARM64 (expérimentale)'
echo 'Merci à Nomi’s Kitchen : https://github.com/RainWritesCode/NomisKitchenHDT'
echo 'Le module modifie le client ; son auteur indique que les conditions Blizzard ne le permettent pas.'
echo 'Fermer Hearthstone. Installation par défaut : /Applications/Hearthstone/Hearthstone.app'
echo 'Laisser Battle.net ouvert, avec ses mises à jour terminées, pendant installation et lancement.'
if /usr/bin/pgrep -x 'Battle[.]net' >/dev/null; then
    echo 'Battle.net est déjà ouvert.'
else
    result=$?
    if [[ "$result" != 1 ]]; then
        echo 'Impossible de vérifier Battle.net. Installation arrêtée.' >&2
        exit 1
    fi
    echo 'Ouverture de Battle.net…'
    if ! /usr/bin/open -a 'Battle.net'; then
        echo 'Battle.net est introuvable ou ne peut pas être ouvert. Installation arrêtée.' >&2
        exit 1
    fi
fi
echo 'Attendre que Battle.net soit prêt et que le bouton Jouer soit disponible pour Hearthstone.'
echo 'Ne pas lancer Hearthstone pendant l’installation. Laisser Battle.net ouvert ensuite.'
printf 'Quand Battle.net est prêt, appuyer sur Entrée, ou saisir le chemin complet vers Hearthstone.app : '
IFS= read -r app
/usr/bin/arch -arm64 /bin/bash "$BASE/manage.sh" install "${app:-/Applications/Hearthstone/Hearthstone.app}"
result=$?
printf '\nAppuyer sur Entrée pour fermer. '
IFS= read -r answer
exit "$result"
