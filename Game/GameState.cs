namespace SegurancaEletricaEmCasa.Game
{
    /// <summary>Estado geral da partida, controlado pelo GameManager.</summary>
    public enum GameState
    {
        /// <summary>Jogador pode se mover livremente pelo quarto.</summary>
        Playing,

        /// <summary>Quiz aberto na tela: input de movimento é ignorado.</summary>
        QuizOpen,

        /// <summary>Animação/estado de choque em andamento após uma resposta errada.</summary>
        ShockAnimation,

        /// <summary>Vidas chegaram a 0.</summary>
        GameOver,

        /// <summary>Todos os perigos do cômodo atual foram resolvidos.</summary>
        Victory
    }
}
