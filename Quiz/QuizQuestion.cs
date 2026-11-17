namespace SegurancaEletricaEmCasa.Quiz
{
    public class QuizQuestion
    {
        public string Prompt { get; }
        public string[] Options { get; }
        public int CorrectOptionIndex { get; }

        // Mensagem mostrada quando o jogador acerta.
        public string CorrectFeedback { get; }

        //Mensagem mostrada quando o jogador erra.
        public string WrongFeedback { get; }

        public QuizQuestion(
            string prompt,
            string[] options,
            int correctOptionIndex,
            string correctFeedback = null,
            string wrongFeedback = null)
        {
            Prompt = prompt;
            Options = options;
            CorrectOptionIndex = correctOptionIndex;
            CorrectFeedback = correctFeedback;
            WrongFeedback = wrongFeedback;
        }
    }
}
