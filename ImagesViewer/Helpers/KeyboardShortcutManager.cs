using System.Windows;
using System.Windows.Input;

namespace ImagesViewer.Helpers
{
    public static class KeyboardShortcutManager
    {
        public static void Register(Window window, Action<Key> onKeyPressed)
        {
            window.PreviewKeyDown += (s, e) =>
            {
                onKeyPressed?.Invoke(e.Key);
            };
        }
    }
}
