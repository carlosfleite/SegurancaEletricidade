using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;
using SegurancaEletricaEmCasa.UI;

namespace SegurancaEletricaEmCasa.Forms
{
    /// <summary>
    /// Tela "Aprender": uma mini-aula em páginas, pensada para crianças lerem ANTES de jogar.
    /// Cada página tem um desenho em pixel art (gerado em código, ver os métodos Draw*Icon),
    /// um título, uma frase curta e dois quadros: o que FAZER (verde) e o que NÃO fazer
    /// (vermelho). Os perigos explicados aqui são os mesmos que aparecem nos cômodos do jogo.
    ///
    /// Navegação: botões ANTERIOR/PRÓXIMO, setas do teclado ou clique nas bolinhas. Na última
    /// página, o botão JOGAR fecha esta tela e o menu abre o jogo direto (ver
    /// <see cref="StartGameRequested"/>).
    /// </summary>
    public class LearnForm : Form
    {
        private enum IconKind { Lamp, Water, Outlet, BrokenWire, Overload, Covered, Shield }

        private sealed class Page
        {
            public IconKind Icon;
            public Color IconBackground;
            public string Title;
            public string Text;
            public string Do;
            public string Dont;
        }

        private static readonly Page[] Pages =
        {
            new Page
            {
                Icon = IconKind.Lamp, IconBackground = Color.FromArgb(232, 170, 60),
                Title = "A eletricidade nos ajuda",
                Text = "Ela acende as luzes, liga a TV e carrega o celular. Mas, se for usada do jeito errado, pode dar choque e até começar um incêndio!",
                Do = "Pedir ajuda a um adulto quando tiver dúvida.",
                Dont = "Mexer sozinho em fios, tomadas ou aparelhos estragados."
            },
            new Page
            {
                Icon = IconKind.Water, IconBackground = Color.FromArgb(70, 130, 190),
                Title = "Água e eletricidade não combinam",
                Text = "A água deixa a eletricidade passar para o nosso corpo. Por isso, molhado + tomada = choque!",
                Do = "Secar bem as mãos antes de ligar qualquer aparelho.",
                Dont = "Usar secador ou celular perto da pia, do chuveiro ou da banheira."
            },
            new Page
            {
                Icon = IconKind.Outlet, IconBackground = Color.FromArgb(150, 120, 180),
                Title = "Tomada não é brinquedo",
                Text = "Dentro da tomada tem eletricidade o tempo todo, mesmo sem nada ligado nela.",
                Do = "Manter as tomadas com a tampa (espelho) bem presa.",
                Dont = "Colocar dedos, grampos ou qualquer objeto na tomada."
            },
            new Page
            {
                Icon = IconKind.BrokenWire, IconBackground = Color.FromArgb(200, 90, 70),
                Title = "Cuidado com fios estragados",
                Text = "Fio descascado, queimado ou amassado pode dar choque e pegar fogo.",
                Do = "Avisar um adulto para trocar o fio estragado.",
                Dont = "Esconder fios embaixo do tapete ou deixar fios no caminho."
            },
            new Page
            {
                Icon = IconKind.Overload, IconBackground = Color.FromArgb(220, 120, 50),
                Title = "Tomada cheia demais esquenta",
                Text = "Ligar muitos aparelhos no mesmo benjamim (o \"T\") faz os fios esquentarem, como uma panela no fogo.",
                Do = "Ligar cada aparelho em uma tomada diferente.",
                Dont = "Encher um benjamim com vários plugues ao mesmo tempo."
            },
            new Page
            {
                Icon = IconKind.Covered, IconBackground = Color.FromArgb(90, 150, 110),
                Title = "Aparelhos precisam respirar",
                Text = "Aparelhos ligados esquentam. Se ficarem cobertos ou perto do fogão, podem pegar fogo.",
                Do = "Deixar o aparelho livre, com espaço para o ar passar.",
                Dont = "Cobrir aparelhos ligados com roupas, panos ou cobertas."
            },
            new Page
            {
                Icon = IconKind.Shield, IconBackground = Color.FromArgb(70, 150, 90),
                Title = "Agora é a sua vez!",
                Text = "No jogo, ande pela casa com as setas ou W, A, S, D, encontre os perigos e responda às perguntas. Cada resposta errada custa um coração.",
                Do = "Usar o que você aprendeu aqui para deixar a casa segura!",
                Dont = "Tentar consertar fios e tomadas de verdade sozinho."
            }
        };

