namespace CybersecurityAwarenessBot.Features
{
    /// <summary>
    /// This runs the cybersecurity quiz. It holds the questions, shuffles their order for each game, checks answers, keep the score and gives feedback.
    /// </summary>
    internal class QuizManager
    {
        /// <summary>
        /// Storing all the questions. They are a mixture of multiple choice and true/false
        /// </summary>
        private readonly List<QuizQuestion> _questions = new()
        {
            new QuizQuestion("....", new List<string> { "....", ".....", ".....","......" }, 2, "1234")

        };

        /// <summary>
        /// Feedback for the various choices so that the quiz doesn't repeat the same line after every answer
        /// </summary>
        private readonly string[] _correctOpenings = { "Correct!", "Well done, that's right!", "Spot on!", "Exactly right!" };
        private readonly string[] _incorrectOpenings = { "Not quite.", "That's not it this time.", "Not this time.", "Good try, but no." };

        private readonly ActivityLog _log;
        private readonly Random _random = new();

        /// <summary>
        /// The questions are shuffled at the start of every game and then the question are stored in the order they are asked.
        /// </summary>
        private List<QuizQuestion> _order = new();

        /// <summary>
        /// Stores the position of the current question.
        /// </summary>
        private int _index;
        
        /// <summary>
        /// This is true once the current question has been answered. 
        /// </summary>
        private bool _answered;

        /// <summary>
        /// Keep number of correct answers so far in the game.
        /// </summary>
        public int Score { get; private set; }

        /// <summary>
        /// This is true while the game is in progress.
        /// </summary>
        public bool IsRunning { get; private set; }

        /// <summary>
        /// Amount of questions in the game.
        /// </summary>
        public int TotalQuestions => _questions.Count;

        /// <summary>
        /// The current question's number for display, starting at 1.
        /// </summary>
        public int QuestionNumber => _index + 1;

        /// <summary>
        /// Stores the question being asked.
        /// </summary>
        public QuizQuestion CurrentQuestion => _order[_index];

        /// <summary>
        /// This creates the quiz with the activity logs recording the starts and its results in.
        /// </summary>
        /// <param name="log">Shared activity log</param>
        public QuizManager(ActivityLog log) 
        {
            _log = log;
        }

        /// <summary>
        /// This starts a new game and also shuffles the questions and resets the score.
        /// </summary>
        public void Start()
        {
            // This restarting part-way through is recorded, so the log shows the earlier attempt
            if (IsRunning && _index > 0) 
            {
                _log.Add($"Quiz restarted after {_index} question(s) answered");
            }

            // This give every question a random number and sort by it, which puts them in a random order
            _order = _questions.OrderBy(question => _random.Next()).ToList();
            _index = 0;
            _answered = false;
            Score = 0;
            IsRunning = true;

            _log.Add($"Quiz started ({TotalQuestions} questions)");
        }

        /// <summary>
        /// This method checks the user answer to the current question and then returns the immediate feedback plus the explanation.
        /// </summary>
        /// <param name="choice">Position of the option the user picked</param>
        /// <returns>This return the answer was correct, and the feedback to show.</returns>
        public (bool IsCorrect, string Feedback) SubmitAnswer(int choice) {
            QuizQuestion question = CurrentQuestion;

            // Ignore a second click on the same question
            if (_answered)
            {
                return (choice == question.CorrectIndex, "You've already answered this question. Click Next to carry on.");
            }

            _answered = true;
            bool isCorrect = choice == question.CorrectIndex;

            if (isCorrect)
            {
                Score++;
                return (true, $"{Pick(_correctOpenings)} {question.Explanation}");
            }

            return (false, $"{Pick(_incorrectOpenings)} The correct answer is {question.GetOptionLabel(question.CorrectIndex)}. {question.Explanation}");

        }


        /// <summary>
        /// This method moves on to the next question. When there are no more, the game ends and the result is logged.
        ///</summary>
        /// <returns>True if there is another question, false if the quiz is finished</returns>
        public bool NextQuestion()
        {
            _index++;
            _answered = false;

            if (_index < _order.Count)
            {
                return true;
            }

            IsRunning = false;
            _log.Add($"Quiz completed: scored {Score}/{TotalQuestions}");
            return false;
        }

        /// <summary>
        /// This is the final feedback based on the percentage of correct answers.
        /// </summary>
        /// <returns>A message to show with the final score</returns>
        public string GetFinalFeedback()
        {
            int percentage = Score * 100 / TotalQuestions;

            if (percentage >= 90)
            {
                return "Outstanding! You're a cybersecurity pro!";
            }

            if (percentage >= 70)
            {
                return "Great job! You have strong cybersecurity awareness.";
            }

            if (percentage >= 50)
            {
                return "Good effort! Read through the explanations to strengthen your knowledge.";
            }

            return "Keep learning to stay safe online! Try the quiz again to build your confidence.";
        }

        /// <summary>
        /// Picks a random item from a list of options.
        /// </summary>
        /// <param name="options">The options to choose from</param>
        /// <returns>One of the options</returns>
        private string Pick(string[] options)
        {
            return options[_random.Next(options.Length)];
        }

    }
}
