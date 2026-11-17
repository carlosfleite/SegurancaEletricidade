using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;
using SegurancaEletricaEmCasa.Data;
using SegurancaEletricaEmCasa.Entities;
using SegurancaEletricaEmCasa.Game;
using SegurancaEletricaEmCasa.UI;

namespace SegurancaEletricaEmCasa.Forms
{
    public class MainForm : Form
    {
        private readonly System.Windows.Forms.Timer _gameTimer;
        private readonly Stopwatch _stopwatch = new Stopwatch();
        private long _lastElapsedTicks;

        private readonly HashSet<Keys> _pressedKeys = new HashSet<Keys>();

        private GameManager _gameManager;
        private QuizOverlay _quizOverlay;
        private Rectangle _roomBounds;

        // --- Sprites -------------------------------------------------------
        // Os bitmaps abaixo são gerados em código (retângulos coloridos) apenas para o
        // projeto RODAR e ser TESTÁVEL sem nenhum arquivo de arte ainda. Troque o método
        // LoadAssets() para carregar seus PNGs reais (Image.FromFile ou Properties.Resources)
        // assim que tiver os assets em pixel art prontos.
        private Bitmap _roomBackground;
        private GraphicsPath _floorPath; // contorno do piso do cômodo atual (ver Room.FloorPolygon)
        private Bitmap _playerSpriteDown;
        private Bitmap _playerSpriteUp;
        private Bitmap _playerSpriteLeft;
        private Bitmap _playerSpriteRight;

        // Enquanto um perigo está ativo ou danificado, o próprio fundo do cômodo (ex.: Sprites/quarto.png) já mostra
        // o problema (copo d'água, fio à mostra, etc.) — não desenhamos nenhum ícone por cima.
        // Só quando o jogador ACERTA o quiz é que colamos, por cima do fundo, o recorte da
        // versão "consertada" daquele pedaço exato do cômodo (ver Hazard.OverlayBounds).
        private readonly Dictionary<string, Bitmap> _hazardResolvedPatches = new Dictionary<string, Bitmap>();

        private Bitmap _heartFull;
        private Bitmap _heartEmpty;

        // Fases em ordem (quarto, banheiro, sala, cozinha — ver RoomFactory). Ao resolver todos
        // os perigos de um cômodo, a mensagem de vitória oferece o botão para ir ao próximo.
        private List<Room> _rooms;
        private int _roomIndex;
        private Room CurrentRoom => _rooms[_roomIndex];
        private bool IsLastRoom => _roomIndex == _rooms.Count - 1;

        // O aviso "Como jogar" aparece só na primeira vez que o jogo é aberto (static: vale para
        // toda a execução do programa). "Jogar de novo" e novas partidas pelo menu não repetem.
        private static bool _introShown;
        private bool _introOpen; // aviso "Como jogar" aberto (ainda não há progresso a perder)

        public MainForm()
        {
            Text = "Segurança Elétrica em Casa";
            ClientSize = new Size(1000, 650);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            Icon = Assets.AppIcon ?? Icon;
            DoubleBuffered = true;
            KeyPreview = true;

            _roomBounds = new Rectangle(0, 0, ClientSize.Width, ClientSize.Height);

            // SetupGame precisa vir antes de LoadAssets: é ele quem cria a lista de Hazards,
            // e LoadAssets usa o Id/OverlayBounds de cada um para carregar o recorte "consertado"
            // correspondente.
            SetupGame();
            LoadAssets();
            SetupQuizOverlay();
            SetupBackButton();
            SetupInput();

            _gameTimer = new System.Windows.Forms.Timer { Interval = 16 }; // ~60 FPS
            _gameTimer.Tick += GameLoop;
            _stopwatch.Start();
            _gameTimer.Start();
        }

        // ---------------------------------------------------------------
        // Configuração inicial
        // ---------------------------------------------------------------

