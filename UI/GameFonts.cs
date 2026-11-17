using System;
using System.Drawing;
using System.Drawing.Text;
using System.IO;
using System.Runtime.InteropServices;

namespace SegurancaEletricaEmCasa.UI
{
    public static class GameFonts
    {
        // O .ttf existe em dois lugares no projeto (raiz e pasta Fonts); aceita os dois.
        private static readonly string[] FontPaths = { "Fonts/GameFont.ttf", "GameFont.ttf" };
        private const string FallbackFamily = "Segoe UI";
        private const float FallbackScale = 1.6f;

        private static readonly PrivateFontCollection Collection = new PrivateFontCollection();
        private static readonly FontFamily CustomFamily;

        //True quando a fonte customizada foi encontrada e carregada com sucesso.
        public static bool UsingCustomFont { get; }

        [DllImport("gdi32.dll")]
        private static extern IntPtr AddFontMemResourceEx(IntPtr pbFont, uint cbFont, IntPtr pdv, ref uint pcFonts);

        static GameFonts()
        {
            foreach (string relativePath in FontPaths)
            {
                string path = Assets.FullPath(relativePath);
                if (!File.Exists(path))
                    continue;

                try
                {
                    byte[] data = File.ReadAllBytes(path);

                    // A memória precisa continuar válida enquanto a fonte estiver em uso (a vida
                    // inteira do programa), por isso ela nunca é liberada.
                    IntPtr memory = Marshal.AllocCoTaskMem(data.Length);
                    Marshal.Copy(data, 0, memory, data.Length);

                    Collection.AddMemoryFont(memory, data.Length);
                    uint installed = 0;
                    AddFontMemResourceEx(memory, (uint)data.Length, IntPtr.Zero, ref installed);

                    if (Collection.Families.Length > 0)
                    {
                        CustomFamily = Collection.Families[0];
                        UsingCustomFont = true;
                        return;
                    }
                }
                catch
                {
                    // Arquivo presente mas inválido/corrompido: tenta o próximo ou cai no fallback.
                }
            }

            UsingCustomFont = false;
        }

        public static Font GetReadable(float size, FontStyle style = FontStyle.Regular)
        {
            return new Font(FallbackFamily, size, style);
        }

        public static Font Get(float size, FontStyle style = FontStyle.Regular)
        {
            if (!UsingCustomFont)
                return new Font(FallbackFamily, size * FallbackScale, style);

            if (!CustomFamily.IsStyleAvailable(style))
                style = FontStyle.Regular;

            return new Font(CustomFamily, size, style);
        }
    }
}
