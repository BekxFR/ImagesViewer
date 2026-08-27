# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

Visionneuse d'images JPEG en WPF (.NET 8), Windows uniquement : on choisit un
fichier, l'application liste le dossier qui le contient et on circule dedans au
clavier. Le projet cible `net8.0-windows` avec `UseWPF`, donc il ne se construit
ni ne se lance sur le poste Linux courant ; toute verification de compilation
demande une machine Windows.

## Construire et lancer

Depuis la racine du depot, sous Windows avec le SDK .NET 8 :

```
dotnet build ImagesViewer.sln
dotnet run --project ImagesViewer
```

[a verifier] Ces deux commandes viennent du README et n'ont pas ete lancees :
`dotnet` est absent du poste 42, et la cible `-windows` echouerait de toute
facon. La solution s'ouvre aussi directement dans Visual Studio 2022.

`OutputType` vaut `WinExe` depuis le commit 53e9e15 : aucune console n'est
attachee au processus. Les `Console.WriteLine` du code, dont celui du bloc
`catch` de `ShowImage`, n'aboutissent nulle part a l'execution. Pour tracer
quelque chose, passer par `Debug.WriteLine` ou une `MessageBox`.

## Architecture

Quatre fichiers de code portent tout le comportement.

| Fichier | Role |
|---|---|
| `ImagesViewer/MainWindow.xaml.cs` | etat complet de l'application |
| `ImagesViewer/MainWindow.xaml` | une `Image` centree et un bouton `Select` |
| `ImagesViewer/Helpers/KeyboardShortcutManager.cs` | branche `PreviewKeyDown` sur un `Action<Key>` |
| `ImagesViewer/Helpers/NaturalStringComparer.cs` | tri par segments alternes chiffres / non-chiffres |

`MainWindow` tient l'etat dans ses champs prives : `_imagesFilesList` la liste
triee des chemins, `_currentImageIndex` la position, `_imagesDirectory` et
`_oldImagesDirectory` le dossier courant et le precedent. Il n'y a ni ViewModel
ni service : le flux va du clic ou de la touche directement aux champs.

`LoadImage` sort immediatement quand `_imagesDirectory` est egal a
`_oldImagesDirectory` : c'est le cache qui evite de relister un dossier deja
parcouru. Ajouter un chemin qui modifie la liste sans changer le dossier
demande de casser cette garde, sinon la nouvelle liste n'est jamais construite.

`NaturalStringComparer` est declare dans l'espace de noms global malgre son
emplacement sous `Helpers/`, d'ou l'usage sans qualification dans `MainWindow`.

L'extension est codee en dur a trois endroits : le filtre de la boite de
dialogue, le motif `*.jpg` de `Directory.GetFiles`, et le controle
`EndsWith(".jpg")`. Les fichiers `.jpeg` ne sont donc jamais vus. Elargir les
formats suppose de traiter les trois.

## Ou vivent les images

En usage reel, le dossier choisi n'est pas sur la machine qui affiche : il est
servi par un partage Windows depuis une machine distante, atteint par un chemin
UNC ou par un lecteur reseau monte. Le code l'ignore completement. Il n'y a
aucune dependance reseau, aucun client HTTP : `Directory.GetFiles` et l'URI
`file://` de `LoadImageWithCache` traversent SMB sans rien en savoir.

Ce que cela change, et qui n'est visible nulle part dans le code :

- La lecture se fait sur le fil d'interface. Un partage lent ou une reprise de
  connexion gele la fenetre le temps du `LoadImageWithCache`. C'est ce que
  visent les blocs `await Task.Run` laisses en commentaire dans `LoadImage`.
- Une coupure du partage tombe dans le `catch` de `ShowImage`, qui tente une
  seconde lecture du meme chemin distant puis laisse remonter l'echec. Sa trace
  `Console.WriteLine` ne s'affiche nulle part depuis le passage en `WinExe`.
- Le cache de `LoadImage` ne se rafraichit pas. Une image deposee sur le partage
  pendant que le dossier est ouvert reste invisible tant que l'on ne passe pas
  par un autre dossier.
- `BitmapCacheOption.OnLoad` joue en faveur de ce montage : le fichier est lu
  entierement puis referme, donc aucun handle n'est tenu ouvert sur le partage
  pendant l'affichage. Ne pas le remplacer par `OnDemand`.

## Conventions propres a ce depot

Les identifiants, les commentaires generes par le modele de projet et les noms
de fichiers sont en anglais. Les chaines montrees a l'utilisateur, messages
d'erreur des `MessageBox` et libelles de filtre, sont en francais accentue.
Suivre cette repartition dans tout ajout.

Les fichiers issus du modele Visual Studio sont en UTF-8 avec BOM, seul
`AssemblyInfo.cs` en est depourvu. Les fins de ligne sont en LF partout. Ne pas
retirer un BOM au passage d'une modification sans rapport : le diff porterait
alors sur tout le fichier.

## Etat du travail

Au 2026-08-27 :

- `MainWindow` implemente `INotifyPropertyChanged` depuis ce jour. L'evenement
  et `OnPropertyChanged` existaient deja, l'interface manquait, donc WPF ne
  s'abonnait pas et le titre lie par `Title="{Binding WindowTitle}"` restait
  fige sur sa valeur initiale. Correction non compilee, faute de Windows.
- `UpdateTitle` n'est appelee depuis nulle part.

`MainWindow.xaml.cs` contient encore trois blocs commentes d'anciennes tentatives
de chargement, dont une version `await Task.Run`. Ils marquent la piste du
chargement asynchrone, pas encore tranchee, que la lecture a travers le partage
rend la plus utile des evolutions en attente.
