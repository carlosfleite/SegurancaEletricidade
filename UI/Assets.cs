using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace SegurancaEletricaEmCasa.UI
{
    public static class Assets
    {
        private static Icon _appIcon;
        private static bool _appIconLoaded;
        public static string FullPath(string relativePath) =>
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, relativePath);
        public static bool Exists(string relativePath) => File.Exists(FullPath(relativePath));
        public static Bitmap LoadBitmapOrNull(string relativePath)
        {
            string path = FullPath(relativePath);
            if (!File.Exists(path))
                return null;

            try
            {
                byte[] bytes = File.ReadAllBytes(path);
                using (var stream = new MemoryStream(bytes))
                using (var temp = new Bitmap(stream))
                {
                    return new Bitmap(temp);
                }
            }
            catch (ArgumentException)
            {
                return null; // arquivo presente, mas não é uma imagem válida
            }
        }

        //O mesmo ícone do .exe (Icone.ico), para aparecer na barra de título de todas as janelas.
        public static Icon AppIcon
        {
            get
            {
                if (!_appIconLoaded)
                {
                    _appIconLoaded = true;
                    try { _appIcon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); }
                    catch { _appIcon = null; }
                }
                return _appIcon;
            }
        }
    }
}
