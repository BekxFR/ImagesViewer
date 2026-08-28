using ImagesViewer.Helpers;
using Microsoft.Win32;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace ImagesViewer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private string _windowTitle = "ImagesViewer";
        private int _currentImageIndex = -1;
        private string _imagesDirectory = "";
        private string _oldImagesDirectory = "";
        private string _imageName = "";
        private List<string> _imagesFilesList = new();
        private ImageCache? _imageCache;
        private int _navigationToken;

        // Assez d'images pour couvrir un aller-retour autour de la position courante sans
        // relire le partage. Chaque entree tient la taille de l'ecran en memoire.
        private const int CacheCapacity = 7;
        // La lecture suivante est presque toujours la suivante dans la liste.
        private static readonly int[] PrefetchOffsets = { 1, -1, 2 };

        private ImageCache Cache => _imageCache ??= new ImageCache(CacheCapacity, GetDisplayPixelWidth());
        public string WindowTitle
        {
            get => _windowTitle;
            set
            {
                if (string.IsNullOrEmpty(value) || _windowTitle == value)
                    return;
                _windowTitle = value;
                OnPropertyChanged();
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            Console.WriteLine("ImagesViewer - Application de visualisation d'images JPEG");

            KeyboardShortcutManager.Register(this, OnShortcutPressed);
        }

        private void LoadImage()
        {
            if (_oldImagesDirectory != "" && _imagesDirectory == _oldImagesDirectory) return;

            _oldImagesDirectory = _imagesDirectory;
            _imagesFilesList.Clear();
            if (Directory.Exists(_imagesDirectory))
            {
                _imagesFilesList = Directory.GetFiles(_imagesDirectory, "*.jpg")
                                            .OrderBy(static f => System.IO.Path.GetFileName(f), new NaturalStringComparer())
                                            .ToList();

            }
            //await Task.Run(() => {
            //    _imagesFilesList = Directory.GetFiles(_imagesDirectory, "*.jpg")
            //        .OrderBy(f => System.IO.Path.GetFileName(f), new NaturalStringComparer())
            //        .ToList();
            //});

            //if (string.IsNullOrEmpty(_imagesDirectory))
            //    return;
            //var files = System.IO.Directory.GetFiles(_imagesDirectory, "*.jpg");
            //if (files.Length == 0)
            //    return;
            //_imagesFilesList.AddRange(files);
            if (_imagesFilesList.Count == 0)
            {
                MessageBox.Show("Aucune image JPEG trouvée dans le dossier sélectionné.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            _currentImageIndex = 0;
        }

        private void OpenFile_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Fichiers JPEG (*.jpg)|*.jpg"
            };

            if (dialog.ShowDialog() == true)
            {
                if (!File.Exists(dialog.FileName))
                {
                    MessageBox.Show("Le fichier sélectionné n'existe pas.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                if (!dialog.FileName.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase))
                {
                    MessageBox.Show("Le fichier sélectionné n'est pas un fichier JPEG valide.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                var newFolder = Path.GetDirectoryName(dialog.FileName) ?? string.Empty;
                _imageName = System.IO.Path.GetFileNameWithoutExtension(dialog.FileName);
                if (_imagesDirectory != newFolder)
                {
                    _imagesDirectory = newFolder;
                    LoadImage();
                }

                _currentImageIndex = _imagesFilesList.FindIndex(f => System.IO.Path.GetFileNameWithoutExtension(f) == _imageName);

                ShowImage(_currentImageIndex);
            }
        }

        private int GetDisplayPixelWidth()
        {
            // L'ecran borne ce qui sera visible : au-dela, les pixels decodes sont perdus.
            var scale = VisualTreeHelper.GetDpi(this).DpiScaleX;
            return (int)Math.Ceiling(SystemParameters.PrimaryScreenWidth * scale);
        }

        private async void ShowImage(int index)
        {
            if (index < 0 || index >= _imagesFilesList.Count)
                return;
            var imagePath = _imagesFilesList[index];
            var fileName = System.IO.Path.GetFileNameWithoutExtension(imagePath);
            _currentImageIndex = index;
            WindowTitle = string.IsNullOrEmpty(fileName) ? "ImagesViewer" : $"ImagesViewer - {fileName}";

            // Les chargements ne se terminent pas dans l'ordre ou ils partent : seule la
            // derniere image demandee doit etre peinte, sinon une image deja quittee
            // vient recouvrir la courante.
            var token = ++_navigationToken;
            try
            {
                var bitmap = await Cache.GetAsync(imagePath);
                if (token != _navigationToken)
                    return;
                BroadcastedImage.Source = bitmap;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erreur chargement image : {ex}");
                if (token != _navigationToken)
                    return;
                MessageBox.Show($"Impossible d'afficher l'image {fileName}.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            Prefetch(index);
        }

        private void Prefetch(int index)
        {
            foreach (var offset in PrefetchOffsets)
            {
                var neighbour = index + offset;
                if (neighbour >= 0 && neighbour < _imagesFilesList.Count)
                    Cache.Prefetch(_imagesFilesList[neighbour]);
            }
        }

        private void OnShortcutPressed(Key key)
        {
            if (key == Key.D && _currentImageIndex < _imagesFilesList.Count - 1)
            {
                ShowImage(_currentImageIndex + 1);
            }
            if (key == Key.Q && _currentImageIndex > 0)
            {
                ShowImage(_currentImageIndex - 1);
            }
        }

        public void UpdateTitle(string newTitle)
        {
            WindowTitle = string.IsNullOrEmpty(newTitle) ? "ImagesViewer" : $"ImagesViewer - {newTitle}";
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    }
}