        private void LoadAssets()
        {
            // Assim que você tiver os PNGs prontos, coloque-os em uma pasta "Sprites" dentro
            // do projeto com esses nomes exatos. Enquanto o arquivo não existir, o jogo
            // continua rodando normalmente com o retângulo colorido de sempre — então dá
            // para trocar os sprites um de cada vez e ir testando.
            LoadRoomAssets();

            // Um sprite por direção (baixo/cima/esquerda/direita) — MainForm.DrawPlayer escolhe
            // qual desenhar de acordo com Player.FacingDirection a cada frame.
            _playerSpriteDown = LoadOrPlaceholder("Sprites/personagem_baixo.png", Player.Width, Player.Height, Color.Crimson);
            _playerSpriteUp = LoadOrPlaceholder("Sprites/personagem_cima.png", Player.Width, Player.Height, Color.Crimson);
            _playerSpriteLeft = LoadOrPlaceholder("Sprites/personagem_esquerda.png", Player.Width, Player.Height, Color.Crimson);
            _playerSpriteRight = LoadOrPlaceholder("Sprites/personagem_direita.png", Player.Width, Player.Height, Color.Crimson);

            _heartFull = LoadOrHeartPlaceholder("Sprites/coracao_cheio.png", 22, Color.FromArgb(214, 64, 64));
            _heartEmpty = LoadOrHeartPlaceholder("Sprites/coracao_vazio.png", 22, Color.FromArgb(96, 84, 68));
        }

        /// <summary>
        /// Carrega o fundo e os recortes "consertados" do cômodo atual, liberando os do cômodo
        /// anterior. Chamado no início e a cada troca de fase (ver GoToRoom).
        /// </summary>
        private void LoadRoomAssets()
        {
            _roomBackground?.Dispose();
            foreach (Bitmap patch in _hazardResolvedPatches.Values)
                patch.Dispose();
            _hazardResolvedPatches.Clear();

            _roomBackground = LoadOrPlaceholder(CurrentRoom.BackgroundSprite, ClientSize.Width, ClientSize.Height, Color.FromArgb(60, 40, 30));
            _floorPath?.Dispose();
            _floorPath = new GraphicsPath();
            _floorPath.AddPolygon(CurrentRoom.FloorPolygon);

            // Um recorte "consertado" por perigo, do tamanho exato do OverlayBounds dele —
            // arquivo esperado: Sprites/perigo_resolvido_<id>.png (ids em HazardFactory.cs).
            // Sem o arquivo ainda, cai num retângulo verde simples só pra não ficar invisível
            // durante o teste.
            foreach (Hazard hazard in _gameManager.Hazards)
            {
                string path = $"Sprites/perigo_resolvido_{hazard.Id}.png";
                _hazardResolvedPatches[hazard.Id] = LoadOrPlaceholder(
                    path, hazard.OverlayBounds.Width, hazard.OverlayBounds.Height, Color.FromArgb(140, 200, 140));
            }

            Text = $"Segurança Elétrica em Casa - {CurrentRoom.Name}";
        }

        /// <summary>
        /// Carrega um PNG real se ele existir na pasta de saída (bin\Debug\Sprites\...), sem
        /// travar o arquivo (ver Assets.LoadBitmapOrNull); caso contrário, gera o retângulo
        /// colorido de placeholder para o jogo não quebrar.
        /// </summary>
        private static Bitmap LoadOrPlaceholder(string relativePath, int width, int height, Color fallbackColor)
        {
            return Assets.LoadBitmapOrNull(relativePath) ?? CreatePlaceholder(width, height, fallbackColor);
        }

        /// <summary>Igual ao LoadOrPlaceholder, mas o placeholder é um coração desenhado, não um retângulo.</summary>
        private static Bitmap LoadOrHeartPlaceholder(string relativePath, int targetHeight, Color fillColor)
        {
            return Assets.LoadBitmapOrNull(relativePath) ?? CreateHeartPlaceholder(targetHeight, fillColor);
        }

        private static Bitmap CreatePlaceholder(int width, int height, Color color)
        {
            var bitmap = new Bitmap(width, height);
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.Clear(color);
                g.DrawRectangle(Pens.Black, 0, 0, width - 1, height - 1);
            }
            return bitmap;
        }

