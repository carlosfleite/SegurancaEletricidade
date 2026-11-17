using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;
using SegurancaEletricaEmCasa.Quiz;

namespace SegurancaEletricaEmCasa.UI
{
    /// <summary>
    /// Caixa de diálogo do quiz, desenhada com a arte em pixel art de Sprites/quiz_painel.png
    /// (moldura de madeira + faixa do título + pergaminho) e Sprites/quiz_opcao_*.png (barras
    /// laranja/lilás das alternativas). Todas as imagens são desenhadas em "9-slice"
    /// (ver <see cref="NineSlice"/>), então o painel cresce sozinho conforme o tamanho da
    /// pergunta e das alternativas, sem distorcer as bordas.
    ///
    /// Fluxo: ShowQuestion → jogador escolhe uma alternativa → o painel mostra o feedback
    /// (acertou/errou + explicação da QuizQuestion) com o botão CONTINUAR → só então o evento
    /// OptionSelected é disparado. Se a pergunta não tiver texto de feedback, o evento é
    /// disparado direto no clique.
    ///
    /// Se algum PNG não existir, o painel e as barras são gerados em código com o visual antigo
    /// (pergaminho + faixa degradê), para o jogo continuar funcionando.
    /// </summary>
    public class QuizOverlay : UserControl
    {
        public const int PanelWidth = 620;

        // Medidas dos sprites (em pixels do PNG): cantos/bordas que não podem ser esticados.
        private static readonly Padding PanelSlice = new Padding(32, 88, 32, 24);
        private static readonly Padding OptionSlice = new Padding(14, 12, 14, 16);
        private static readonly Size PanelSpriteSize = new Size(423, 382);
        private static readonly Size OptionSpriteSize = new Size(342, 62);

        // Faixa do título dentro do painel (y) e área útil do pergaminho.
        private const int HeaderTextTop = 12;
        private const int HeaderTextBottom = 66;
        private const int HeaderTextLeft = 30;
        private const int ContentLeft = 46;
        private const int ContentRight = 46;
        private const int ContentTop = 102;
        private const int ContentBottom = 40;
        private const int PromptToOptionsGap = 16;

        // Área de texto dentro de cada barra de alternativa (descontando contorno e sombra).
        private const int OptionMinHeight = 62;
        private const int OptionGap = 6;
        private const int OptionTextPaddingX = 22;
        private const int OptionTextTop = 9;
        private const int OptionTextBottom = 17;

        private static readonly Color TitleColor = Color.FromArgb(250, 236, 204);
        private static readonly Color TitleShadow = Color.FromArgb(96, 42, 18);
        private static readonly Color PromptColor = Color.FromArgb(60, 38, 18);
        private static readonly Color CorrectColor = Color.FromArgb(46, 120, 60);
        private static readonly Color WrongColor = Color.FromArgb(170, 50, 36);
        private static readonly Color OptionTextColor = Color.FromArgb(255, 250, 238);

        // Uma entrada por "estilo" de alternativa (A, B, C, D, ...), repetindo se houver mais.
        private static readonly OptionStyle[] OptionStyles =
        {
            new OptionStyle("Sprites/quiz_opcao_laranja.png", Color.FromArgb(241, 120, 8), Color.FromArgb(96, 38, 4)),
            new OptionStyle("Sprites/quiz_opcao_lilas.png", Color.FromArgb(193, 133, 197), Color.FromArgb(64, 22, 84))
        };

        private static Bitmap _panelSprite;

        private readonly List<OptionItem> _options = new List<OptionItem>();
        private readonly Timer _animationTimer;
        private readonly Font _titleFont;
        private readonly Font _promptFont;
        private readonly Font _optionFont;
        private readonly int _promptLineHeight;
        private readonly int _optionLineHeight;

        private QuizQuestion _question;
        private Action<int> _messageCallback;
        private bool _showingFeedback;
        private bool _answerSent;
        private int _selectedOption;
        private string _title = "QUIZ";
        private Color _promptColor = PromptColor;
        private List<string> _promptLines = new List<string>();

        private Bitmap _panelCache;
        private int _hoverIndex = -1;
        private int _pressedIndex = -1;

