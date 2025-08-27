# ImagesViewer

Une application de visualisation d'images légère et simple développée en WPF pour Windows.

## Description

ImagesViewer est une application .NET 8.0 WPF qui permet de parcourir et visualiser facilement des images JPEG dans un répertoire. L'application offre une interface minimaliste avec navigation par raccourcis clavier pour une expérience utilisateur fluide.

## Fonctionnalités

- **Visualisation d'images JPEG** : Affichage optimisé des fichiers .jpg
- **Sélection de répertoire** : Choisissez une image et l'application charge automatiquement toutes les images du dossier
- **Navigation par clavier** :
  - **Q** : Image précédente
  - **D** : Image suivante
- **Tri naturel** : Les images sont triées dans l'ordre naturel des noms de fichiers
- **Interface simple** : Interface épurée centrée sur l'affichage des images
- **Performance optimisée** : Mise en cache des images pour un affichage fluide

## Prérequis

- Windows 10/11
- .NET 8.0 Runtime
- Framework WPF (inclus avec .NET)

## Installation

1. Clonez le repository :

   ```
   git clone https://github.com/BekxFR/ImagesViewer.git
   ```

2. Ouvrez le projet dans Visual Studio 2022 ou utilisez la ligne de commande :

   ```
   cd ImagesViewer
   dotnet build
   ```

3. Exécutez l'application :
   ```
   dotnet run --project ImagesViewer
   ```

## Utilisation

1. **Démarrage** : Lancez l'application ImagesViewer
2. **Sélection** : Cliquez sur le bouton "Select" pour choisir une image JPEG
3. **Navigation** :
   - Utilisez la touche **Q** pour aller à l'image précédente
   - Utilisez la touche **D** pour aller à l'image suivante
4. **Titre** : Le nom du fichier actuel s'affiche dans la barre de titre

## Structure du projet

```
ImagesViewer/
├── ImagesViewer.csproj          # Configuration du projet .NET 8.0
├── MainWindow.xaml              # Interface utilisateur WPF
├── MainWindow.xaml.cs           # Logique principale de l'application
├── App.xaml                     # Configuration de l'application
├── App.xaml.cs                  # Point d'entrée de l'application
└── Helpers/
    ├── KeyboardShortcutManager.cs   # Gestionnaire des raccourcis clavier
    └── NaturalStringComparer.cs     # Comparateur pour tri naturel
```

## Technologies utilisées

- **.NET 8.0** : Framework principal
- **WPF (Windows Presentation Foundation)** : Interface utilisateur
- **C#** : Langage de programmation
- **XAML** : Markup pour l'interface utilisateur

## Formats supportés

- **JPEG** (.jpg) - Format principal supporté

## Raccourcis clavier

| Touche | Action           |
| ------ | ---------------- |
| Q      | Image précédente |
| D      | Image suivante   |

## Contribuer

Les contributions sont les bienvenues ! N'hésitez pas à :

- Signaler des bugs
- Proposer de nouvelles fonctionnalités
- Soumettre des pull requests

## Licence

Ce projet est sous licence [LICENSE](LICENSE).
