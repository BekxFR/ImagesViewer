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
attachee au processus. Le `Console.WriteLine` restant, dans le constructeur,
n'aboutit nulle part a l'execution. Les traces passent par `Debug.WriteLine`,
lisible dans la fenetre Sortie de Visual Studio ou dans DebugView, et absent du
binaire en configuration Release.

## Architecture

Six fichiers de code portent tout le comportement.

| Fichier | Role |
|---|---|
| `ImagesViewer/MainWindow.xaml.cs` | etat complet de l'application |
| `ImagesViewer/MainWindow.xaml` | une `Image` centree et un bouton `Select` |
| `ImagesViewer/Helpers/ImageLoader.cs` | lecture du fichier puis decodage, hors du fil d'interface |
| `ImagesViewer/Helpers/ImageCache.cs` | cache LRU des images decodees et prechargees |
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

Le chemin d'affichage, depuis le 2026-08-28, tient en trois etapes que
`ShowImage` enchaine : `ImageCache` rend la tache deja en cours ou en lance une,
`ImageLoader.ReadAllAsync` lit le fichier en une passe sequentielle, puis
`ImageLoader.Decode` decode dans un `Task.Run` et `Freeze` le resultat pour qu'il
traverse les fils. `ShowImage` compare ensuite `_navigationToken` avant de peindre
: les chargements ne reviennent pas dans l'ordre ou ils partent, et sans cette
garde une image deja quittee recouvrirait la courante. Apres l'affichage,
`Prefetch` lance les voisins `+1`, `-1` et `+2`.

`ImageCache` n'a aucun verrou : il n'est touche que depuis le fil d'interface.
Une tache en echec n'est pas conservee, sinon une coupure passagere du partage
resterait collee a l'image jusqu'a la fermeture.

L'extension est codee en dur a trois endroits : le filtre de la boite de
dialogue, le motif `*.jpg` de `Directory.GetFiles`, et le controle
`EndsWith(".jpg")`. Les fichiers `.jpeg` ne sont donc jamais vus. Elargir les
formats suppose de traiter les trois.

## Ou vivent les images

En usage reel, le dossier choisi n'est pas sur la machine qui affiche : il est
servi par un partage Windows depuis un serveur interne, atteint par un chemin
UNC ou par un lecteur reseau monte. Une base de donnees interne tient les liens
vers ces images et les rend rapidement ; elle est hors de ce depot et n'est pas
la source de la lenteur constatee. Le code l'ignore completement, comme il ignore
le partage : aucune dependance reseau, aucun client HTTP, `Directory.GetFiles` et
l'ouverture du `FileStream` traversent SMB sans rien en savoir.

Ce que cela change, et qui n'est visible nulle part dans le code :

- Le cout d'affichage se decompose en deux parts que le partage separe nettement :
  le transfert des octets, proportionnel au poids du fichier et a la latence SMB,
  et le decodage JPEG, proportionnel au nombre de pixels. `ImageLoader` mesure les
  deux et les ecrit en `Debug.WriteLine`. Sans ce partage, toute optimisation
  future se decide sur ces deux nombres, pas sur une intuition.
- `Directory.GetFiles` reste sur le fil d'interface. Sur un dossier charge servi
  par le partage, le premier `Select` gele donc encore la fenetre. C'est ce que
  visent les blocs `await Task.Run` laisses en commentaire dans `LoadImage`.
- Le cache de `LoadImage` ne se rafraichit pas. Une image deposee sur le partage
  pendant que le dossier est ouvert reste invisible tant que l'on ne passe pas
  par un autre dossier. `ImageCache` a le meme defaut par construction : il ne
  revalide jamais une entree.
- `BitmapCacheOption.OnLoad` joue en faveur de ce montage : le fichier est lu
  entierement puis referme, donc aucun handle n'est tenu ouvert sur le partage
  pendant l'affichage. Ne pas le remplacer par `OnDemand`.

## Conventions propres a ce depot

Les identifiants, les commentaires generes par le modele de projet et les noms
de fichiers sont en anglais. Les chaines montrees a l'utilisateur, messages
d'erreur des `MessageBox` et libelles de filtre, sont en francais accentue.
Suivre cette repartition dans tout ajout.

Les fichiers issus du modele Visual Studio sont en UTF-8 avec BOM. Ne pas en
retirer un au passage d'une modification sans rapport : le diff porterait alors
sur tout le fichier. Les fichiers ecrits a la main en sont depourvus,
`AssemblyInfo.cs` comme `ImageLoader.cs` et `ImageCache.cs` : le controle de
typographie du poste refuse le BOM, et un fichier purement ASCII se lit de facon
identique avec ou sans. Un ajout qui contient du francais accentue, une chaine
d'interface par exemple, a besoin du BOM pour que le compilateur ne se trompe pas
d'encodage ; il faudra alors le commiter avec `--no-verify`.

Les fins de ligne sont en LF partout.

## Etat du travail

Au 2026-08-28 :

- `MainWindow` implemente `INotifyPropertyChanged` depuis le 2026-08-27.
  L'evenement et `OnPropertyChanged` existaient deja, l'interface manquait, donc
  WPF ne s'abonnait pas et le titre lie par `Title="{Binding WindowTitle}"`
  restait fige sur sa valeur initiale.
- Le chargement d'image est passe en asynchrone avec decodage a la taille de
  l'ecran, cache et prechargement, contre un temps d'affichage juge trop long en
  interne. `LoadImageWithCache` a disparu au profit de `ImageLoader`.
- `UpdateTitle` n'est appelee depuis nulle part.

Rien de tout cela n'a ete compile ni mesure : le poste courant est sous Linux et
la cible est `net8.0-windows`. La premiere execution sur Windows doit verifier
les nombres de `Debug.WriteLine` avant d'aller plus loin.

Pistes non tranchees, dans l'ordre ou elles se justifieront si le temps
d'affichage reste trop long :

- Une vignette EXIF affichee des les premiers kilo-octets lus, remplacee par
  l'image complete quand elle arrive : c'est du ressenti gagne, pas du temps.
- Des derivees reduites generees sur le serveur et rangees a cote des originaux,
  leur chemin ajoute a la base : c'est le seul levier qui reduise vraiment les
  octets transferes, et il se decide hors de ce depot.
- Un cache disque local sous `%LOCALAPPDATA%`, utile seulement si les memes
  images sont revisitees d'une session a l'autre.
- `Directory.GetFiles` hors du fil d'interface, cf. les blocs commentes de
  `LoadImage`.
