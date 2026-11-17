using System.Drawing;
using SegurancaEletricaEmCasa.Quiz;

namespace SegurancaEletricaEmCasa.Entities
{
    //Estado visual/lógico de um ponto de perigo.
    public enum HazardState
    {
        //Perigo presente, ainda não resolvido. 
        Active,

        //Jogador acertou o quiz: perigo corrigido (ex.: copo afastado, tomada segura).
        Resolved,

        //Jogador errou ao menos uma vez: mostra aspecto queimado/danificado.
        Damaged
    }

    //Um "ponto de perigo" interativo do quarto (copo d'água perto da tomada, fio desencapado,
    // benjamim sobrecarregado, aparelho coberto por roupas, fio sob o tapete, etc.).
    // Guarda a pergunta associada e o estado atual (ativo / resolvido / danificado).
    public class Hazard
    {
        //Identificador único, útil para debug, salvamento de progresso, etc.
        public string Id { get; }
        public Rectangle Bounds { get; }
        public Rectangle TriggerZone { get; }
        public Rectangle OverlayBounds { get; }
        public QuizQuestion Question { get; }
        public HazardState State { get; private set; } = HazardState.Active;

        //Tempo restante (em segundos) até este perigo poder reabrir
        public float RetryCooldown { get; private set; }

        public Hazard(string id, Rectangle bounds, int triggerPadding, QuizQuestion question, Rectangle? overlayBounds = null)
        {
            Id = id;
            Bounds = bounds;
            TriggerZone = Rectangle.Inflate(bounds, triggerPadding, triggerPadding);
            Question = question;
            OverlayBounds = overlayBounds ?? bounds;
        }

        public bool IsResolved => State == HazardState.Resolved;

        //Só pode abrir o quiz se ainda não foi resolvido e o cooldown já zerou.
        public bool CanTrigger => State != HazardState.Resolved && RetryCooldown <= 0f;

        //Chamado a cada frame pelo GameManager para reduzir o cooldown.
        public void Tick(float deltaTime)
        {
            if (RetryCooldown > 0f)
                RetryCooldown -= deltaTime;
        }
        public void MarkResolved()
        {
            State = HazardState.Resolved;
            RetryCooldown = 0f;
        }
        public void MarkWrongAnswer()
        {
            State = HazardState.Damaged;
            RetryCooldown = 1.5f;
        }
        public void Reset()
        {
            State = HazardState.Active;
            RetryCooldown = 0f;
        }
    }
}
