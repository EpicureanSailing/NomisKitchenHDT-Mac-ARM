# Validation de v0.2.0

- Compilation des trois composants : ARM64 pour le relais et Doorstop, IL .NET pour BepInEx.Preloader. Compilation finale .NET : zéro avertissement, zéro erreur.
- Vérification des signatures ad hoc du relais et de la bibliothèque Doorstop.
- Treize tests automatisés réussis dans une application factice : installation/restauration avec vérification de l’exécutable original, nettoyage des restes lorsque l’original a déjà été restauré, transmission des arguments et chemins relatifs avec espaces, désactivation temporaire, refus d’installation existante, protection d’un exécutable mis à jour, refus d’une sauvegarde altérée, refus de processus actifs ou de liste de processus inaccessible, refus de version Unity différente, vérification du payload et des téléchargements, deux retours arrière après échec de transaction, refus si le jeu démarre pendant la préparation, détection de données locales par l’audit.
- Audit des sources et des archives, y compris les chaînes UTF-8/UTF-16 des binaires. Export par liste explicite de fichiers autorisés, sans attributs étendus macOS, journaux, sauvegardes, compte Battle.net ou historique Git.

Le montage initial du même chargeur avec le module d’origine a été testé en situation réelle sur Apple Silicon : lancement via Battle.net, sept patches appliqués, interventions de correction de positions en partie. Le nouvel installateur portable a été testé sur une application factice ; il n’a pas encore été validé en partie sur un second Mac. Pas de mesure de performance ou de test de stabilité prolongé.

Les tests n’arrêtent aucun processus de jeu et ne modifient aucune installation réelle. La découverte des processus est simulée dans une copie temporaire du script ; les autres opérations d’installation, vérification, exécution du relais et restauration sont exécutées sur les fichiers factices.

Indicateur compilé en .NET Framework 4.7.2 contre les références locales Unity, sans redistribuer ces dernières. Son affichage a été confirmé dans le jeu réel après installation du paquet v0.2.0 via Battle.net déjà ouvert. Le journal confirme ARM64, le chargement de Nomi Dance Status et « Nomi Dance actif · 7/7 », sans erreur dans le journal BepInEx consulté.

Quatre tests supplémentaires du point d’entrée passent : ouverture de Battle.net fermé, conservation de Battle.net déjà ouvert, arrêt si l’ouverture échoue, arrêt si la découverte des processus échoue. Aucune app réelle n’est ouverte par ces tests.
