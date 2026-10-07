# Préparation de la publication GitHub

Le dossier de sources exporté est indépendant du dossier de travail et de son historique Git. Ne pas téléverser le dossier parent utilisé pour les essais : il peut contenir des journaux, sauvegardes du jeu et chemins personnels.

1. Exécuter `bash scripts/build.sh`, puis `python3 scripts/test_installer.py` et `python3 scripts/package.py`.
2. Créer le dépôt GitHub de destination. Téléverser **uniquement le contenu du ZIP `-source.zip`** pour publier le code. Ce ZIP ne contient aucun `.git` ni identité Git locale. Pour une publication avec Git, choisir explicitement une identité de commit publique ou une adresse GitHub noreply avant de créer le premier commit.
3. Créer une release **préversion** `v0.2.0`, avec le texte de `RELEASE_NOTES.md`.
4. Joindre le ZIP ARM64, le ZIP de sources correspondant et `SHA256SUMS`. Fournir les sources avec le binaire Doorstop modifié fait partie des obligations de redistribution LGPL.
5. Vérifier les liens et garder les crédits Nomi’s Kitchen visibles dans la page du dépôt et dans la release.

Les archives sont créées sans attributs étendus macOS, sans Finder metadata, sans historique Git et sans dates personnelles. Le ZIP installable contient aussi les sources des composants modifiés. Les dépendances tierces et le module Nomi original sont téléchargés à l’installation, pas incorporés dans les archives.

L’audit automatique détecte les chemins de comptes usuels, l’identité locale, les clés privées et certains formats de jetons, mais ne remplace pas une relecture humaine. Les journaux, préférences Battle.net, `client.config` et sauvegardes sont exclus par construction. Les noms et contacts présents dans les notices amont appartiennent aux auteurs crédités et doivent être conservés.

Avant une diffusion large, faire tester le ZIP sur une autre installation Apple Silicon. Le chargement et les corrections en partie ont été observés sur un seul environnement ; le nouvel installateur portable est testé dans une installation factice et ne prétend pas avoir été validé sur plusieurs Mac.