        /// <summary>
        /// Desenho do coração em "pixel art" (blocos quadrados, sem anti-aliasing), no estilo
        /// clássico 8-bit, em vez de curvas suaves — fica mais parecido com um coração de
        /// verdade em tamanhos pequenos do que a versão anterior feita com círculos.
        /// Cada caractere 'X' vira um bloco preenchido; '.' fica vazio (transparente).
        /// </summary>
        private static readonly string[] HeartPattern =
        {
            ".XX.XX.",
            "XXXXXXX",
            "XXXXXXX",
            ".XXXXX.",
            "..XXX..",
            "...X...",
        };

        private static Bitmap CreateHeartPlaceholder(int targetHeight, Color fill)
        {
            int rows = HeartPattern.Length;
            int cols = HeartPattern[0].Length;
            int blockSize = Math.Max(1, targetHeight / rows);

            var bitmap = new Bitmap(cols * blockSize, rows * blockSize);
            Color outline = DarkenColor(fill, 0.45f);

            bool IsOn(int r, int c) => r >= 0 && r < rows && c >= 0 && c < cols && HeartPattern[r][c] == 'X';

            using (Graphics g = Graphics.FromImage(bitmap))
            using (var fillBrush = new SolidBrush(fill))
            using (var outlineBrush = new SolidBrush(outline))
            {
                g.SmoothingMode = SmoothingMode.None; // bordas duras de propósito (visual pixel art)
                g.PixelOffsetMode = PixelOffsetMode.Half;

                for (int r = 0; r < rows; r++)
                {
                    for (int c = 0; c < cols; c++)
                    {
                        if (!IsOn(r, c)) continue;

                        // Blocos na borda da silhueta (vizinho vazio de algum lado) ficam com a
                        // cor mais escura, formando um contorno; o miolo usa a cor de preenchimento.
                        bool isEdge = !IsOn(r - 1, c) || !IsOn(r + 1, c) || !IsOn(r, c - 1) || !IsOn(r, c + 1);
                        Brush brush = isEdge ? outlineBrush : fillBrush;
                        g.FillRectangle(brush, c * blockSize, r * blockSize, blockSize, blockSize);
                    }
                }
            }

            return bitmap;
        }

