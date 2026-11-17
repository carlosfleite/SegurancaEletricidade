using System;
using SegurancaEletricaEmCasa.Entities;

namespace SegurancaEletricaEmCasa.Quiz
{
    public class QuizManager
    {
        public Hazard ActiveHazard { get; private set; }
        public bool IsOpen => ActiveHazard != null;

        //Disparado quando um Hazard é ativado e o quiz deve ser exibido na tela.
        public event Action<Hazard> QuizOpened;

        //Disparado depois que o jogador responde. bool = resposta estava correta.
        public event Action<Hazard, bool> QuizAnswered;

        public void Open(Hazard hazard)
        {
            if (IsOpen) return;
            ActiveHazard = hazard;
            QuizOpened?.Invoke(hazard);
        }

        public void SubmitAnswer(int selectedOptionIndex)
        {
            if (!IsOpen) return;

            Hazard hazard = ActiveHazard;
            bool isCorrect = selectedOptionIndex == hazard.Question.CorrectOptionIndex;

            if (isCorrect)
                hazard.MarkResolved();
            else
                hazard.MarkWrongAnswer();

            ActiveHazard = null;
            QuizAnswered?.Invoke(hazard, isCorrect);
        }

        public void Close()
        {
            ActiveHazard = null;
        }
    }
}
