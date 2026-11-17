using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using SegurancaEletricaEmCasa.UI;

namespace SegurancaEletricaEmCasa.Forms
{
    public class StartForm : Form
    {
        private static readonly Color BgFallbackTop = Color.FromArgb(48, 33, 22);
        private static readonly Color BgFallbackBottom = Color.FromArgb(24, 16, 10);
        private static readonly Color SubtitleColor = Color.FromArgb(230, 220, 200);

        private readonly Bitmap _background;
        private readonly Bitmap _logo;
        private QuizOverlay _avisoOverlay;

        public StartForm()
        {
            Text = "Segurança Elétrica em Casa";
            ClientSize = new Size(1000, 650);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            Icon = Assets.AppIcon ?? Icon;
            DoubleBuffered = true;

            // Carregados sem travar o arquivo; null se ainda não existirem (o OnPaint cai de
            // volta no gradiente/selo desenhados em código nesse caso).
            _background = Assets.LoadBitmapOrNull("Sprites/tela_inicial_fundo.png");
            _logo = Assets.LoadBitmapOrNull("Sprites/logo.png");

            BuildLayout();
        }

        private void BuildLayout()
        {
            var jogarButton = new PixelButton
            {
                Text = "JOGAR",
                BaseColor = Color.FromArgb(224, 142, 53),
                HoverColor = Color.FromArgb(240, 165, 80),
                Font = GameFonts.Get(9f, FontStyle.Bold),
                SpriteImage = PixelButton.LoadSpriteOrNull("Sprites/botao_jogar.png")
            };
            PositionButtonBySprite(jogarButton, width: 260, fallbackHeight: 56, centerY: 430);
            jogarButton.Click += (s, e) => AbrirJogo();

            var aprenderButton = new PixelButton
            {
                Text = "APRENDER",
                BaseColor = Color.FromArgb(80, 140, 90),
                HoverColor = Color.FromArgb(100, 165, 110),
                Font = GameFonts.Get(7.5f, FontStyle.Bold),
                SpriteImage = PixelButton.LoadSpriteOrNull("Sprites/botao_aprender.png")
            };
            PositionButtonBySprite(aprenderButton, width: 260, fallbackHeight: 48, centerY: 499);
            aprenderButton.Click += (s, e) => AbrirAprender();

            var creditosButton = new PixelButton
            {
                Text = "CRÉDITOS",
                BaseColor = Color.FromArgb(140, 120, 170),
                HoverColor = Color.FromArgb(160, 140, 190),
                Font = GameFonts.Get(7.5f, FontStyle.Bold),
                SpriteImage = PixelButton.LoadSpriteOrNull("Sprites/botao_creditos.png")
            };
            PositionButtonBySprite(creditosButton, width: 260, fallbackHeight: 48, centerY: 568);
            creditosButton.Click += (s, e) => AbrirCreditos();

            var sairButton = new PixelButton
            {
                Text = "Sair",
                BaseColor = Color.FromArgb(196, 90, 90),
                HoverColor = Color.FromArgb(215, 115, 115),
                PressColor = Color.FromArgb(160, 70, 70),
                Font = GameFonts.Get(6f, FontStyle.Bold),
                SpriteImage = PixelButton.LoadSpriteOrNull("Sprites/botao_sair.png")
            };

            // Mesma largura do botão "Voltar" do jogo; a altura acompanha a proporção da arte.
            const int sairWidth = 110;
            int sairHeight = 42;
            if (sairButton.SpriteImage != null)
                sairHeight = (int)System.Math.Round(sairWidth * (sairButton.SpriteImage.Height / (float)sairButton.SpriteImage.Width));

            sairButton.Size = new Size(sairWidth, sairHeight);
            sairButton.Location = new Point(ClientSize.Width - sairWidth - 16, 12);
            sairButton.Click += (s, e) => ConfirmarSaida();

            Controls.Add(jogarButton);
            Controls.Add(aprenderButton);
            Controls.Add(creditosButton);
            Controls.Add(sairButton);

            // Aviso de saída: usa o mesmo painel em pixel art do quiz do jogo.
            _avisoOverlay = new QuizOverlay { Visible = false };
            _avisoOverlay.Resize += (s, e) => PositionAvisoOverlay();
            Controls.Add(_avisoOverlay);
        }

