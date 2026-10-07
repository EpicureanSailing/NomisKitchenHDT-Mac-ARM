# Nomi Dance pour macOS ARM64

Adaptation expérimentale permettant de charger **Nomi Can't Dance** dans Hearthstone natif sur Apple Silicon, tout en lançant le jeu depuis **Battle.net → Jouer**. HSTracker peut rester utilisé à côté ; ce paquet ne s’installe pas dans HSTracker.

**Merci à Nomi’s Kitchen / RainWritesCode pour le correctif original !** Retrouvez leur projet ici : **[NomisKitchenHDT sur GitHub](https://github.com/RainWritesCode/NomisKitchenHDT)**. La DLL Nomi Can't Dance est téléchargée depuis ce dépôt et utilisée sans modification. Ce projet indépendant fournit uniquement l’adaptation du chargement et l’installateur macOS ARM64 ; il n’est pas une publication officielle de Nomi’s Kitchen ou de Blizzard.

## État

Un essai sur Apple Silicon avec Unity **6000.3.11f1** a confirmé le lancement depuis Battle.net, le chargement natif ARM64, l’application des sept patches et des corrections de positions pendant une partie. Les animations ordinaires restent présentes. Aucun benchmark ni test prolongé ne garantit la stabilité ou les performances sur toutes les configurations.

L’auteur du module avertit que celui-ci modifie le client et que les conditions de Blizzard ne permettent pas cette modification. Ce paquet ne constitue pas une autorisation de Blizzard.

## Installation

1. Utiliser un Mac Apple Silicon. Ouvrir **Battle.net**, laisser ses mises à jour et vérifications se terminer, puis **fermer Hearthstone en laissant Battle.net ouvert**. Une vérification après installation peut restaurer l’exécutable original et retirer le relais.
2. Télécharger le ZIP ARM64 de la release, puis l’extraire entièrement. Une connexion Internet est nécessaire pour récupérer les dépendances amont.
3. Ouvrir `Installer.command`. Il ouvre Battle.net si nécessaire. **Attendre que ses mises à jour soient terminées et que le bouton Jouer soit disponible pour Hearthstone**, puis valider dans l’installateur, sans démarrer le jeu. Le script ne peut pas confirmer automatiquement la fin des mises à jour. Le chemin proposé est `/Applications/Hearthstone/Hearthstone.app` ; saisir un autre chemin si nécessaire, sans guillemets ni échappements.
4. Attendre le message de réussite, puis lancer le jeu avec **Jouer dans Battle.net**.

Le paquet est signé localement avec une signature ad hoc, **sans signature Developer ID ni notarisation Apple**. macOS peut demander une autorisation dans les réglages de confidentialité et sécurité. Ne pas désactiver globalement Gatekeeper. Aucun mot de passe de compte Battle.net n’est demandé par l’installateur.

L’installation refuse une version Unity différente de celle testée, un jeu en cours d’exécution, une installation déjà modifiée ou des fichiers dont les empreintes ne correspondent pas. Elle n’utilise pas `sudo`. Si le dossier du jeu n’est pas accessible en écriture, vérifier ses permissions dans Finder.

## Ce qui change

L’exécutable `Hearthstone.app/Contents/MacOS/Hearthstone` est sauvegardé juste à côté sous `Hearthstone.nomi-original`, puis remplacé par un petit relais ARM64. Le relais conserve les arguments et le contexte de Battle.net et ajoute le chargement de Doorstop avant d’exécuter l’original. Le runtime est rangé dans `.nomi-dance-arm64`, à côté de `Hearthstone.app`. Aucune DLL du jeu n’est remplacée.

Cette opération change le contenu couvert par la signature de l’application Hearthstone. Une mise à jour ou une réparation Battle.net peut enlever le relais. Ne pas réinstaller par-dessus un état inattendu : les scripts s’arrêtent plutôt que d’écraser une mise à jour ou une sauvegarde différente.

Après installation, le dossier téléchargé peut être déplacé ; le jeu utilise uniquement le runtime placé à côté de l’application. Conserver le ZIP pour disposer du désinstallateur.

## Désinstallation et diagnostic

Un indicateur discret apparaît en haut à droite du jeu. **Vert « Nomi Dance actif · 7/7 »** signifie que le module est chargé, sa configuration l’active et les sept méthodes attendues portent des patches provenant de son assemblage. Jaune indique un état partiel, désactivé ou inconnu ; rouge indique que le module n’est pas chargé. La vérification se répète toutes les deux secondes et ne journalise que les changements d’état. L’indicateur ne capte ni clics ni clavier.

Si le chargeur lui-même ne démarre pas, l’indicateur ne peut pas apparaître : **absence de marque = chargement non confirmé**. Le vert confirme l’installation des patches, pas un résultat garanti pour chaque animation. Le plugin d’indication est indépendant ; la DLL Nomi originale reste inchangée. Pour masquer la marque, régler `Visible = false` dans la section `[Display]` de `BepInEx/config/org.nomidance.arm64.status.cfg`.

Fermer Hearthstone, puis ouvrir `Desinstaller.command` avec le même chemin d’application. L’exécutable original est restauré après vérification SHA-256. Le runtime, sa configuration et ses journaux sont supprimés. Si le jeu a été mis à jour entre-temps, la restauration est refusée afin de préserver la nouvelle version.

Pour désactiver temporairement le chargement, créer un fichier vide `.nomi-dance-arm64/disabled` puis redémarrer le jeu. Le relais lancera l’original sans ajouter Doorstop. Supprimer ce fichier pour réactiver le chargement.

La configuration du module se trouve dans `.nomi-dance-arm64/BepInEx/config/com.community.hs.NomiCantDance.cfg`. `Enabled = true` active le correctif ; `LogFixes = false` évite les traces détaillées de partie par défaut. Pour un diagnostic ponctuel, passer `LogFixes` à `true`.

Le journal local est `.nomi-dance-arm64/BepInEx/LogOutput.log`. Il peut contenir des informations de partie ou des chemins locaux. **Ne pas publier de journaux bruts, de `client.config`, de jetons ou de fichiers Battle.net.** Ce projet n’intègre aucune télémétrie ; les téléchargements utilisent les serveurs amont indiqués dans `installer/downloads.sh`.

## Compilation et publication

Pour compiler l’indicateur, les références Unity sont lues dans l’installation locale de Hearthstone. Si elle se trouve ailleurs, définir `UNITY_MANAGED_DIR` vers `Hearthstone.app/Contents/Resources/Data/Managed`. Les DLL du jeu ne sont pas intégrées aux archives.

Les sources sont fournies, y compris celles du chargeur modifié. La compilation nécessite un Mac Apple Silicon, les outils en ligne de commande Xcode, .NET SDK 8 et Python 3 pour créer/auditer les archives.

Dans le ZIP installable, elles se trouvent dans `source/` : se placer dans ce dossier pour utiliser les commandes ci-dessous. Le ZIP de sources possède directement cette structure à sa racine.

```sh
bash scripts/build.sh
python3 scripts/test_installer.py
python3 scripts/test_entrypoint.py
python3 scripts/package.py
```

`build.sh` télécharge les dépendances figées et compile le relais, Doorstop et BepInEx.Preloader. `package.py` utilise une liste explicite de fichiers autorisés ; il produit un ZIP installable, un ZIP des sources et leurs SHA-256 dans `dist/`, après audit des chemins personnels et fichiers interdits. Les journaux et sauvegardes locales ne font jamais partie de cette liste. Voir [les instructions de publication](docs/PUBLISHING.md) et [les crédits détaillés](THIRD_PARTY.md).
