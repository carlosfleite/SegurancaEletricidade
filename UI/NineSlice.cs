using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace SegurancaEletricaEmCasa.UI
{
    public static class NineSlice
    {
        //Renderiza o sprite em um Bitmap novo do tamanho pedido.
        public static Bitmap Render(Bitmap sprite, Size size, Padding slice)
        {
            var result = new Bitmap(Math.Max(1, size.Width), Math.Max(1, size.Height), PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(result))
            {
                Draw(g, sprite, new Rectangle(Point.Empty, result.Size), slice);
            }
            return result;
        }

        public static void Draw(Graphics g, Bitmap sprite, Rectangle dest, Padding slice)
        {
            g.InterpolationMode = InterpolationMode.NearestNeighbor;
            g.PixelOffsetMode = PixelOffsetMode.Half;

            int sw = sprite.Width, sh = sprite.Height;

            // Se o destino for menor que os cantos somados, reduz os cantos proporcionalmente.
            int l = slice.Left, r = slice.Right, t = slice.Top, b = slice.Bottom;
            if (l + r > dest.Width) { float k = dest.Width / (float)(l + r); l = (int)(l * k); r = dest.Width - l; }
            if (t + b > dest.Height) { float k = dest.Height / (float)(t + b); t = (int)(t * k); b = dest.Height - t; }

            int[] srcX = { 0, slice.Left, sw - slice.Right, sw };
            int[] srcY = { 0, slice.Top, sh - slice.Bottom, sh };
            int[] dstX = { dest.Left, dest.Left + l, dest.Right - r, dest.Right };
            int[] dstY = { dest.Top, dest.Top + t, dest.Bottom - b, dest.Bottom };

            // WrapMode.TileFlipXY impede que o GDI+ "puxe" pixels vizinhos (ou transparentes)
            // para dentro das bordas de cada pedaço, o que criaria linhas finas entre eles.
            using (var attributes = new ImageAttributes())
            {
                attributes.SetWrapMode(WrapMode.TileFlipXY);

                for (int row = 0; row < 3; row++)
                {
                    for (int col = 0; col < 3; col++)
                    {
                        int sx = srcX[col], sy = srcY[row];
                        int sWidth = srcX[col + 1] - sx, sHeight = srcY[row + 1] - sy;
                        var target = Rectangle.FromLTRB(dstX[col], dstY[row], dstX[col + 1], dstY[row + 1]);

                        if (sWidth <= 0 || sHeight <= 0 || target.Width <= 0 || target.Height <= 0)
                            continue;

                        g.DrawImage(sprite, target, sx, sy, sWidth, sHeight, GraphicsUnit.Pixel, attributes);
                    }
                }
            }
        }

        public static Region RegionFromAlpha(Bitmap bitmap, byte alphaThreshold = 128)
        {
            var region = new Region(Rectangle.Empty);
            var rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
            BitmapData data = bitmap.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

            try
            {
                int stride = data.Stride;
                var row = new byte[stride];

                for (int y = 0; y < bitmap.Height; y++)
                {
                    Marshal.Copy(data.Scan0 + y * stride, row, 0, stride);

                    int x = 0;
                    while (x < bitmap.Width)
                    {
                        while (x < bitmap.Width && row[x * 4 + 3] < alphaThreshold) x++;
                        int start = x;
                        while (x < bitmap.Width && row[x * 4 + 3] >= alphaThreshold) x++;

                        if (x > start)
                            region.Union(new Rectangle(start, y, x - start, 1));
                    }
                }
            }
            finally
            {
                bitmap.UnlockBits(data);
            }

            return region;
        }
    }
}