        private static Color DarkenColor(Color color, float factor)
        {
            int r = (int)(color.R * (1f - factor));
            int g = (int)(color.G * (1f - factor));
            int b = (int)(color.B * (1f - factor));
            return Color.FromArgb(color.A, Math.Max(r, 0), Math.Max(g, 0), Math.Max(b, 0));
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

        private void SetupGame()
        {
            _rooms = RoomFactory.CreateRooms();
            _roomIndex = 0;

            var player = new Player(CurrentRoom.PlayerStart);
            _gameManager = new GameManager(player, CurrentRoom.Hazards);
            _gameManager.QuizManager.QuizOpened += hazard => _quizOverlay.ShowQuestion(hazard.Question);
            _gameManager.QuizManager.QuizAnswered += (hazard, wasCorrect) => Invalidate();
            _gameManager.LivesChanged += Invalidate;
            _gameManager.ProgressChanged += Invalidate;
            _gameManager.GameOver += OnGameOver;
            _gameManager.Victory += OnVictory;
        }

        private void SetupQuizOverlay()
        {
            _quizOverlay = new QuizOverlay { Visible = false };

            // O painel muda de altura conforme o tamanho da pergunta/alternativas, então ele é
            // reposicionado (centralizado, perto da parte de baixo da tela) toda vez que muda.
            _quizOverlay.Resize += (s, e) => PositionQuizOverlay();
            _quizOverlay.OptionSelected += OnQuizOptionSelected;

            Controls.Add(_quizOverlay);
            PositionQuizOverlay();
        }

        private void PositionQuizOverlay()
        {
            int x = (ClientSize.Width - _quizOverlay.Width) / 2;
            int y = Math.Max(12, ClientSize.Height - _quizOverlay.Height - 16);
            _quizOverlay.Location = new Point(x, y);
        }

        /// <summary>
        /// Botão "Voltar" fixo no canto superior direito: fecha só esta janela do jogo.
        /// Quem reabre a tela inicial é o próprio StartForm, ouvindo o evento FormClosed.
        /// </summary>
        private void SetupBackButton()
        {
            var backButton = new PixelButton
            {
                Text = "Voltar",
                BaseColor = Color.FromArgb(140, 120, 170),
                HoverColor = Color.FromArgb(160, 140, 190),
                Font = GameFonts.Get(6f, FontStyle.Bold),
                SpriteImage = PixelButton.LoadSpriteOrNull("Sprites/botao_voltar.png")
            };

            // Largura fixa igual à do layout antigo; a altura acompanha a proporção real da
            // arte para não distorcer o pixel art (sem imagem ainda, cai no 96x32 de antes).
            const int width = 96;
            int height = 32;
            if (backButton.SpriteImage != null)
                height = (int)Math.Round(width * (backButton.SpriteImage.Height / (float)backButton.SpriteImage.Width));

            backButton.Size = new Size(width, height);
            backButton.Location = new Point(ClientSize.Width - width - 16, 12);
            backButton.Click += (s, e) => ConfirmarVoltar();

            Controls.Add(backButton);
            backButton.BringToFront();
        }

        /// <summary>
        /// Pede confirmação antes de sair da partida (o progresso do cômodo é perdido), usando o
        /// mesmo painel em pixel art do quiz. Pausa o loop enquanto o painel está aberto.
        /// </summary>
        private void ConfirmarVoltar()
        {
            // Se já há um painel aberto (pergunta, fim de jogo, vitória), ele tem seus próprios
            // botões; um segundo painel por cima o apagaria.
            if (_introOpen)
            {
                Close();
                return;
            }
            if (_quizOverlay.Visible) return;

            _gameTimer.Stop();
            _pressedKeys.Clear();

            _quizOverlay.ShowMessage(
                "VOLTAR AO MENU",
                "Se você voltar ao menu, o progresso deste cômodo será perdido. Deseja voltar?",
                QuizOverlay.DefaultTextColor,
                new[] { "SIM, VOLTAR", "CONTINUAR JOGANDO" },
                choice =>
                {
                    if (choice == 0)
                    {
                        Close();
                        return;
                    }

                    _pressedKeys.Clear();
                    _lastElapsedTicks = _stopwatch.ElapsedTicks; // evita um "salto" de deltaTime
                    _gameTimer.Start();
                });
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);

            if (!_introShown)
            {
                _introShown = true;
                ShowIntro();
            }
        }

        /// <summary>
        /// Aviso inicial com as regras, no mesmo painel do quiz. O jogo fica pausado até o
        /// jogador clicar em "VAMOS LÁ!".
        /// </summary>
        private void ShowIntro()
        {
            _gameTimer.Stop();
            _pressedKeys.Clear();
            _introOpen = true;

            _quizOverlay.ShowMessage(
                "COMO JOGAR",
                "Olá! Esta casa tem perigos com a eletricidade. Ajude a deixar todos os cômodos seguros!\n\n" +
                "- Ande com as setas ou W, A, S, D.\n" +
                "- Chegue perto de um perigo e responda à pergunta.\n" +
                "- Acertou? O perigo é consertado!\n" +
                "- Errou? Você leva um choque e perde um coração.\n" +
                "- Conserte tudo para ir ao próximo cômodo.",
                QuizOverlay.DefaultTextColor,
                new[] { "VAMOS LÁ!" },
                choice =>
                {
                    _introOpen = false;
                    _lastElapsedTicks = _stopwatch.ElapsedTicks; // evita um "salto" de deltaTime
                    _gameTimer.Start();
                });
        }