        private static readonly Color TitleColor = Color.FromArgb(240, 178, 90);
        private static readonly Color TitleShadow = Color.FromArgb(20, 12, 6);
        private static readonly Color TextColor = Color.FromArgb(250, 236, 204);
        private static readonly Color DoColor = Color.FromArgb(70, 150, 80);
        private static readonly Color DontColor = Color.FromArgb(190, 70, 55);
        private static readonly Color Outline = Color.FromArgb(46, 20, 6);

        // Desenhos são feitos numa grade pequena e ampliados sem suavização (pixel art).
        private const int IconGrid = 32;

        private readonly Bitmap _background;
        private readonly Bitmap[] _icons = new Bitmap[Pages.Length];
        private readonly Rectangle _panelRect;
        private readonly Rectangle[] _dotRects = new Rectangle[Pages.Length];

        private PixelButton _anteriorButton;
        private PixelButton _proximoButton;
        private PixelButton _jogarButton;
        private int _pageIndex;

        /// <summary>True quando a criança clicou em JOGAR na última página.</summary>
        public bool StartGameRequested { get; private set; }

        public LearnForm()
        {
            Text = "Aprender";
            ClientSize = new Size(1000, 650);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            Icon = Assets.AppIcon ?? Icon;
            DoubleBuffered = true;
            KeyPreview = true;
            _background = Assets.LoadBitmapOrNull("Sprites/tela_inicial_fundo.png");

            _panelRect = new Rectangle(60, 100, ClientSize.Width - 120, 420);
            for (int i = 0; i < Pages.Length; i++)
            {
                _icons[i] = CreateIcon(Pages[i].Icon);

                const int dotSize = 14, dotGap = 12;
                int dotsWidth = Pages.Length * dotSize + (Pages.Length - 1) * dotGap;
                int dotX = (ClientSize.Width - dotsWidth) / 2 + i * (dotSize + dotGap);
                _dotRects[i] = new Rectangle(dotX, _panelRect.Bottom - 30, dotSize, dotSize);
            }

            BuildLayout();
            UpdatePage();
        }

        private void BuildLayout()
        {
            const int buttonCenterY = 580;

            _anteriorButton = CreateButton("Anterior", Color.FromArgb(140, 90, 100), "Sprites/botao_anterior.png", 220, 52);
            _anteriorButton.Location = new Point(_panelRect.X, buttonCenterY - _anteriorButton.Height / 2);
            _anteriorButton.Click += (s, e) => GoToPage(_pageIndex - 1);

            _proximoButton = CreateButton("Próximo", Color.FromArgb(80, 120, 150), "Sprites/botao_proximo.png", 220, 52);
            _proximoButton.Location = new Point(_panelRect.Right - _proximoButton.Width, buttonCenterY - _proximoButton.Height / 2);
            _proximoButton.Click += (s, e) => GoToPage(_pageIndex + 1);

            _jogarButton = CreateButton("Jogar", Color.FromArgb(224, 142, 53), "Sprites/botao_jogar.png", 220, 56);
            _jogarButton.Location = new Point(_panelRect.Right - _jogarButton.Width, buttonCenterY - _jogarButton.Height / 2);
            _jogarButton.Click += (s, e) => { StartGameRequested = true; Close(); };

            var fecharButton = CreateButton("Fechar", Color.FromArgb(196, 90, 90), "Sprites/botao_fechar.png", 150, 52);
            fecharButton.Location = new Point((ClientSize.Width - fecharButton.Width) / 2, buttonCenterY - fecharButton.Height / 2);
            fecharButton.Click += (s, e) => Close();

            Controls.Add(_anteriorButton);
            Controls.Add(_proximoButton);
            Controls.Add(_jogarButton);
            Controls.Add(fecharButton);
        }