        /// <summary>Disparado quando o jogador confirma uma alternativa (0 = A, 1 = B, ...).</summary>
        public event Action<int> OptionSelected;

        public QuizOverlay()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            SetStyle(ControlStyles.Selectable, false);
            TabStop = false;

            // Só aparece por trás de pixels semitransparentes das bordas (a Region recorta o resto).
            BackColor = Color.FromArgb(40, 20, 8);

            _titleFont = GameFonts.Get(12f, FontStyle.Bold);
            // Título na fonte pixel (combina com a moldura); pergunta e alternativas numa fonte
            // de leitura, maior e mais nítida, para frases longas não cansarem a vista.
            _promptFont = GameFonts.GetReadable(12f, FontStyle.Bold);
            _optionFont = GameFonts.GetReadable(11f, FontStyle.Bold);

            const float leading = 1.12f;
            _promptLineHeight = (int)Math.Ceiling(TextHeight(_promptFont) * leading);
            _optionLineHeight = (int)Math.Ceiling(TextHeight(_optionFont) * leading);

            _animationTimer = new Timer { Interval = 15 };
            _animationTimer.Tick += AnimationTick;
        }

        // ---------------------------------------------------------------
        // API pública
        // ---------------------------------------------------------------

        /// <summary>Monta o painel para a pergunta e mostra o overlay.</summary>
        public void ShowQuestion(QuizQuestion question)
        {
            _question = question;
            _messageCallback = null;
            _showingFeedback = false;
            _answerSent = false;
            _selectedOption = -1;
            _title = "QUIZ";
            _promptColor = PromptColor;

            var labels = new List<string>();
            for (int i = 0; i < question.Options.Length; i++)
                labels.Add($"{(char)('A' + i)}) {question.Options[i]}");

            BuildLayout(question.Prompt, labels);

            Visible = true;
            BringToFront();
        }

        /// <summary>
        /// Usa o mesmo painel do quiz para uma mensagem com botões próprios (ex.: vitória e
        /// game over). Ao clicar, o painel some e onChoice recebe o índice do botão (0, 1, ...).
        /// </summary>
        public void ShowMessage(string title, string message, Color messageColor, string[] buttons, Action<int> onChoice)
        {
            _question = null;
            _messageCallback = onChoice;
            _showingFeedback = false;
            _answerSent = false;
            _title = title;
            _promptColor = messageColor;

            BuildLayout(message, buttons);

            Visible = true;
            BringToFront();
        }

        /// <summary>Cores prontas para o texto de mensagens (mesmas do feedback de acerto/erro).</summary>
        public static Color DefaultTextColor => PromptColor;
        public static Color SuccessTextColor => CorrectColor;
        public static Color FailureTextColor => WrongColor;

        public void HideQuiz()
        {
            Visible = false;
            _hoverIndex = -1;
            _pressedIndex = -1;
            _animationTimer.Stop();
        }

        // ---------------------------------------------------------------
        // Layout
        // ---------------------------------------------------------------

        private void BuildLayout(string prompt, IList<string> optionLabels)
        {
            int contentWidth = PanelWidth - ContentLeft - ContentRight;

            _promptLines = WrapText(prompt, _promptFont, contentWidth);
            int y = ContentTop + _promptLines.Count * _promptLineHeight + PromptToOptionsGap;

            foreach (OptionItem old in _options)
                old.Dispose();
            _options.Clear();

            for (int i = 0; i < optionLabels.Count; i++)
            {
                OptionStyle style = OptionStyles[i % OptionStyles.Length];
                List<string> lines = WrapText(optionLabels[i], _optionFont, contentWidth - OptionTextPaddingX * 2);

                int textHeight = lines.Count * _optionLineHeight;
                int height = Math.Max(OptionMinHeight, textHeight + OptionTextTop + OptionTextBottom + 12);
                var bounds = new Rectangle(ContentLeft, y, contentWidth, height);

                _options.Add(new OptionItem
                {
                    Lines = lines,
                    Bounds = bounds,
                    Style = style,
                    Image = NineSlice.Render(style.Sprite, bounds.Size, OptionSlice)
                });

                y += height + OptionGap;
            }

            int totalHeight = y - OptionGap + ContentBottom;
            RebuildPanel(new Size(PanelWidth, totalHeight));

            _hoverIndex = HitTest(PointToClient(Cursor.Position));
            UpdateCursor();
            Invalidate();
        }

