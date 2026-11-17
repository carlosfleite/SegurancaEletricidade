using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace SegurancaEletricaEmCasa.UI
{
    /// <summary>
    /// Botão com cantos arredondados, cor sólida e uma transição suave de cor ao passar o
    /// mouse (em vez de trocar a cor de uma vez), desenhado via GDI+ — sem precisar de
    /// nenhum arquivo de imagem. Opcionalmente, se <see cref="SpriteImage"/> for definida
    /// (um PNG em pixel art já com o texto do botão desenhado dentro da imagem), o botão
    /// desenha essa imagem no lugar do retângulo colorido + texto, só aplicando um leve
    /// realce/escurecimento para indicar hover/clique.
    /// </summary>
    public class PixelButton : Button
    {
        public Color BaseColor { get; set; } = Color.FromArgb(224, 142, 53);
        public Color HoverColor { get; set; } = Color.FromArgb(240, 165, 80);
        public Color PressColor { get; set; } = Color.FromArgb(190, 110, 30);
        private Bitmap _spriteImage;
        public Bitmap SpriteImage
        {
            get => _spriteImage;
            set { _spriteImage = value; UpdateSpriteRegion(); }
        }

        public static Bitmap LoadSpriteOrNull(string relativePath) => Assets.LoadBitmapOrNull(relativePath);

        private bool _hoverTarget;
        private bool _pressed;
        private float _hoverProgress; // 0 = BaseColor, 1 = HoverColor
        private readonly Timer _animationTimer;

        public PixelButton()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
             ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
             ControlStyles.SupportsTransparentBackColor, true);

            // Selecionável e na ordem do Tab: Enter/Espaço acionam o botão focado (acessibilidade
            // por teclado). O foco aparece como um contorno claro (ver DrawFocusOutline).
            SetStyle(ControlStyles.Selectable, true);
            TabStop = true;

            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            BackColor = Color.Transparent;
            ForeColor = Color.FromArgb(250, 236, 204); 
            Font = GameFonts.Get(9f, FontStyle.Bold);
            Cursor = Cursors.Hand;

            _animationTimer = new Timer { Interval = 15 };
            _animationTimer.Tick += AnimationTick;

            MouseEnter += (s, e) => { _hoverTarget = true; _animationTimer.Start(); };
            MouseLeave += (s, e) => { _hoverTarget = false; _pressed = false; _animationTimer.Start(); };
            MouseDown += (s, e) => { _pressed = true; Invalidate(); };
            MouseUp += (s, e) => { _pressed = false; Invalidate(); };

            Disposed += (s, e) => _animationTimer.Dispose();
        }

        protected override void OnSizeChanged(System.EventArgs e)
        {
            base.OnSizeChanged(e);
            UpdateSpriteRegion();
        }

        /// <summary>
        /// Recorta o controle no formato do sprite (pixels opacos), como o QuizOverlay faz com o
        /// painel. Sem isso, os cantos transparentes do PNG apareciam como uma moldura escura em
        /// volta do botão, já que o WinForms não pinta o cenário do jogo atrás de um Button.
        /// </summary>
        private void UpdateSpriteRegion()
        {
            if (_spriteImage == null || Width <= 0 || Height <= 0)
                return;

            using (var scaled = new Bitmap(Width, Height, PixelFormat.Format32bppArgb))
            {
                using (Graphics g = Graphics.FromImage(scaled))
                {
                    g.InterpolationMode = InterpolationMode.NearestNeighbor;
                    g.PixelOffsetMode = PixelOffsetMode.Half;
                    g.DrawImage(_spriteImage, new Rectangle(0, 0, Width, Height));
                }

                Region oldRegion = Region;
                Region = NineSlice.RegionFromAlpha(scaled);
                oldRegion?.Dispose();
            }
        }

        private void AnimationTick(object sender, System.EventArgs e)
        {
            const float step = 0.18f;
            float target = _hoverTarget ? 1f : 0f;

            if (_hoverProgress < target)
                _hoverProgress = System.Math.Min(target, _hoverProgress + step);
            else if (_hoverProgress > target)
                _hoverProgress = System.Math.Max(target, _hoverProgress - step);

            if (_hoverProgress == target)
                _animationTimer.Stop();

            Invalidate();
        }

        private static Color Lerp(Color a, Color b, float t)
        {
            int r = a.R + (int)((b.R - a.R) * t);
            int g = a.G + (int)((b.G - a.G) * t);
            int bl = a.B + (int)((b.B - a.B) * t);
            return Color.FromArgb(r, g, bl);
        }

        protected override void OnGotFocus(System.EventArgs e)
        {
            base.OnGotFocus(e);
            Invalidate();
        }

        protected override void OnLostFocus(System.EventArgs e)
        {
            base.OnLostFocus(e);
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs pevent)
        {
            PaintContent(pevent.Graphics);

            if (Focused && ShowFocusCues)
                DrawFocusOutline(pevent.Graphics);
        }

        // Contorno claro em volta do botão que está com o foco do teclado (Tab).
        private void DrawFocusOutline(Graphics g)
        {
            g.SmoothingMode = SmoothingMode.None;
            using (var pen = new Pen(Color.FromArgb(255, 240, 150), 3))
                g.DrawRectangle(pen, 2, 2, Width - 5, Height - 5);
        }

        private void PaintContent(Graphics g)
        {
            if (SpriteImage != null)
            {
                DrawSprite(g);
                return;
            }

            g.SmoothingMode = SmoothingMode.AntiAlias;

            Color fill = _pressed ? PressColor : Lerp(BaseColor, HoverColor, _hoverProgress);
            var rect = new Rectangle(0, 0, Width - 1, Height - 1);

            using (GraphicsPath path = RoundedRect(rect, 10))
            {
                using (var brush = new SolidBrush(fill))
                    g.FillPath(brush, path);

                Region previousClip = g.Clip;
                g.SetClip(path, CombineMode.Replace);

                int highlightHeight = System.Math.Max(1, (int)(rect.Height * 0.34f));
                using (var highlightBrush = new SolidBrush(Color.FromArgb(_pressed ? 20 : 70, 255, 255, 255)))
                    g.FillRectangle(highlightBrush, rect.X, rect.Y, rect.Width, highlightHeight);

                int shadowHeight = System.Math.Max(1, (int)(rect.Height * 0.22f));
                using (var shadowBrush = new SolidBrush(Color.FromArgb(55, 0, 0, 0)))
                    g.FillRectangle(shadowBrush, rect.X, rect.Bottom - shadowHeight, rect.Width, shadowHeight);

                g.Clip = previousClip;

                using (var borderPen = new Pen(Color.FromArgb(70, 40, 20), 3))
                    g.DrawPath(borderPen, path);
            }

            Rectangle textRect = Rectangle.Inflate(rect, -14, 0);
            TextFormatFlags textFlags = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                                         TextFormatFlags.WordEllipsis | TextFormatFlags.EndEllipsis;

            Rectangle shadowTextRect = textRect;
            shadowTextRect.Offset(2, 2);
            TextRenderer.DrawText(g, Text, Font, shadowTextRect, Color.FromArgb(90, 40, 20), textFlags);
            TextRenderer.DrawText(g, Text, Font, textRect, ForeColor, textFlags);
        }

        private void DrawSprite(Graphics g)
        {
            g.InterpolationMode = InterpolationMode.NearestNeighbor;
            g.PixelOffsetMode = PixelOffsetMode.Half;

            float scale = _pressed ? 0.88f : 1f + 0.12f * _hoverProgress;

            var rect = new Rectangle(0, 0, Width, Height);

            if (scale == 1f)
            {
                g.DrawImage(SpriteImage, rect);
                return;
            }

            float[][] matrixItems =
            {
                new float[] { scale, 0, 0, 0, 0 },
                new float[] { 0, scale, 0, 0, 0 },
                new float[] { 0, 0, scale, 0, 0 },
                new float[] { 0, 0, 0, 1, 0 },
                new float[] { 0, 0, 0, 0, 1 }
            };

            using (var attributes = new ImageAttributes())
            {
                attributes.SetColorMatrix(new ColorMatrix(matrixItems));
                g.DrawImage(SpriteImage, rect, 0, 0, SpriteImage.Width, SpriteImage.Height, GraphicsUnit.Pixel, attributes);
            }
        }

        private static GraphicsPath RoundedRect(Rectangle bounds, int radius)
        {
            int d = radius * 2;
            var path = new GraphicsPath();
            path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
            path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
            path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
