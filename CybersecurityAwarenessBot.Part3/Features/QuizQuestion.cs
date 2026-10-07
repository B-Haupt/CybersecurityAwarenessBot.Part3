namespace CybersecurityAwarenessBot.Features
{
    /// <summary>
    ///  Class for one quiz question. Set out the question text, the answer options, the correct option and then the explanation.
    ///  Set up so that multiple-choice questions have four options and True/False questions have two options only
    /// </summary>
    internal class QuizQuestion
    {
        /// <summary>
        /// For the question + true/false question
        /// </summary>
        public string Prompt { get; }

        /// <summary>
        /// Options answers to the question
        /// </summary>
        public List<string> Options { get; }


        /// <summary>
        /// This is the correct option in Options, which starts at 0
        /// </summary>
        public int CorrectIndex { get; }

        /// <summary>
        /// A short explanation that reinforces the cybersecurity concept, that is shown after every answer
        /// </summary>
        public string Explanation { get; }

        /// <summary>
        /// True for true/false questions, which  only have two options
        /// </summary>
        public bool IsTrueFalse => Options.Count == 2;

        /// <summary>
        /// This creates a multiple-choice question.
        /// </summary>
        /// <param name="prompt">The Question</param>
        /// <param name="options">The Answer Options</param>
        /// <param name="correctIndex">The position of the correct option, starting at 0</param>
        /// <param name="explanation">Why the correct answer is right</param>
        public QuizQuestion(string prompt, List<string> options, int correctIndex, string explanation) 
        {
            Prompt = prompt;
            Options = options;
            CorrectIndex = correctIndex;
            Explanation = explanation;
        }

        /// <summary>
        /// This creates a true/false question, so each one doesn't need its own list of options.
        /// </summary>
        /// <param name="statement">The statement the user judges</param>
        /// <param name="isTrue">Whether the statement is true or not</param>
        /// <param name="explanation">Why the statement is true or false</param>
        /// <returns>A question with the option of true or false</returns>
        public static QuizQuestion TrueFalse(string statement, bool isTrue, string explanation) 
        {

            return new QuizQuestion(statement, new List<string> {"True", "False"}, isTrue ? 0 : 1, explanation);
        }

        /// <summary>
        /// The text of an option as it is shown, with a letter.
        /// </summary>
        /// <param name="index">The position of the option</param>
        /// <returns>The option with its letter or just true and false</returns>
        public string GetOptionLabel(int index) {
            if (IsTrueFalse) {
                return Options[index];
            }

            // Plus the index changes the letter
            char letter = (char)('A' + index);
            return $"{letter}) {Options[index]}";
        }
    }
}