        private void PositionAvisoOverlay()
        {
            _avisoOverlay.Location = new Point(
                (ClientSize.Width - _avisoOverlay.Width) / 2,
                (ClientSize.Height - _avisoOverlay.Height) / 2);
        }

        private void ConfirmarSaida()
        {
            if (_avisoOverlay.Visible) return;

            _avisoOverlay.ShowMessage(
                "SAIR DO JOGO",
                "Deseja realmente sair do jogo?",
                QuizOverlay.DefaultTextColor,
                new[] { "SIM, SAIR", "CONTINUAR AQUI" },
                choice =>
                {
                    if (choice == 0) Close();
                });
            PositionAvisoOverlay();
        }

        /// <summary>
        /// Define o tamanho do botão a partir da proporção real da arte (para não distorcer o
        /// pixel art), mantendo a mesma largura e o mesmo centro vertical que o layout original
        /// usava — assim, mesmo a altura mudando um pouco conforme a imagem, os botões
        /// continuam alinhados e com o espaçamento esperado entre eles. Sem imagem (arquivo
        /// ainda não adicionado ao projeto), cai no tamanho antigo do botão desenhado em código.
        /// </summary>
        private void PositionButtonBySprite(PixelButton button, int width, int fallbackHeight, int centerY)
        {
            int height = fallbackHeight;
            if (button.SpriteImage != null)
                height = (int)System.Math.Round(width * (button.SpriteImage.Height / (float)button.SpriteImage.Width));

            button.Size = new Size(width, height);
            button.Location = new Point((ClientSize.Width - width) / 2, centerY - height / 2);
        }

        private void AbrirJogo()
        {
            if (_avisoOverlay.Visible) return;
            Hide();
            var gameForm = new MainForm();
            gameForm.FormClosed += (s, e) => Show();
            gameForm.Show();
        }

        /// <summary>
        /// Abre a mini-aula. Se a criança terminar clicando em JOGAR na última página, já vai
        /// direto para o jogo em vez de voltar ao menu.
        /// </summary>
        private void AbrirAprender()
        {
            if (_avisoOverlay.Visible) return;
            Hide();
            var learnForm = new LearnForm();
            learnForm.FormClosed += (s, e) =>
            {
                if (learnForm.StartGameRequested)
                    AbrirJogo();
                else
                    Show();
            };
            learnForm.Show();
        }

        private void AbrirCreditos()
        {
            if (_avisoOverlay.Visible) return;
            Hide();
            var creditsForm = new CreditsForm();
            creditsForm.FormClosed += (s, e) => Show();
            creditsForm.Show();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;

            DrawBackground(g);
            DrawLogo(g);
        }

        /// <summary>
        /// Fundo: a nova arte do quarto à noite (Sprites/tela_inicial_fundo.png), esticada para
        /// preencher a janela inteira. Se o arquivo ainda não tiver sido adicionado ao projeto,
        /// cai de volta no gradiente simples de antes, só para a tela nunca ficar em branco.
        /// </summary>
        private void DrawBackground(Graphics g)
        {
            if (_background != null)
            {
                g.DrawImage(_background, new Rectangle(Point.Empty, ClientSize));

                // Um leve escurecimento uniforme sobre a arte ajuda o selo/logo e o texto a se
                // destacarem, sem esconder o cenário por trás.
                using (var overlay = new SolidBrush(Color.FromArgb(35, 0, 0, 0)))
                    g.FillRectangle(overlay, ClientRectangle);
            }
            else
            {
                using (var bgBrush = new LinearGradientBrush(ClientRectangle, BgFallbackTop, BgFallbackBottom, LinearGradientMode.Vertical))
                    g.FillRectangle(bgBrush, ClientRectangle);
            }
        }