        /// <summary>Garante que o Timer pare junto com a janela (evita erro de "objeto descartado").</summary>
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _gameTimer.Stop();
            base.OnFormClosing(e);
        }

        private void SetupInput()
        {
            // WASD chega normalmente por aqui: letras nunca são tratadas como "tecla de
            // diálogo" pelo WinForms, então o KeyDown do formulário sempre recebe.
            KeyDown += (s, e) => _pressedKeys.Add(e.KeyCode);
            KeyUp += (s, e) => _pressedKeys.Remove(e.KeyCode);

            // Se a janela perder o foco com uma tecla apertada (ex.: MessageBox, Alt+Tab), o
            // KeyUp nunca chega e o personagem ficaria andando sozinho. Limpamos tudo aqui.
            Deactivate += (s, e) => _pressedKeys.Clear();
        }

        /// <summary>
        /// As setas (Up/Down/Left/Right) são um caso especial no WinForms: assim que qualquer
        /// Button ganha foco (como o botão "Voltar"), o framework passa a tratá-las como
        /// "teclas de navegação de diálogo" — usadas para mover o foco entre controles — em vez
        /// de entregá-las ao KeyDown do formulário. É por isso que, depois de clicar em algum
        /// botão, o jogo passava a responder só ao WASD. Interceptando aqui, em ProcessCmdKey
        /// (que roda ANTES dessa navegação de foco), garantimos que as setas sempre cheguem ao
        /// jogo, não importa qual controle esteja com foco no momento.
        /// </summary>
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            switch (keyData)
            {
                case Keys.Up:
                case Keys.Down:
                case Keys.Left:
                case Keys.Right:
                    _pressedKeys.Add(keyData);
                    return true; // "consumida": não deixa virar navegação de foco nem apitar
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        // ---------------------------------------------------------------
        // Eventos vindos do GameManager / QuizManager
        // ---------------------------------------------------------------

        private void OnQuizOptionSelected(int optionIndex)
        {
            _quizOverlay.HideQuiz();
            _pressedKeys.Clear(); // teclas soltas enquanto o quiz estava aberto não "grudam"
            _gameManager.QuizManager.SubmitAnswer(optionIndex);
        }

        private void OnGameOver()
        {
            _gameTimer.Stop();
            _pressedKeys.Clear();

            _quizOverlay.ShowMessage(
                "FIM DE JOGO",
                "Você levou choques demais e ficou sem vidas. Lembre-se: com eletricidade, " +
                "todo cuidado é pouco! Quer tentar de novo?",
                QuizOverlay.FailureTextColor,
                new[] { "TENTAR NOVAMENTE", "VOLTAR AO MENU" },
                choice =>
                {
                    if (choice == 0) RestartGame();
                    else Close();
                });
        }

        private void OnVictory()
        {
            _gameTimer.Stop();
            _pressedKeys.Clear();

            string roomMessage =
                $"Você identificou e corrigiu todos os {_gameManager.TotalHazards} perigos elétricos " +
                $"deste cômodo. {CurrentRoom.SafeMessage}";

            if (!IsLastRoom)
            {
                Room nextRoom = _rooms[_roomIndex + 1];

                _quizOverlay.ShowMessage(
                    "PARABÉNS!",
                    $"{roomMessage} Próximo cômodo: {nextRoom.Name}.",
                    QuizOverlay.SuccessTextColor,
                    new[] { "PRÓXIMA FASE", "VOLTAR AO MENU" },
                    choice =>
                    {
                        if (choice == 0) GoToRoom(_roomIndex + 1);
                        else Close();
                    });
                return;
            }

            _quizOverlay.ShowMessage(
                "CASA SEGURA!",
                $"{roomMessage} Você deixou todos os {_rooms.Count} cômodos da casa livres de perigos elétricos!",
                QuizOverlay.SuccessTextColor,
                new[] { "JOGAR DE NOVO", "VOLTAR AO MENU" },
                choice =>
                {
                    if (choice == 0) GoToRoom(0);
                    else Close();
                });
        }

        private void RestartGame()
        {
            GoToRoom(_roomIndex);
        }

        /// <summary>
        /// Começa (ou recomeça) a fase do índice informado: troca os perigos no GameManager,
        /// recarrega o fundo/recortes do cômodo e volta o jogador à posição inicial dele.
        /// </summary>
        private void GoToRoom(int index)
        {
            bool roomChanged = index != _roomIndex;
            _roomIndex = index;

            _gameManager.LoadRoom(CurrentRoom.Hazards, CurrentRoom.PlayerStart);
            if (roomChanged)
                LoadRoomAssets();

            _pressedKeys.Clear();
            _lastElapsedTicks = _stopwatch.ElapsedTicks; // evita um "salto" de deltaTime
            _gameTimer.Start();
            Invalidate();
        }

        // ---------------------------------------------------------------
        // Game loop
        // ---------------------------------------------------------------

        private void GameLoop(object sender, EventArgs e)
        {
            long currentTicks = _stopwatch.ElapsedTicks;
            float deltaTime = (currentTicks - _lastElapsedTicks) / (float)Stopwatch.Frequency;
            _lastElapsedTicks = currentTicks;

            bool up = _pressedKeys.Contains(Keys.W) || _pressedKeys.Contains(Keys.Up);
            bool down = _pressedKeys.Contains(Keys.S) || _pressedKeys.Contains(Keys.Down);
            bool left = _pressedKeys.Contains(Keys.A) || _pressedKeys.Contains(Keys.Left);
            bool right = _pressedKeys.Contains(Keys.D) || _pressedKeys.Contains(Keys.Right);

            _gameManager.Update(deltaTime, up, down, left, right, _roomBounds, IsFloorWalkable);

            Invalidate(); // solicita um novo OnPaint
        }

        /// <summary>
        /// Diz se um ponto (normalmente os "pés" do jogador) está sobre o piso do cômodo. O piso
        /// de cada cômodo é um losango definido em RoomFactory (Room.FloorPolygon), então o
        /// jogador não consegue subir pelas paredes nem sair da silhueta do cômodo.
        /// </summary>
        private bool IsFloorWalkable(PointF point)
        {
            return _floorPath == null || _floorPath.IsVisible(point);
        }

        // ---------------------------------------------------------------
        // Renderização (equivalente ao "OnRender"/"Update visual" de uma engine)
        // ---------------------------------------------------------------

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            Graphics g = e.Graphics;
            g.InterpolationMode = InterpolationMode.NearestNeighbor; // mantém a cara "pixel art"
            g.PixelOffsetMode = PixelOffsetMode.Half;

            g.DrawImage(_roomBackground, 0, 0, ClientSize.Width, ClientSize.Height);

            foreach (Hazard hazard in _gameManager.Hazards)
                DrawHazard(g, hazard);

            DrawPlayer(g);
            DrawHud(g);
        }

        /// <summary>
        /// Ativo/danificado: não desenha nada — o fundo do cômodo já mostra o problema.
        /// Resolvido: cola o recorte "consertado" (Sprites/perigo_resolvido_&lt;id&gt;.png) por
        /// cima do fundo, exatamente na região de OverlayBounds, cobrindo o objeto original.
        /// </summary>
        private void DrawHazard(Graphics g, Hazard hazard)
        {
            if (hazard.State != HazardState.Resolved)
                return;

            if (_hazardResolvedPatches.TryGetValue(hazard.Id, out Bitmap patch))
                g.DrawImage(patch, hazard.OverlayBounds);
        }

        /// <summary>Escolhe o sprite correto (baixo/cima/esquerda/direita) para a direção atual do jogador.</summary>
        private Bitmap GetPlayerSprite(Direction direction)
        {
            switch (direction)
            {
                case Direction.Up:
                    return _playerSpriteUp;
                case Direction.Left:
                    return _playerSpriteLeft;
                case Direction.Right:
                    return _playerSpriteRight;
                default:
                    return _playerSpriteDown;
            }
        }

        private void DrawPlayer(Graphics g)
        {
            Rectangle bounds = _gameManager.Player.Bounds;
            Bitmap sprite = GetPlayerSprite(_gameManager.Player.FacingDirection);

            if (_gameManager.Player.IsShocked)
            {
                // Efeito simples de "choque": pisca um tingimento amarelo sobre o sprite,
                // alternando a cada ~80ms. Usa ColorMatrix (em vez de preencher um retângulo)
                // para respeitar a transparência real do PNG — só os pixels do personagem
                // ficam amarelados, o resto do sprite continua invisível.
                bool flash = (_stopwatch.ElapsedMilliseconds / 80) % 2 == 0;

                if (flash)
                {
                    float[][] matrixItems =
                    {
                        new float[] { 1.5f, 0, 0, 0, 0 },
                        new float[] { 0, 1.5f, 0, 0, 0 },
                        new float[] { 0, 0, 0.25f, 0, 0 },
                        new float[] { 0, 0, 0, 1, 0 },
                        new float[] { 0, 0, 0, 0, 1 }
                    };

                    using (var attributes = new ImageAttributes())
                    {
                        attributes.SetColorMatrix(new ColorMatrix(matrixItems));
                        g.DrawImage(sprite, bounds, 0, 0, sprite.Width, sprite.Height, GraphicsUnit.Pixel, attributes);
                    }
                }
                else
                {
                    g.DrawImage(sprite, bounds);
                }
            }
            else
            {
                g.DrawImage(sprite, bounds);
            }
        }

        /// <summary>
        /// HUD desenhado sobre um painel arredondado semi-transparente (para não "brigar"
        /// visualmente com o cenário atrás), com corações de verdade, texto de progresso na
        /// fonte do jogo e um "pip" (quadradinho) por perigo — aceso quando resolvido.
        /// </summary>
        private void DrawHud(Graphics g)
        {
            var panelRect = new Rectangle(12, 12, 260, 120);

            using (GraphicsPath panelPath = RoundedRect(panelRect, 14))
            using (var panelBrush = new SolidBrush(Color.FromArgb(150, 40, 26, 16)))
            using (var panelBorder = new Pen(Color.FromArgb(190, 120, 74, 34), 2))
            {
                g.FillPath(panelBrush, panelPath);
                g.DrawPath(panelBorder, panelPath);
            }

            for (int i = 0; i < GameManager.MaxLives; i++)
            {
                Bitmap heart = i < _gameManager.Lives ? _heartFull : _heartEmpty;

                // Os corações não são mais quadrados (o desenho em pixel art é mais largo que
                // alto), então preservamos a proporção real do bitmap em vez de forçar 24x24 —
                // isso também deixa certo automaticamente se um PNG real, com outra proporção,
                // for colocado em Sprites/coracao_cheio.png e coracao_vazio.png.
                const int heartHeight = 22;
                int heartWidth = (int)Math.Round(heartHeight * (heart.Width / (float)heart.Height));
                g.DrawImage(heart, panelRect.X + 14 + i * 30, panelRect.Y + 10, heartWidth, heartHeight);
            }

            string roomText = $"Fase {_roomIndex + 1}/{_rooms.Count}: {CurrentRoom.Name}";
            string progressText = $"Resolvidos: {_gameManager.ResolvedCount}/{_gameManager.TotalHazards}";
            using (var font = GameFonts.Get(6.5f, FontStyle.Bold))
            {
                TextRenderer.DrawText(g, roomText, font,
                    new Point(panelRect.X + 14, panelRect.Y + 42),
                    Color.FromArgb(240, 170, 90));
                TextRenderer.DrawText(g, progressText, font,
                    new Point(panelRect.X + 14, panelRect.Y + 64),
                    Color.FromArgb(250, 236, 204));
            }

            // Pips de progresso: um quadradinho por perigo, preenchido de laranja quando resolvido.
            const int pipSize = 18;
            const int pipGap = 6;
            int pipsX = panelRect.X + 14;
            int pipsY = panelRect.Y + 90;

            for (int i = 0; i < _gameManager.Hazards.Count; i++)
            {
                var pipRect = new Rectangle(pipsX + i * (pipSize + pipGap), pipsY, pipSize, pipSize);
                bool resolved = _gameManager.Hazards[i].IsResolved;

                using (GraphicsPath pipPath = RoundedRect(pipRect, 4))
                using (var fillBrush = new SolidBrush(resolved ? Color.FromArgb(224, 142, 53) : Color.FromArgb(70, 55, 45)))
                using (var borderPen = new Pen(Color.FromArgb(250, 236, 204), 1.4f))
                {
                    g.FillPath(fillBrush, pipPath);
                    g.DrawPath(borderPen, pipPath);
                }
            }
        }
    }
}