        /// <summary>Renderiza a moldura no tamanho novo e recorta o controle no formato dela.</summary>
        private void RebuildPanel(Size size)
        {
            _panelCache?.Dispose();
            _panelCache = NineSlice.Render(PanelSprite, size, PanelSlice);

            Region oldRegion = Region;
            Size = size;
            Region = NineSlice.RegionFromAlpha(_panelCache);
            oldRegion?.Dispose();
        }

        /// <summary>Quebra o texto em linhas que caibam em maxWidth (quebra por palavra).</summary>
        private static List<string> WrapText(string text, Font font, int maxWidth)
        {
            var lines = new List<string>();
            if (string.IsNullOrEmpty(text))
                return lines;

            foreach (string paragraph in text.Replace("\r", "").Split('\n'))
            {
                string current = "";
                foreach (string word in paragraph.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    string candidate = current.Length == 0 ? word : current + " " + word;
                    if (current.Length > 0 && TextWidth(candidate, font) > maxWidth)
                    {
                        lines.Add(current);
                        current = word;
                    }
                    else
                    {
                        current = candidate;
                    }
                }
                lines.Add(current);
            }

            return lines;
        }

        private static int TextWidth(string text, Font font) =>
            TextRenderer.MeasureText(text, font, Size.Empty, TextFormatFlags.NoPadding | TextFormatFlags.SingleLine).Width;

        private static int TextHeight(Font font) =>
            TextRenderer.MeasureText("Ág", font, Size.Empty, TextFormatFlags.NoPadding).Height;

        // ---------------------------------------------------------------
        // Interação
        // ---------------------------------------------------------------

        private int HitTest(Point point)
        {
            for (int i = 0; i < _options.Count; i++)
            {
                // Ignora a faixa da sombra embaixo da barra.
                Rectangle hitArea = _options[i].Bounds;
                hitArea.Height -= 6;
                if (hitArea.Contains(point))
                    return i;
            }
            return -1;
        }

        /// <summary>Redesenha só a barra da alternativa (com folga para o deslocamento de "apertado").</summary>
        private void InvalidateOption(OptionItem item) => Invalidate(Rectangle.Inflate(item.Bounds, 0, 3));

        private void UpdateCursor() => Cursor = _hoverIndex >= 0 ? Cursors.Hand : Cursors.Default;

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            int hit = HitTest(e.Location);
            if (hit != _hoverIndex)
            {
                _hoverIndex = hit;
                UpdateCursor();
                _animationTimer.Start();
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _hoverIndex = -1;
            _pressedIndex = -1;
            UpdateCursor();
            _animationTimer.Start();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left) return;

            _pressedIndex = HitTest(e.Location);
            if (_pressedIndex >= 0)
                InvalidateOption(_options[_pressedIndex]);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (e.Button != MouseButtons.Left) return;

            int pressed = _pressedIndex;
            _pressedIndex = -1;

