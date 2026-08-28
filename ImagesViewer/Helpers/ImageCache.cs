using System.Diagnostics;
using System.Windows.Media.Imaging;

namespace ImagesViewer.Helpers
{
    // Garde les dernieres images decodees et celles qui viennent. Appele depuis le fil
    // d'interface uniquement, d'ou l'absence de verrou.
    public sealed class ImageCache
    {
        private readonly Dictionary<string, Task<BitmapSource>> _entries = new();
        private readonly LinkedList<string> _order = new();
        private readonly int _capacity;
        private readonly int _decodePixelWidth;

        public ImageCache(int capacity, int decodePixelWidth)
        {
            _capacity = capacity;
            _decodePixelWidth = decodePixelWidth;
        }

        public Task<BitmapSource> GetAsync(string imagePath)
        {
            if (_entries.TryGetValue(imagePath, out var cached))
            {
                // Un echec garde en cache masquerait une reprise du partage.
                if (!cached.IsFaulted)
                {
                    Touch(imagePath);
                    return cached;
                }
                Forget(imagePath);
            }

            var loading = ImageLoader.LoadAsync(imagePath, _decodePixelWidth);
            _entries[imagePath] = loading;
            _order.AddFirst(imagePath);
            Evict();
            return loading;
        }

        public void Prefetch(string imagePath)
        {
            _ = GetAsync(imagePath).ContinueWith(
                static task => Debug.WriteLine($"Prechargement abandonne : {task.Exception?.GetBaseException().Message}"),
                TaskContinuationOptions.OnlyOnFaulted);
        }

        private void Touch(string imagePath)
        {
            _order.Remove(imagePath);
            _order.AddFirst(imagePath);
        }

        private void Forget(string imagePath)
        {
            _entries.Remove(imagePath);
            _order.Remove(imagePath);
        }

        private void Evict()
        {
            while (_order.Count > _capacity)
            {
                var oldest = _order.Last!.Value;
                _order.RemoveLast();
                _entries.Remove(oldest);
            }
        }
    }
}
