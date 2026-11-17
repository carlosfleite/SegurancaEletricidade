using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using SegurancaEletricaEmCasa.UI;

namespace SegurancaEletricaEmCasa.Forms
{
    public class CreditsForm : Form
    {
        private static readonly (string Label, string Value)[] Fields =
        {
            ("Disciplina", "Programação de Computadores"),
            ("Professor responsável", "Prof. Dr. Elvio Gilberto da Silva"),
            ("Desenvolvedores", "Carlos Henrique Fernandes Leite"),
            ("Curso", "Ciência da Computação"),
            ("Instituição", "Centro Universitário Sagrado Coração - UNISAGRADO"),
            ("Período", "2º semestre de 2026"),
            ("Versão", "1.0"),
            ("Tecnologias", "C#, .NET Framework 4.8.1, Windows Forms")
        };

        private static readonly Color LabelColor = Color.FromArgb(230, 178, 120);
        private static readonly Color ValueColor = Color.FromArgb(250, 236, 204);
        private static readonly Color TitleColor = Color.FromArgb(240, 178, 90);
        private static readonly Color TitleShadow = Color.FromArgb(20, 12, 6);

        private readonly Bitmap _background;
        private readonly Bitmap _logoCurso;
        private readonly Bitmap _logoExtensao;

        public CreditsForm()
        {
            Text = "Créditos";
            ClientSize = new Size(1000, 650);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            Icon = Assets.AppIcon ?? Icon;
            DoubleBuffered = true;
            _background = PixelButton.LoadSpriteOrNull("Sprites/tela_inicial_fundo.png");
            _logoCurso = Assets.LoadBitmapOrNull("Sprites/logo_curso_ciencia_da_computacao.png");
            _logoExtensao = Assets.LoadBitmapOrNull("Sprites/logo_coordenadoria_de_extensao.png");

            BuildLayout();
        }

        private void BuildLayout()
        {
            var fecharButton = new PixelButton
            {
                Text = "Fechar",
                BaseColor = Color.FromArgb(196, 90, 90),
                HoverColor = Color.FromArgb(215, 115, 115),
                PressColor = Color.FromArgb(160, 70, 70),
                Font = GameFonts.Get(8f, FontStyle.Bold),
                SpriteImage = PixelButton.LoadSpriteOrNull("Sprites/botao_fechar.png")
            };

            const int width = 180;
            int height = 54;
            if (fecharButton.SpriteImage != null)
                height = (int)System.Math.Round(width * (fecharButton.SpriteImage.Height / (float)fecharButton.SpriteImage.Width));

            fecharButton.Size = new Size(width, height);
            fecharButton.Location = new Point((ClientSize.Width - width) / 2, 548);
            fecharButton.Click += (s, e) => Close();

            Controls.Add(fecharButton);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            DrawBackground(g);
            DrawTitle(g);
            DrawInfoPanel(g);
            DrawLogos(g);
        }

        private void DrawBackground(Graphics g)
        {
            if (_background != null)
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.DrawImage(_background, new Rectangle(Point.Empty, ClientSize));

                using (var overlay = new SolidBrush(Color.FromArgb(80, 0, 0, 0)))
                    g.FillRectangle(overlay, ClientRectangle);
            }
            else
            {
                using (var bgBrush = new LinearGradientBrush(
                    ClientRectangle, Color.FromArgb(48, 33, 22), Color.FromArgb(24, 16, 10), LinearGradientMode.Vertical))
                    g.FillRectangle(bgBrush, ClientRectangle);
            }
        }

        private void DrawTitle(Graphics g)
        {
            var titleRect = new Rectangle(0, 34, ClientSize.Width, 60);
            var titleFormat = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };

            using (var titleFont = GameFonts.Get(15f, FontStyle.Bold))
            {
                var shadowRect = titleRect;
                shadowRect.Offset(3, 3);
                using (var shadowBrush = new SolidBrush(TitleShadow))
                    g.DrawString("CRÉDITOS", titleFont, shadowBrush, shadowRect, titleFormat);

                using (var titleBrush = new SolidBrush(TitleColor))
                    g.DrawString("CRÉDITOS", titleFont, titleBrush, titleRect, titleFormat);
            }
        }

        private void DrawInfoPanel(Graphics g)
        {
            var panelRect = new Rectangle((ClientSize.Width - 760) / 2, 96, 760, 280);

            using (GraphicsPath panelPath = RoundedRect(panelRect, 18))
            using (var panelBrush = new SolidBrush(Color.FromArgb(165, 40, 26, 16)))
            using (var panelBorder = new Pen(Color.FromArgb(200, 120, 74, 34), 2))
            {
                g.FillPath(panelBrush, panelPath);
                g.DrawPath(panelBorder, panelPath);
            }

            int rowHeight = panelRect.Height / Fields.Length;
            int labelWidth = (int)(panelRect.Width * 0.36f);

            using (var labelFont = GameFonts.Get(6.5f, FontStyle.Bold))
            using (var valueFont = GameFonts.Get(6.5f))
            using (var separatorPen = new Pen(Color.FromArgb(60, 250, 236, 204), 1f))
            {
                for (int i = 0; i < Fields.Length; i++)
                {
                    int rowY = panelRect.Y + i * rowHeight;
                    var labelRect = new Rectangle(panelRect.X + 26, rowY, labelWidth - 26, rowHeight);
                    var valueRect = new Rectangle(panelRect.X + labelWidth + 10, rowY, panelRect.Width - labelWidth - 36, rowHeight);

                    TextRenderer.DrawText(g, Fields[i].Label, labelFont, labelRect, LabelColor,
                        TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.WordEllipsis);
                    TextRenderer.DrawText(g, Fields[i].Value, valueFont, valueRect, ValueColor,
                        TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.WordEllipsis);

                    if (i < Fields.Length - 1)
                    {
                        int lineY = rowY + rowHeight;
                        g.DrawLine(separatorPen, panelRect.X + 20, lineY, panelRect.Right - 20, lineY);
                    }
                }
            }
        }

        // Logotipos institucionais (curso e Extensão), sem deformar nem recolorir, sobre uma
        // placa clara: o texto das logos é escuro e não ficaria legível sobre o fundo escuro.
        private void DrawLogos(Graphics g)
        {
            DrawLogo(g, _logoCurso, "Desenvolvimento", new Rectangle(70, 402, 400, 84));
            DrawLogo(g, _logoExtensao, "Apoio", new Rectangle(530, 402, 400, 84));
        }

        private void DrawLogo(Graphics g, Bitmap logo, string legenda, Rectangle area)
        {
            using (var legendaFont = GameFonts.Get(6f, FontStyle.Bold))
                TextRenderer.DrawText(g, legenda, legendaFont, new Rectangle(area.X, area.Y - 22, area.Width, 20), LabelColor,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

            using (GraphicsPath plate = RoundedRect(area, 12))
            using (var plateBrush = new SolidBrush(Color.FromArgb(250, 246, 238)))
            using (var plateBorder = new Pen(Color.FromArgb(200, 120, 74, 34), 2))
            {
                g.FillPath(plateBrush, plate);
                g.DrawPath(plateBorder, plate);
            }

            if (logo == null) return;

            Rectangle inner = Rectangle.Inflate(area, -16, -14);
            float scale = System.Math.Min(inner.Width / (float)logo.Width, inner.Height / (float)logo.Height);
            int w = (int)(logo.Width * scale), h = (int)(logo.Height * scale);
            var dest = new Rectangle(inner.X + (inner.Width - w) / 2, inner.Y + (inner.Height - h) / 2, w, h);

            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.DrawImage(logo, dest);
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
