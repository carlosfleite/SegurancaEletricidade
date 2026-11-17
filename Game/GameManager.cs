using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using SegurancaEletricaEmCasa.Entities;
using SegurancaEletricaEmCasa.Quiz;

namespace SegurancaEletricaEmCasa.Game
{
    public class GameManager
    {
        public const int MaxLives = 3;

        public Player Player { get; }
        public List<Hazard> Hazards { get; private set; }
        public QuizManager QuizManager { get; }

        public int Lives { get; private set; } = MaxLives;
        public int TotalHazards => Hazards.Count;
        public int ResolvedCount => Hazards.Count(h => h.IsResolved);
        public GameState State { get; private set; } = GameState.Playing;

        public event Action LivesChanged;
        public event Action ProgressChanged;
        public event Action GameOver;
        public event Action Victory;

        public GameManager(Player player, List<Hazard> hazards)
        {
            Player = player;
            Hazards = hazards;

            QuizManager = new QuizManager();
            QuizManager.QuizOpened += _ => State = GameState.QuizOpen;
            QuizManager.QuizAnswered += OnQuizAnswered;
        }

        //Deve ser chamado a cada tick do game loop (veja MainForm.GameLoop).
        //up/down/left/right = teclas pressionadas neste frame.
        //roomBounds = área jogável (normalmente o ClientSize do Form).
        public void Update(float deltaTime, bool up, bool down, bool left, bool right, Rectangle roomBounds, Func<PointF, bool> isWalkable = null)
        {
            foreach (Hazard hazard in Hazards)
                hazard.Tick(deltaTime);

            switch (State)
            {
                case GameState.Playing:
                    Player.Update(deltaTime, up, down, left, right, roomBounds, isWalkable);
                    CheckHazardTriggers();
                    break;

                case GameState.ShockAnimation:
                    // Ignora o input do jogador, mas deixa o Player.Update rodar sem direção
                    // para que o cronômetro interno do "choque" continue contando.
                    Player.Update(deltaTime, false, false, false, false, roomBounds, isWalkable);
                    if (!Player.IsShocked)
                        State = GameState.Playing;
                    break;

                case GameState.QuizOpen:
                case GameState.GameOver:
                case GameState.Victory:
                default:
                    break; // jogo totalmente pausado
            }
        }

        private void CheckHazardTriggers()
        {
            foreach (Hazard hazard in Hazards)
            {
                if (!hazard.CanTrigger) continue;

                if (Player.CollisionBounds.IntersectsWith(hazard.TriggerZone))
                {
                    QuizManager.Open(hazard);
                    break; // só um quiz por vez
                }
            }
        }

        private void OnQuizAnswered(Hazard hazard, bool wasCorrect)
        {
            if (wasCorrect)
            {
                ProgressChanged?.Invoke();

                if (ResolvedCount >= TotalHazards)
                {
                    State = GameState.Victory;
                    Victory?.Invoke();
                    return;
                }

                State = GameState.Playing;
            }
            else
            {
                Player.ApplyShock();
                Lives--;
                LivesChanged?.Invoke();

                if (Lives <= 0)
                {
                    State = GameState.GameOver;
                    GameOver?.Invoke();
                    return;
                }

                // Volta a "Playing" automaticamente assim que a animação de choque terminar
                // (ver o case ShockAnimation dentro de Update).
                State = GameState.ShockAnimation;
            }
        }

        //Troca para os perigos de outro cômodo (próxima fase) e reinicia vidas/posição.
        public void LoadRoom(List<Hazard> hazards, PointF playerStart)
        {
            Hazards = hazards;
            Restart(playerStart);
        }

        //Reinicia a partida.
        public void Restart(PointF playerStart)
        {
            Lives = MaxLives;
            State = GameState.Playing;
            Player.ResetPosition(playerStart);

            foreach (Hazard hazard in Hazards)
                hazard.Reset();
        }
    }
}
