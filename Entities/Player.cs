using System;
using System.Drawing;

namespace SegurancaEletricaEmCasa.Entities
{
    //Representa o boneco controlável pelo jogador.
    //Guarda posição, direção, estado de "choque" e a lógica de movimentação em 4 direções.
    //Não depende de nenhuma engine: usa apenas System.Drawing (Point/Rectangle) para posição
    //e colisão, o que funciona perfeitamente dentro de um Form/Panel do Windows Forms.
    public class Player
    {
        public const int Width = 73;
        public const int Height = 172;

        private const float Speed = 160f;          // pixels por segundo
        private const float ShockDuration = 0.8f;   // segundos que o choque imobiliza o jogador

        private float _shockTimer;

        public PointF Position { get; private set; }
        public Direction FacingDirection { get; private set; } = Direction.Down;
        public bool IsMoving { get; private set; }
        public bool IsShocked { get; private set; }

        public Player(PointF startPosition)
        {
            Position = startPosition;
        }

        //Retângulo usado para desenhar o sprite do jogador.
        public Rectangle Bounds =>
            new Rectangle((int)Position.X, (int)Position.Y, Width, Height);

        //Retângulo menor, na altura dos "pés", usado para colisão/gatilho dos perigos.
        //Isso evita que o jogador ative um Hazard só porque a cabeça do sprite encostou nele.
        public Rectangle CollisionBounds =>
            new Rectangle((int)Position.X + 6, (int)Position.Y + Height - 16, Width - 12, 14);

        //Ponto usado para testar se o jogador pode pisar em determinado lugar: fica perto dos
        //"pés" do sprite (não no centro), já que num jogo com perspectiva o chão visível embaixo
        //do personagem é o que importa, não a posição do sprite inteiro (que inclui a cabeça).
        private PointF FeetPoint(float x, float y) => new PointF(x + Width / 2f, y + Height - 8f);

        //Atualiza a posição do jogador a cada frame.
        //up/down/left/right vêm do teclado (WASD ou setas) capturado no MainForm.
        //roomBounds impede que o jogador saia da área do Form/Panel (limite de segurança).
        //isWalkable diz, pixel a pixel, se os "pés" do jogador podem pisar ali (usado para não
        //deixar o personagem sair do chão do quarto, que tem formato de losango/hexágono).
        public void Update(float deltaTime, bool up, bool down, bool left, bool right, Rectangle roomBounds, Func<PointF, bool> isWalkable)
        {
            if (IsShocked)
            {
                _shockTimer -= deltaTime;
                if (_shockTimer <= 0f)
                    IsShocked = false;

                IsMoving = false;
                return; // enquanto está levando choque, o jogador não se move
            }

            float dx = 0, dy = 0;
            if (up) dy -= 1;
            if (down) dy += 1;
            if (left) dx -= 1;
            if (right) dx += 1;

            IsMoving = dx != 0 || dy != 0;
            if (!IsMoving) return;

            // Normaliza para que andar na diagonal não seja mais rápido que andar reto
            float length = (float)Math.Sqrt(dx * dx + dy * dy);
            dx /= length;
            dy /= length;

            if (Math.Abs(dx) > Math.Abs(dy))
                FacingDirection = dx > 0 ? Direction.Right : Direction.Left;
            else if (dy != 0)
                FacingDirection = dy > 0 ? Direction.Down : Direction.Up;

            float currentX = Position.X;
            float currentY = Position.Y;

            float candidateX = currentX + dx * Speed * deltaTime;
            float candidateY = currentY + dy * Speed * deltaTime;

            candidateX = Math.Max(roomBounds.Left, Math.Min(candidateX, roomBounds.Right - Width));
            candidateY = Math.Max(roomBounds.Top, Math.Min(candidateY, roomBounds.Bottom - Height));

            // Testa cada eixo separadamente ("sliding collision"): assim, se o jogador está
            // encostando numa borda do chão na diagonal, ele continua deslizando no eixo livre
            // em vez de travar completamente.
            bool canMoveX = isWalkable == null || dx == 0 || isWalkable(FeetPoint(candidateX, currentY));
            bool canMoveY = isWalkable == null || dy == 0 || isWalkable(FeetPoint(currentX, candidateY));

            float newX = canMoveX ? candidateX : currentX;
            float newY = canMoveY ? candidateY : currentY;

            Position = new PointF(newX, newY);
        }

        //Chamado pelo GameManager quando o jogador erra um quiz.
        public void ApplyShock()
        {
            IsShocked = true;
            _shockTimer = ShockDuration;
        }

        //Usado ao reiniciar a partida
        public void ResetPosition(PointF start)
        {
            Position = start;
            IsShocked = false;
            _shockTimer = 0f;
            IsMoving = false;
            FacingDirection = Direction.Down;
        }
    }
}
