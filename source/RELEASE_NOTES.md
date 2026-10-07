# v0.2.0 — expérimental, macOS ARM64

Ajout d’un indicateur non interactif en haut à droite : vert uniquement si le module est activé et ses patches présents sur les sept méthodes attendues. États partiel, désactivé, inconnu et non chargé distingués. Si le chargeur ne démarre pas, aucune marque ne s’affiche : le chargement reste non confirmé. Affichage confirmé en jeu après installation : le journal confirme ARM64 et « Nomi Dance actif · 7/7 ».

Installer en gardant Battle.net ouvert et ses mises à jour terminées : une vérification ultérieure peut remettre l’exécutable d’origine.
L’installateur ouvre Battle.net si nécessaire, puis attend que l’utilisateur confirme que le bouton Jouer est disponible avant de modifier le jeu.

Chargement natif de Nomi Can't Dance sur Apple Silicon, via le bouton Jouer de Battle.net. Installation et désinstallation avec sauvegarde de l’exécutable original et vérifications SHA-256. Dépendances téléchargées depuis des versions amont figées. Aucune donnée de compte, aucun journal de partie et aucun fichier personnel inclus.

Le désinstallateur nettoie également les restes si l’exécutable actif est déjà exactement l’original sauvegardé, sans le remplacer. Une version différente reste protégée contre l’écrasement.

**Merci à Nomi’s Kitchen / RainWritesCode pour le correctif original !** Projet source : **https://github.com/RainWritesCode/NomisKitchenHDT**. Le module est utilisé sans modification ; cette release fournit une adaptation macOS indépendante.

Validé sur une configuration Apple Silicon avec Unity 6000.3.11f1 : lancement via Battle.net, sept patches chargés et corrections observées en partie. Les autres versions Unity sont refusées par l’installateur. Stabilité prolongée et performances non garanties. Paquet non notarisé ; l’application Hearthstone est temporairement modifiée. L’auteur du module indique que les conditions Blizzard ne permettent pas la modification du client.

Archives : `NomiDance-macOS-arm64-v0.2.0.zip` (installateur), `NomiDance-macOS-arm64-v0.2.0-source.zip` (sources), `SHA256SUMS` (empreintes).