        /// <summary>Mesmo esquema dos outros menus: altura segue a proporção do sprite; sem PNG, botão desenhado.</summary>
        private static PixelButton CreateButton(string text, Color baseColor, string spritePath, int width, int fallbackHeight)
        {
            var button = new PixelButton
            {
                Text = text,
                BaseColor = baseColor,
                HoverColor = ControlPaint.Light(baseColor, 0.3f),
                PressColor = ControlPaint.Dark(baseColor, 0.1f),
                Font = GameFonts.Get(7.5f, FontStyle.Bold),
                SpriteImage = PixelButton.LoadSpriteOrNull(spritePath)
            };

            int height = fallbackHeight;
            if (button.SpriteImage != null)
                height = (int)Math.Round(width * (button.SpriteImage.Height / (float)button.SpriteImage.Width));

            button.Size = new Size(width, height);
            return button;
        }

        private void GoToPage(int index)
        {
            if (index < 0 || index >= Pages.Length || index == _pageIndex)
                return;

            _pageIndex = index;
            UpdatePage();
        }

        private void UpdatePage()
        {
            bool isLast = _pageIndex == Pages.Length - 1;
            _anteriorButton.Visible = _pageIndex > 0;
            _proximoButton.Visible = !isLast;
            _jogarButton.Visible = isLast;
            Invalidate();
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            switch (keyData)
            {
                case Keys.Left:
                case Keys.A:
                    GoToPage(_pageIndex - 1);
                    return true;
                case Keys.Right:
                case Keys.D:
                    GoToPage(_pageIndex + 1);
                    return true;
                case Keys.Escape:
                    Close();
                    return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            for (int i = 0; i < _dotRects.Length; i++)
            {
                if (Rectangle.Inflate(_dotRects[i], 6, 6).Contains(e.Location))
                {
                    GoToPage(i);
                    return;
                }
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            foreach (Bitmap icon in _icons)
                icon?.Dispose();
            _background?.Dispose();
            base.OnFormClosed(e);
        }

        // ------------------------------------------------------------------ desenho

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            DrawBackground(g);
            DrawHeader(g);
            DrawPanel(g);
            DrawDots(g);
        }

        private void DrawBackground(Graphics g)
        {
            if (_background != null)
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.DrawImage(_background, new Rectangle(Point.Empty, ClientSize));

                using (var overlay = new SolidBrush(Color.FromArgb(110, 0, 0, 0)))
                    g.FillRectangle(overlay, ClientRectangle);
            }
            else
            {
                using (var bgBrush = new LinearGradientBrush(
                    ClientRectangle, Color.FromArgb(48, 33, 22), Color.FromArgb(24, 16, 10), LinearGradientMode.Vertical))
                    g.FillRectangle(bgBrush, ClientRectangle);
            }
        }

        private void DrawHeader(Graphics g)
        {
            var titleRect = new Rectangle(0, 26, ClientSize.Width, 60);
            var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };

            using (var titleFont = GameFonts.Get(15f, FontStyle.Bold))
            {
                const string title = "VAMOS APRENDER!";
                var shadowRect = titleRect;
                shadowRect.Offset(3, 3);
                using (var shadowBrush = new SolidBrush(TitleShadow))
                    g.DrawString(title, titleFont, shadowBrush, shadowRect, format);
                using (var titleBrush = new SolidBrush(TitleColor))
                    g.DrawString(title, titleFont, titleBrush, titleRect, format);
            }
        }