        /// <summary>
        /// Desenha a logo (selo + nome do jogo, já com o texto do título dentro da própria
        /// imagem — veja Sprites/logo.png) e, abaixo dela, só o subtítulo, que continua sendo
        /// texto para poder reaproveitar a fonte do jogo. Se a logo ainda não existir, cai de
        /// volta no selo desenhado em código de antes.
        /// </summary>
        private void DrawLogo(Graphics g)
        {
            int logoBottom;

            if (_logo != null)
            {
                const int logoWidth = 360;
                int logoHeight = (int)Math.Round(logoWidth * (_logo.Height / (float)_logo.Width));
                var logoRect = new Rectangle((ClientSize.Width - logoWidth) / 2, 20, logoWidth, logoHeight);

                g.DrawImage(_logo, logoRect);
                logoBottom = logoRect.Bottom;
            }
            else
            {
                logoBottom = DrawFallbackBadgeAndTitle(g);
            }

            var subtitleRect = new Rectangle(0, logoBottom + 10, ClientSize.Width, 28);

            // Painel translúcido atrás do subtítulo, no mesmo estilo do HUD do jogo, para o
            // texto continuar legível não importa qual parte do quarto fique atrás dele.
            var panelRect = new Rectangle((ClientSize.Width - 460) / 2, subtitleRect.Y - 2, 460, 26);
            using (GraphicsPath panelPath = RoundedRect(panelRect, 12))
            using (var panelBrush = new SolidBrush(Color.FromArgb(130, 30, 20, 14)))
                g.FillPath(panelBrush, panelPath);

            var subtitleFormat = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            using (var subtitleFont = GameFonts.Get(7f, FontStyle.Italic))
            using (var subtitleBrush = new SolidBrush(SubtitleColor))
            {
                g.DrawString("Um jogo educativo sobre eletricidade em casa", subtitleFont, subtitleBrush, panelRect, subtitleFormat);
            }
        }

        /// <summary>Reserva: o antigo selo desenhado em código, usado só se Sprites/logo.png não existir ainda.</summary>
        private int DrawFallbackBadgeAndTitle(Graphics g)
        {
            var badgeFill = Color.FromArgb(224, 142, 53);
            var badgeBorder = Color.FromArgb(90, 55, 20);
            var titleColor = Color.FromArgb(240, 178, 90);
            var titleShadow = Color.FromArgb(20, 12, 6);

            var badgeCenter = new Point(ClientSize.Width / 2, 130);
            const int badgeRadius = 62;
            var badgeRect = new Rectangle(badgeCenter.X - badgeRadius, badgeCenter.Y - badgeRadius, badgeRadius * 2, badgeRadius * 2);

            using (var badgeBrush = new SolidBrush(badgeFill))
            using (var badgeBorderPen = new Pen(badgeBorder, 5))
            {
                g.FillEllipse(badgeBrush, badgeRect);
                g.DrawEllipse(badgeBorderPen, badgeRect);
            }

            using (GraphicsPath bolt = BoltPath(badgeCenter, badgeRadius * 0.62f))
            using (var boltBrush = new SolidBrush(Color.FromArgb(250, 236, 204)))
            {
                g.FillPath(boltBrush, bolt);
            }

            var titleRect = new Rectangle(0, 208, ClientSize.Width, 90);
            var titleFormat = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Near };

            using (var titleFont = GameFonts.Get(16f, FontStyle.Bold))
            {
                const string title = "SEGURANÇA NO USO\nDE ELETRICIDADE";

                var shadowRect = titleRect;
                shadowRect.Offset(3, 3);
                using (var shadowBrush = new SolidBrush(titleShadow))
                    g.DrawString(title, titleFont, shadowBrush, shadowRect, titleFormat);

                using (var titleBrush = new SolidBrush(titleColor))
                    g.DrawString(title, titleFont, titleBrush, titleRect, titleFormat);
            }

            return titleRect.Bottom;
        }

        private static GraphicsPath BoltPath(Point center, float size)
        {
            PointF[] points =
            {
                new PointF(center.X + size * 0.15f, center.Y - size),
                new PointF(center.X - size * 0.55f, center.Y + size * 0.15f),
                new PointF(center.X - size * 0.05f, center.Y + size * 0.15f),
                new PointF(center.X - size * 0.15f, center.Y + size),
                new PointF(center.X + size * 0.55f, center.Y - size * 0.15f),
                new PointF(center.X + size * 0.05f, center.Y - size * 0.15f)
            };
            var path = new GraphicsPath();
            path.AddPolygon(points);
            return path;
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
