# ScreenMover

**Vos fenêtres en un coup d’œil. Glissez-les vers un autre écran ou masquez-les.** Un outil visuel pour organiser les fenêtres sur plusieurs écrans sous Windows.

[简体中文](README.md) · [English](README.en.md) · [Deutsch](README.de.md) · [Français](README.fr.md) · [Español](README.es.md)

[Dernière version](https://github.com/lbyh0116/ScreenMover/releases/latest) · [Signaler un problème](https://github.com/lbyh0116/ScreenMover/issues/new/choose)

## Télécharger et lancer

Version stable actuelle : **v0.2.2 · Windows x64, portable**.

1. Téléchargez [ScreenMover-v0.2.2-win-x64.zip](https://github.com/lbyh0116/ScreenMover/releases/download/v0.2.2/ScreenMover-v0.2.2-win-x64.zip).
2. Extrayez toute l’archive, puis lancez `ScreenMover.exe`. Conservez tous les fichiers fournis dans leur dossier.
3. Avant une mise à niveau, quittez l’ancienne version depuis son icône dans la zone de notification. Les paramètres, l’ordre des fenêtres et les entrées de récupération sont conservés.

Les composants nécessaires sont inclus ; aucune installation séparée de .NET n’est requise. Le paquet ne dispose pas de signature de code commerciale. La version Microsoft Store n’est pas encore disponible. L’interface propose le chinois simplifié, l’anglais, l’allemand, le français et l’espagnol. Elle suit par défaut la langue d’affichage de Windows et utilise l’anglais si celle-ci n’est pas prise en charge. Vous pouvez changer de langue dans les paramètres, puis redémarrer l’application pour appliquer votre choix.

## Fonctionnalités

- **Vue d’ensemble :** aperçus fixes des écrans et des fenêtres dans une galerie. L’icône de l’application prend le relais lorsqu’une capture est impossible.
- **Déplacement par glisser-déposer :** déposez les fenêtres sur la vignette de l’écran cible. La sélection multiple, le déplacement groupé et l’annulation du dernier déplacement sont pris en charge.
- **Panneau rapide :** une liste compacte des fenêtres à côté des aperçus d’écran, accessible depuis la zone de notification. Un clic à l’extérieur le referme.
- **Masquage et récupération :** sur les versions de Windows compatibles, une fenêtre est déplacée vers un bureau virtuel dédié, nommé d’après son application, puis réduite si possible. Vous pouvez la récupérer depuis la fenêtre principale. Si l’application refuse de se réduire, le déplacement déjà effectué est conservé. Les retours manuels sont détectés automatiquement.
- **Personnalisation :** ordre des fenêtres mémorisé, thème clair ou sombre, couleur d’accentuation système ou personnalisée, raccourcis configurables et enregistrement automatique des paramètres.
- **Mises à jour intégrées :** recherche sur GitHub Releases, téléchargement et vérification du paquet adapté, puis installation et redémarrage.

Les aperçus sont des captures ponctuelles, pas un enregistrement continu. Par défaut, ils sont actualisés au lancement, lorsque ScreenMover redevient actif, toutes les minutes ou avec F5. Fermer la fenêtre principale laisse normalement l’application dans la zone de notification. Utilisez le menu de son icône pour la quitter complètement.

## Utilisation

| Action | Commande |
| --- | --- |
| Déplacer une fenêtre | La glisser sur la vignette d’un autre écran ou utiliser les boutons de déplacement |
| Sélection multiple / plage | Ctrl / Maj + clic |
| Activer une fenêtre | Double-clic sur sa vignette |
| Rechercher / tout sélectionner | Ctrl+F / Ctrl+A |
| Actualiser les aperçus | F5 |
| Annuler / fermer le panneau rapide | Échap ; un clic dans une zone vide efface la sélection |
| Ouvrir le panneau rapide / masquer la fenêtre active | Définir deux raccourcis globaux distincts dans les paramètres |

Dans le panneau rapide, déposer une fenêtre sur son écran actuel la masque ; la déposer sur un autre écran la déplace. Une indication sur la cible annonce l’action avant de relâcher la souris.

## Compatibilité

- **Windows 11 x64 :** effet Mica lorsqu’il est disponible. Le masquage sur un bureau virtuel dédié est actuellement activé uniquement sur les builds **26100 / 26200**.
- **Windows 10 22H2 x64 :** compatibilité de base prévue, avec l’effet Acrylique lorsque le système le permet. La validation sur des appareils Windows 10 reste à faire. Le masquage sur un bureau virtuel dédié est désactivé.
- **x86 / ARM64 :** aucun paquet disponible pour le moment.

Les fenêtres protégées, réduites, rendues par le GPU ou qui ne répondent plus peuvent ne pas fournir d’aperçu. Pour gérer des applications exécutées avec des droits élevés, il peut être nécessaire de relancer ScreenMover en tant qu’administrateur. Les configurations à plusieurs résolutions DPI, les branchements à chaud et les différents matériels graphiques nécessitent encore des tests. [Détails de compatibilité](docs/COMPATIBILITY.md) (en chinois).

## Mises à jour et données locales

Dans les paramètres, recherchez les mises à jour, puis choisissez de télécharger et d’installer. Les téléchargements sont vérifiés avec SHA-256. En cas d’échec du remplacement des fichiers, une restauration est tentée ; elle n’est pas garantie après une coupure de courant ou un plantage ultérieur de la nouvelle version. Les recherches sont lancées par l’utilisateur, jamais automatiquement en arrière-plan. [Détails des mises à jour](docs/UPDATES.md) (en chinois).

Les données locales sont stockées dans `%LOCALAPPDATA%\ScreenMover`. Les captures restent en mémoire et ne sont pas envoyées en ligne. Les journaux d’activité et les entrées de récupération peuvent contenir des titres de fenêtres. Les sauvegardes de mise à jour se trouvent dans le sous-dossier `updates`. [Confidentialité](docs/PRIVACY.md) (en chinois).

## Compiler le projet

Prérequis : Windows x64, **SDK .NET 10.0.400** et accès à NuGet.

```powershell
git clone https://github.com/lbyh0116/ScreenMover.git
cd ScreenMover
.\build.ps1 -PureTest
```

Résultat : `dist\ScreenMover\ScreenMover.exe`. Cette commande compile le projet, prépare le paquet et exécute les tests de régression sans interface, sans manipuler vos fenêtres. [Feuille de route](docs/ROADMAP.md) · [Tests manuels](docs/MANUAL_TEST.md) (en chinois).

## Licence et sécurité

Aucune licence open source n’a encore été choisie pour le code original. La mise à disposition publique du code ne vaut pas octroi d’une licence open source. Le code auxiliaire des bureaux virtuels provient de [MScholtes/VirtualDesktop](https://github.com/MScholtes/VirtualDesktop) et conserve sa [licence MIT](https://github.com/lbyh0116/ScreenMover/blob/main/ScreenMover/Helpers/VirtualDesktop-LICENSE.txt). Les autres dépendances conservent leurs propres licences.

Pour une faille de sécurité, utilisez le [signalement privé](https://github.com/lbyh0116/ScreenMover/security/advisories/new). Pour les autres problèmes, ouvrez une issue GitHub. [Notes de la dernière version](CHANGELOG.md) (en chinois et en anglais).