        private void DrawPanel(Graphics g)
        {
            Page page = Pages[_pageIndex];

            using (GraphicsPath panelPath = RoundedRect(_panelRect, 18))
            using (var panelBrush = new SolidBrush(Color.FromArgb(195, 40, 26, 16)))
            using (var panelBorder = new Pen(Color.FromArgb(220, 120, 74, 34), 3))
            {
                g.FillPath(panelBrush, panelPath);
                g.DrawPath(panelBorder, panelPath);
            }

            // --- Desenho à esquerda, num "selo" redondo colorido -------------------
            const int iconSize = 224;
            var circleRect = new Rectangle(_panelRect.X + 36, _panelRect.Y + 70, 250, 250);
            using (var circleBrush = new SolidBrush(page.IconBackground))
            using (var circleBorder = new Pen(Outline, 6))
            using (var circleShine = new SolidBrush(Color.FromArgb(50, 255, 255, 255)))
            {
                g.FillEllipse(circleBrush, circleRect);
                g.FillEllipse(circleShine, circleRect.X + 24, circleRect.Y + 14, circleRect.Width - 48, circleRect.Height / 2 - 10);
                g.DrawEllipse(circleBorder, circleRect);
            }

            var iconRect = new Rectangle(
                circleRect.X + (circleRect.Width - iconSize) / 2,
                circleRect.Y + (circleRect.Height - iconSize) / 2,
                iconSize, iconSize);
            InterpolationMode oldInterpolation = g.InterpolationMode;
            PixelOffsetMode oldOffset = g.PixelOffsetMode;
            g.InterpolationMode = InterpolationMode.NearestNeighbor;
            g.PixelOffsetMode = PixelOffsetMode.Half;
            g.DrawImage(_icons[_pageIndex], iconRect);
            g.InterpolationMode = oldInterpolation;
            g.PixelOffsetMode = oldOffset;

            // --- Textos à direita ---------------------------------------------------
            int textLeft = circleRect.Right + 40;
            int textWidth = _panelRect.Right - 36 - textLeft;

            using (var stepFont = GameFonts.Get(6f, FontStyle.Bold))
            {
                TextRenderer.DrawText(g, $"LIÇÃO {_pageIndex + 1} DE {Pages.Length}", stepFont,
                    new Rectangle(textLeft, _panelRect.Y + 26, textWidth, 20), Color.FromArgb(200, 170, 130),
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
            }

            var titleRect = new Rectangle(textLeft, _panelRect.Y + 50, textWidth, 44);
            using (var titleFont = GameFonts.Get(11f, FontStyle.Bold))
            {
                var flags = TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.WordBreak;
                var shadowRect = titleRect;
                shadowRect.Offset(2, 2);
                TextRenderer.DrawText(g, page.Title, titleFont, shadowRect, TitleShadow, flags);
                TextRenderer.DrawText(g, page.Title, titleFont, titleRect, TitleColor, flags);
            }

            var bodyRect = new Rectangle(textLeft, titleRect.Bottom + 8, textWidth, 86);
            using (var bodyFont = GameFonts.GetReadable(13f))
            {
                TextRenderer.DrawText(g, page.Text, bodyFont, bodyRect, TextColor,
                    TextFormatFlags.Left | TextFormatFlags.Top | TextFormatFlags.WordBreak);
            }

            var doRect = new Rectangle(textLeft, bodyRect.Bottom + 8, textWidth, 80);
            var dontRect = new Rectangle(textLeft, doRect.Bottom + 10, textWidth, 80);
            DrawTipBox(g, doRect, true, page.Do);
            DrawTipBox(g, dontRect, false, page.Dont);
        }

        /// <summary>Quadro "FAÇA" (verde, com ✓) ou "NÃO FAÇA" (vermelho, com ✗).</summary>
        private static void DrawTipBox(Graphics g, Rectangle rect, bool isDo, string text)
        {
            Color color = isDo ? DoColor : DontColor;

            using (GraphicsPath path = RoundedRect(rect, 12))
            using (var fill = new SolidBrush(Color.FromArgb(70, color)))
            using (var border = new Pen(color, 3))
            {
                g.FillPath(fill, path);
                g.DrawPath(border, path);
            }

            // Bolinha com o símbolo
            var badge = new Rectangle(rect.X + 14, rect.Y + (rect.Height - 44) / 2, 44, 44);
            using (var badgeBrush = new SolidBrush(color))
            using (var badgeBorder = new Pen(Outline, 3))
            using (var markPen = new Pen(Color.White, 6) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round })
            {
                g.FillEllipse(badgeBrush, badge);
                g.DrawEllipse(badgeBorder, badge);

                if (isDo)
                {
                    g.DrawLines(markPen, new[]
                    {
                        new Point(badge.X + 12, badge.Y + 23),
                        new Point(badge.X + 19, badge.Y + 30),
                        new Point(badge.X + 32, badge.Y + 14)
                    });
                }
                else
                {
                    g.DrawLine(markPen, badge.X + 14, badge.Y + 14, badge.Right - 14, badge.Bottom - 14);
                    g.DrawLine(markPen, badge.Right - 14, badge.Y + 14, badge.X + 14, badge.Bottom - 14);
                }
            }

            int textX = badge.Right + 14;
            var labelRect = new Rectangle(textX, rect.Y + 6, rect.Right - textX - 12, 18);
            using (var labelFont = GameFonts.Get(6f, FontStyle.Bold))
            {
                TextRenderer.DrawText(g, isDo ? "FAÇA" : "NÃO FAÇA", labelFont, labelRect,
                    isDo ? Color.FromArgb(150, 220, 150) : Color.FromArgb(250, 150, 130),
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
            }

            var textRect = new Rectangle(textX, labelRect.Bottom, rect.Right - textX - 12, rect.Bottom - labelRect.Bottom - 4);
            using (var font = GameFonts.GetReadable(11.5f, FontStyle.Bold))
            {
                TextRenderer.DrawText(g, text, font, textRect, TextColor,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.WordBreak);
            }
        }

        private void DrawDots(Graphics g)
        {
            for (int i = 0; i < _dotRects.Length; i++)
            {
                bool current = i == _pageIndex;
                Color fill = current ? TitleColor : (i < _pageIndex ? Color.FromArgb(160, 120, 80) : Color.FromArgb(90, 70, 50));
                Rectangle r = current ? Rectangle.Inflate(_dotRects[i], 3, 3) : _dotRects[i];

                using (var brush = new SolidBrush(fill))
                using (var pen = new Pen(Outline, 2))
                {
                    g.FillEllipse(brush, r);
                    g.DrawEllipse(pen, r);
                }
            }
        }

        // ------------------------------------------------------------------ ícones em pixel art

        /// <summary>
        /// Cada ícone é pintado com retângulos numa grade de 32x32 e depois recebe um contorno
        /// escuro automático (como nos sprites do jogo). Na tela ele é ampliado com
        /// NearestNeighbor, então os "pixels" ficam bem marcados.
        /// </summary>
        private static Bitmap CreateIcon(IconKind kind)
        {
            // +2: uma margem de 1 pixel em cada lado para o contorno caber mesmo nas bordas.
            var bmp = new Bitmap(IconGrid + 2, IconGrid + 2, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.None;
                g.PixelOffsetMode = PixelOffsetMode.Half;
                g.TranslateTransform(1, 1);
                var p = new PixelPainter(g);

                switch (kind)
                {
                    case IconKind.Lamp: DrawLampIcon(p); break;
                    case IconKind.Water: DrawWaterIcon(p); break;
                    case IconKind.Outlet: DrawOutletIcon(p); break;
                    case IconKind.BrokenWire: DrawBrokenWireIcon(p); break;
                    case IconKind.Overload: DrawOverloadIcon(p); break;
                    case IconKind.Covered: DrawCoveredIcon(p); break;
                    case IconKind.Shield: DrawShieldIcon(p); break;
                }
            }

            return AddOutline(bmp);
        }

        private sealed class PixelPainter
        {
            private readonly Graphics _g;
            public PixelPainter(Graphics g) { _g = g; }

            public void R(int x, int y, int w, int h, Color c)
            {
                using (var b = new SolidBrush(c))
                    _g.FillRectangle(b, x, y, w, h);
            }

            /// <summary>"Círculo" em pixels: linhas horizontais calculadas pela equação do círculo.</summary>
            public void Circle(int cx, int cy, int radius, Color c)
            {
                for (int dy = -radius; dy < radius; dy++)
                {
                    double yy = dy + 0.5;
                    int half = (int)Math.Round(Math.Sqrt(radius * radius - yy * yy));
                    R(cx - half, cy + dy, half * 2, 1, c);
                }
            }

            public void Bolt(int x, int y, Color c)
            {
                // raio pequeno (7x11)
                R(x + 3, y, 4, 1, c);
                R(x + 2, y + 1, 4, 2, c);
                R(x + 1, y + 3, 4, 2, c);
                R(x, y + 5, 7, 1, c);
                R(x + 3, y + 6, 3, 2, c);
                R(x + 2, y + 8, 3, 1, c);
                R(x + 2, y + 9, 2, 1, c);
                R(x + 1, y + 10, 2, 1, c);
            }
        }

        private static readonly Color Yellow = Color.FromArgb(255, 222, 90);
        private static readonly Color YellowLight = Color.FromArgb(255, 245, 190);
        private static readonly Color Gray = Color.FromArgb(150, 150, 160);
        private static readonly Color GrayDark = Color.FromArgb(100, 100, 112);
        private static readonly Color White = Color.FromArgb(245, 240, 230);
        private static readonly Color Cream = Color.FromArgb(225, 215, 195);
        private static readonly Color Black = Color.FromArgb(40, 36, 40);
        private static readonly Color Copper = Color.FromArgb(220, 130, 60);
        private static readonly Color Red = Color.FromArgb(220, 60, 50);
        private static readonly Color Orange = Color.FromArgb(250, 150, 40);
        private static readonly Color Blue = Color.FromArgb(90, 170, 240);
        private static readonly Color BlueLight = Color.FromArgb(180, 225, 255);

        private static void DrawLampIcon(PixelPainter p)
        {
            // raios de luz
            p.R(15, 1, 2, 3, YellowLight);
            p.R(4, 6, 3, 2, YellowLight); p.R(25, 6, 3, 2, YellowLight);
            p.R(2, 14, 3, 2, YellowLight); p.R(27, 14, 3, 2, YellowLight);
            // bulbo
            p.Circle(16, 13, 8, Yellow);
            p.R(12, 20, 8, 3, Yellow);
            p.R(12, 9, 3, 3, YellowLight);
            // filamento
            p.R(14, 14, 1, 5, Orange); p.R(17, 14, 1, 5, Orange); p.R(14, 14, 4, 1, Orange);
            // rosca
            p.R(12, 23, 8, 2, Gray); p.R(12, 25, 8, 1, GrayDark);
            p.R(12, 26, 8, 2, Gray); p.R(14, 28, 4, 2, GrayDark);
        }

        private static void DrawWaterIcon(PixelPainter p)
        {
            // gota
            p.R(11, 3, 2, 2, Blue);
            p.R(10, 5, 4, 3, Blue);
            p.R(8, 8, 8, 3, Blue);
            p.R(6, 11, 12, 3, Blue);
            p.Circle(12, 19, 7, Blue);
            p.R(8, 15, 2, 4, BlueLight);
            // tomada + raio, ao lado
            p.Circle(24, 22, 6, White);
            p.R(22, 20, 1, 3, Black); p.R(26, 20, 1, 3, Black); p.R(24, 24, 1, 2, Black);
            p.Bolt(19, 4, Yellow);
        }

        private static void DrawOutletIcon(PixelPainter p)
        {
            // espelho (placa) quadrado
            p.R(4, 4, 24, 24, White);
            p.R(4, 26, 24, 2, Cream);
            // parte redonda
            p.Circle(16, 16, 8, Cream);
            // três furinhos (padrão brasileiro)
            p.R(12, 12, 2, 4, Black);
            p.R(18, 12, 2, 4, Black);
            p.R(15, 19, 2, 3, Black);
            // parafuso
            p.R(15, 6, 2, 2, GrayDark);
            p.R(15, 24, 2, 2, GrayDark);
        }

        private static void DrawBrokenWireIcon(PixelPainter p)
        {
            // fio da esquerda
            p.R(1, 18, 10, 4, Black);
            p.R(11, 17, 2, 6, Copper);
            p.R(13, 16, 1, 2, Copper); p.R(13, 22, 1, 2, Copper);
            // fio da direita
            p.R(21, 10, 10, 4, Black);
            p.R(19, 9, 2, 6, Copper);
            p.R(18, 8, 1, 2, Copper); p.R(18, 14, 1, 2, Copper);
            // faísca no meio
            p.R(15, 12, 2, 8, YellowLight);
            p.R(12, 15, 8, 2, YellowLight);
            p.R(14, 14, 4, 4, Yellow);
            p.R(10, 9, 2, 2, Yellow); p.R(21, 21, 2, 2, Yellow);
            // fumacinha
            p.R(5, 4, 3, 3, Gray); p.R(7, 2, 3, 2, Gray); p.R(24, 25, 3, 3, Gray);
        }

        private static void DrawOverloadIcon(PixelPainter p)
        {
            // fogo em cima
            p.R(13, 2, 2, 2, Red);
            p.R(12, 4, 5, 2, Red);
            p.R(18, 3, 2, 3, Orange);
            p.R(11, 6, 10, 3, Orange);
            p.R(13, 7, 5, 2, Yellow);
            // benjamim (T)
            p.R(8, 9, 16, 10, White);
            p.R(8, 17, 16, 2, Cream);
            p.R(12, 19, 8, 6, White);
            // pinos para a tomada
            p.R(14, 25, 1, 3, Gray); p.R(17, 25, 1, 3, Gray);
            // três plugues encaixados
            p.R(2, 10, 6, 6, Black); p.R(0, 12, 2, 2, Black);
            p.R(24, 10, 6, 6, Black); p.R(30, 12, 2, 2, Black);
            p.R(13, 11, 6, 5, GrayDark);
        }

        private static void DrawCoveredIcon(PixelPainter p)
        {
            // ondas de calor
            p.R(9, 1, 2, 2, Red); p.R(10, 3, 2, 2, Red); p.R(9, 5, 2, 2, Red);
            p.R(16, 1, 2, 2, Orange); p.R(17, 3, 2, 2, Orange); p.R(16, 5, 2, 2, Orange);
            p.R(23, 1, 2, 2, Red); p.R(24, 3, 2, 2, Red); p.R(23, 5, 2, 2, Red);
            // aparelho (aquecedor/caixa)
            p.R(4, 9, 24, 17, Gray);
            p.R(6, 11, 20, 9, GrayDark);
            p.R(8, 13, 16, 1, Orange); p.R(8, 16, 16, 1, Orange);
            p.R(22, 22, 3, 2, Red);
            p.R(6, 26, 3, 3, GrayDark); p.R(23, 26, 3, 3, GrayDark);
            // roupa jogada por cima (vermelha, caindo pelos lados)
            p.R(2, 8, 18, 4, Color.FromArgb(200, 70, 90));
            p.R(2, 12, 4, 8, Color.FromArgb(200, 70, 90));
            p.R(14, 12, 5, 3, Color.FromArgb(200, 70, 90));
            p.R(4, 9, 12, 1, Color.FromArgb(235, 120, 135));
        }

        private static void DrawShieldIcon(PixelPainter p)
        {
            Color shield = Color.FromArgb(250, 236, 204);
            Color shieldShade = Color.FromArgb(220, 200, 160);
            p.R(6, 3, 20, 2, shield);
            p.R(4, 5, 24, 12, shield);
            p.R(5, 17, 22, 3, shield);
            p.R(7, 20, 18, 3, shield);
            p.R(9, 23, 14, 2, shieldShade);
            p.R(12, 25, 8, 2, shieldShade);
            p.R(14, 27, 4, 2, shieldShade);
            // raio no meio do escudo
            p.R(15, 6, 5, 2, Orange);
            p.R(13, 8, 5, 3, Orange);
            p.R(11, 11, 5, 3, Orange);
            p.R(11, 14, 10, 2, Orange);
            p.R(15, 16, 4, 3, Orange);
            p.R(14, 19, 3, 2, Orange);
            p.R(13, 21, 2, 2, Orange);
        }

        /// <summary>Contorno de 1 "pixel" em volta de tudo que foi desenhado.</summary>
        private static Bitmap AddOutline(Bitmap source)
        {
            int w = source.Width, h = source.Height;
            var result = new Bitmap(w, h, PixelFormat.Format32bppArgb);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    Color c = source.GetPixel(x, y);
                    if (c.A > 0)
                    {
                        result.SetPixel(x, y, c);
                        continue;
                    }

                    bool touches =
                        (x > 0 && source.GetPixel(x - 1, y).A > 0) ||
                        (x < w - 1 && source.GetPixel(x + 1, y).A > 0) ||
                        (y > 0 && source.GetPixel(x, y - 1).A > 0) ||
                        (y < h - 1 && source.GetPixel(x, y + 1).A > 0);

                    if (touches)
                        result.SetPixel(x, y, Outline);
                }
            }

            source.Dispose();
            return result;
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
