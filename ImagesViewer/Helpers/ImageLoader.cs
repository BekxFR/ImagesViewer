using System.Diagnostics;
using System.IO;
using System.Windows.Media.Imaging;

namespace ImagesViewer.Helpers
{
    public static class ImageLoader
    {
        private const int ReadBufferSize = 1 << 16;
        private const int MaxPreallocatedSize = 128 * 1024 * 1024;

        public static async Task<BitmapSource> LoadAsync(string imagePath, int decodePixelWidth)
        {
            var watch = Stopwatch.StartNew();
            var buffer = await ReadAllAsync(imagePath).ConfigureAwait(false);
            var readMs = watch.ElapsedMilliseconds;

            var bitmap = await Task.Run(() => Decode(buffer, decodePixelWidth)).ConfigureAwait(false);

            Debug.WriteLine($"{Path.GetFileName(imagePath)} : {buffer.Length / 1024} Ko, "
                + $"lecture {readMs} ms, decodage {watch.ElapsedMilliseconds - readMs} ms, "
                + $"{bitmap.PixelWidth}x{bitmap.PixelHeight}");
            return bitmap;
        }

        private static async Task<MemoryStream> ReadAllAsync(string imagePath)
        {
            // Une seule passe sequentielle sur le partage. Laisser le decodeur lire l'URI
            // lui-meme le fait aller et venir dans le fichier, et chaque va-et-vient coute
            // un aller-retour reseau.
            await using var source = new FileStream(imagePath, FileMode.Open, FileAccess.Read,
                FileShare.ReadWrite, ReadBufferSize, FileOptions.Asynchronous | FileOptions.SequentialScan);

            var buffer = new MemoryStream((int)Math.Clamp(source.Length, ReadBufferSize, MaxPreallocatedSize));
            await source.CopyToAsync(buffer, ReadBufferSize).ConfigureAwait(false);
            buffer.Position = 0;
            return buffer;
        }

        private static BitmapSource Decode(MemoryStream buffer, int decodePixelWidth)
        {
            var sourceWidth = ReadSourceWidth(buffer);

            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.StreamSource = buffer;
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
            // Le decodeur JPEG sait sauter directement a une echelle reduite, ce qui coute
            // bien moins que decoder tous les pixels pour que le rendu les jette ensuite.
            // Sous la taille source uniquement : au-dessus il agrandirait pour rien.
            if (decodePixelWidth > 0 && decodePixelWidth < sourceWidth)
                bitmap.DecodePixelWidth = decodePixelWidth;
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
        }

        private static int ReadSourceWidth(MemoryStream buffer)
        {
            buffer.Position = 0;
            var decoder = BitmapDecoder.Create(buffer,
                BitmapCreateOptions.DelayCreation | BitmapCreateOptions.IgnoreColorProfile,
                BitmapCacheOption.None);
            var width = decoder.Frames[0].PixelWidth;
            buffer.Position = 0;
            return width;
        }
    }
}