            if (pressed >= 0)
            {
                InvalidateOption(_options[pressed]);
                if (HitTest(e.Location) == pressed)
                    OnOptionClicked(pressed);
            }
        }

        private void OnOptionClicked(int index)
        {
            if (_messageCallback != null)
            {
                Action<int> callback = _messageCallback;
                _messageCallback = null;
                HideQuiz();
                callback(index);
                return;
            }

            if (_question == null || _answerSent)
                return;

            if (_showingFeedback)
            {
                SendAnswer();
                return;
            }

            _selectedOption = index;
            bool correct = index == _question.CorrectOptionIndex;
            string feedback = correct ? _question.CorrectFeedback : _question.WrongFeedback;

            if (string.IsNullOrWhiteSpace(feedback))
            {
                SendAnswer();
                return;
            }

            _showingFeedback = true;
            _title = correct ? "ACERTOU!" : "ERROU!";
            _promptColor = correct ? CorrectColor : WrongColor;
            BuildLayout(feedback, new[] { "CONTINUAR" });
        }

        private void SendAnswer()
        {
            _answerSent = true;
            OptionSelected?.Invoke(_selectedOption);
        }

        private void AnimationTick(object sender, EventArgs e)
        {
            const float step = 0.18f;
            bool animating = false;

            for (int i = 0; i < _options.Count; i++)
            {
                OptionItem item = _options[i];
                float target = i == _hoverIndex ? 1f : 0f;
                float before = item.Hover;

                if (item.Hover < target) item.Hover = Math.Min(target, item.Hover + step);
                else if (item.Hover > target) item.Hover = Math.Max(target, item.Hover - step);

                if (item.Hover != target) animating = true;
                if (item.Hover != before) InvalidateOption(item);
            }

            if (!animating)
                _animationTimer.Stop();
        }

        // ---------------------------------------------------------------
        // Desenho
        // ---------------------------------------------------------------

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.InterpolationMode = InterpolationMode.NearestNeighbor;
            g.PixelOffsetMode = PixelOffsetMode.Half;

            if (_panelCache != null)
                g.DrawImage(_panelCache, new Rectangle(Point.Empty, _panelCache.Size));

            DrawTitle(g);
            DrawPrompt(g);

            for (int i = 0; i < _options.Count; i++)
                DrawOption(g, _options[i], i == _pressedIndex);
        }

        private void DrawTitle(Graphics g)
        {
            int textHeight = TextHeight(_titleFont);
            int y = HeaderTextTop + (HeaderTextBottom - HeaderTextTop - textHeight) / 2;
            var position = new Point(HeaderTextLeft, y);

            DrawTextWithShadow(g, _title, _titleFont, position, TitleColor, TitleShadow, 2);
        }

        private void DrawPrompt(Graphics g)
        {
            int textHeight = TextHeight(_promptFont);
            int offset = (_promptLineHeight - textHeight) / 2;

            for (int i = 0; i < _promptLines.Count; i++)
            {
                var position = new Point(ContentLeft, ContentTop + i * _promptLineHeight + offset);
                TextRenderer.DrawText(g, _promptLines[i], _promptFont, position, _promptColor, TextFormatFlags.NoPadding);
            }
        }

        private void DrawOption(Graphics g, OptionItem item, bool pressed)
        {
            Rectangle bounds = item.Bounds;
            float brightness = pressed ? 0.9f : 1f + 0.12f * item.Hover;
            int pressOffset = pressed ? 2 : 0;

            var imageRect = new Rectangle(bounds.X, bounds.Y + pressOffset, bounds.Width, bounds.Height);
            if (Math.Abs(brightness - 1f) < 0.001f)
            {
                g.DrawImage(item.Image, imageRect);
            }
            else
            {
                float[][] matrix =
                {
                    new float[] { brightness, 0, 0, 0, 0 },
                    new float[] { 0, brightness, 0, 0, 0 },
                    new float[] { 0, 0, brightness, 0, 0 },
                    new float[] { 0, 0, 0, 1, 0 },
                    new float[] { 0, 0, 0, 0, 1 }
                };

                using (var attributes = new ImageAttributes())
                {
                    attributes.SetColorMatrix(new ColorMatrix(matrix));
                    g.DrawImage(item.Image, imageRect, 0, 0, item.Image.Width, item.Image.Height, GraphicsUnit.Pixel, attributes);
                }
            }

            int textHeight = TextHeight(_optionFont);
            int lineOffset = (_optionLineHeight - textHeight) / 2;
            int areaTop = bounds.Y + OptionTextTop + pressOffset;
            int areaHeight = bounds.Height - OptionTextTop - OptionTextBottom;
            int blockTop = areaTop + (areaHeight - item.Lines.Count * _optionLineHeight) / 2;

            for (int i = 0; i < item.Lines.Count; i++)
            {
                string line = item.Lines[i];
                int x = bounds.X + (bounds.Width - TextWidth(line, _optionFont)) / 2;
                var position = new Point(x, blockTop + i * _optionLineHeight + lineOffset);
                DrawTextWithShadow(g, line, _optionFont, position, OptionTextColor, item.Style.TextShadow, 1);
            }
        }

        private static void DrawTextWithShadow(Graphics g, string text, Font font, Point position, Color color, Color shadow, int shadowOffset)
        {
            var shadowPosition = new Point(position.X + shadowOffset, position.Y + shadowOffset);
            TextRenderer.DrawText(g, text, font, shadowPosition, shadow, TextFormatFlags.NoPadding);
            TextRenderer.DrawText(g, text, font, position, color, TextFormatFlags.NoPadding);
        }

        // ---------------------------------------------------------------
        // Sprites (com reserva desenhada em código)
        // ---------------------------------------------------------------

        private static Bitmap PanelSprite =>
            _panelSprite ?? (_panelSprite = Assets.LoadBitmapOrNull("Sprites/quiz_painel.png") ?? CreateFallbackPanel());

        /// <summary>Reserva: o visual antigo (pergaminho arredondado + faixa degradê), no mesmo tamanho do PNG.</summary>
        private static Bitmap CreateFallbackPanel()
        {
            var bitmap = new Bitmap(PanelSpriteSize.Width, PanelSpriteSize.Height, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;

                var body = new Rectangle(8, 60, bitmap.Width - 17, bitmap.Height - 61);
                using (GraphicsPath path = RoundedRect(body, 14))
                using (var fill = new SolidBrush(Color.FromArgb(250, 236, 204)))
                using (var border = new Pen(Color.FromArgb(120, 74, 34), 6))
                {
                    g.FillPath(fill, path);
                    g.DrawPath(border, path);
                }

                var header = new Rectangle(1, 10, bitmap.Width - 3, 58);
                using (GraphicsPath path = RoundedRect(header, 12))
                using (var fill = new LinearGradientBrush(header, Color.FromArgb(198, 124, 58), Color.FromArgb(160, 94, 40), LinearGradientMode.Vertical))
                using (var border = new Pen(Color.FromArgb(70, 40, 20), 3))
                {
                    g.FillPath(fill, path);
                    g.DrawPath(border, path);
                }
            }
            return bitmap;
        }

        /// <summary>Reserva: barra arredondada de cor sólida (estilo PixelButton), no mesmo tamanho do PNG.</summary>
        private static Bitmap CreateFallbackOption(Color color)
        {
            var bitmap = new Bitmap(OptionSpriteSize.Width, OptionSpriteSize.Height, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;

                var rect = new Rectangle(2, 2, bitmap.Width - 5, bitmap.Height - 12);
                using (GraphicsPath path = RoundedRect(rect, 8))
                using (var fill = new SolidBrush(color))
                using (var border = new Pen(Color.FromArgb(70, 40, 20), 3))
                {
                    g.FillPath(fill, path);
                    g.DrawPath(border, path);
                }
            }
            return bitmap;
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

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _animationTimer.Dispose();
                _titleFont.Dispose();
                _promptFont.Dispose();
                _optionFont.Dispose();
                _panelCache?.Dispose();
                foreach (OptionItem item in _options)
                    item.Dispose();
            }
            base.Dispose(disposing);
        }

        // ---------------------------------------------------------------
        // Tipos auxiliares
        // ---------------------------------------------------------------

        private sealed class OptionStyle
        {
            private readonly string _spritePath;
            private readonly Color _fallbackColor;
            private Bitmap _sprite;

            public Color TextShadow { get; }

            public OptionStyle(string spritePath, Color fallbackColor, Color textShadow)
            {
                _spritePath = spritePath;
                _fallbackColor = fallbackColor;
                TextShadow = textShadow;
            }

            public Bitmap Sprite =>
                _sprite ?? (_sprite = Assets.LoadBitmapOrNull(_spritePath) ?? CreateFallbackOption(_fallbackColor));
        }

        private sealed class OptionItem : IDisposable
        {
            public List<string> Lines;
            public Rectangle Bounds;
            public OptionStyle Style;
            public Bitmap Image;
            public float Hover;

            public void Dispose() => Image?.Dispose();
        }
    }
}
