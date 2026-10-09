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
            new QuizQuestion("What is the meaning of two-factor authentication (2FA)?",
                new List<string> { "Changing your password twice a year", "Logging in from two different devices", "Using three different passwords","A second check, such as a code on your phone, as well as your password" },3
                ,"Two-factor authentication is adding a second step to logging in. This ensures that even if someone steals your password they still would not be able to access your account."),

            QuizQuestion.TrueFalse("A padlock icon and \"https\" in the address bar mean your connection to the website is encrypted.", true,
                "HTTPS encrypts the data travelling between you and the site. It does not prove the site is honest, though, because scammers can use HTTPS too."),

            new QuizQuestion("You get a email asking for your password. What do you do?",
                new List<string> { "Ignore it", "Delete the email", "Report the email as a phishing attempt","Send them your password" },2
                ,"By reporting the email as phishing this helps your email provider/IT team to block the scam and protect other users. A real company would never ask for a users password."),

            QuizQuestion.TrueFalse("Is it safe to use the same long strong password for all your accounts, if the password is long enough?", false,
                "The issue is that if one website is breached, then attackers will try to leak passwords on other sites. To prevent this a user should have a unique password for every account."),
            
            new QuizQuestion("Which of these is the strongest password?",
                new List<string> { "Password123", "brittany2005", "Sunny-Kettle-Orbit-42!", "qwerty" }, 2,
                "A long passphrase of random words mixed with numbers and symbols is far harder to guess or crack than a short or personal password."),

            QuizQuestion.TrueFalse("Social engineering attacks breaks into computers directly via hacking.", false,
                "Social engineers use urgency, fear or friendliness to trick people into giving away information or access, which is why pausing to verify is so important."),

            new QuizQuestion("If someone phones you claiming to be from your bank's fraud team and asks for the one-time PIN (OTP) that was just sent to your phone. What should you do in this situation?",
                new List<string> { "Read them the OTP so they can stop the fraud", "Hang up and call your bank on the number on your card",
                "Give them only half of the OTP", "Ask them to email you instead" }, 1,
                "This is social engineering. Banks never ask for your OTP. Hang up and contact the bank yourself using a number you trust."),

            QuizQuestion.TrueFalse("It is safe to do online banking on free public Wi-Fi at a coffee shop.", false,
                "Public networks can be monitored or faked by attackers. Use mobile data or a trusted VPN for anything sensitive."),

            new QuizQuestion("An email contains a link to \"www.yourbank-secure-login.co\". What is the safest thing to do?",
                new List<string> { "Click it quickly before it expires", "Hover over it to see where it really leads, and go to the official website yourself",
                    "Forward it to friends to check", "Reply and ask if the link is real" }, 1,
                "Fake links often look almost right. Checking where a link really leads, and typing the official address yourself, keeps you off fake sites and keeps you safe."),

            new QuizQuestion("Which sign most strongly suggests an email is a phishing attempt?",
                new List<string> { "It comes from a colleague you emailed yesterday", "It creates urgency, such as \"Your account will be closed in 24 hours!\"",
                    "It includes your company's logo in the signature", "It was sent during work hours" }, 1,
                "Phishing relies on pressure. Urgent threats are designed to make you act before you think, so treat them as a warning sign."),

            QuizQuestion.TrueFalse("Security updates are only about new features, so it is fine to postpone them for a few months.", false,
                "Security updates fix weaknesses that attackers actively exploit. Installing them promptly is one of the simplest ways to stay protected."),

            new QuizQuestion("Which of these is the safest to share publicly on social media?",
                new List<string> { "Your ID number", "A photo of your boarding pass", "Your general hobbies and interests",
                    "Your home address and when you will be away on holiday" }, 2,
                "ID numbers, travel documents and your whereabouts can be used for identity theft or burglary. General interests carry far less risk."),

            new QuizQuestion("A torch app asks for access to your contacts and location. What should you do?",
                new List<string> { "Allow it, because every app needs these", "Deny the permissions or choose a different app",
                    "Allow contacts but not location", "Restart your phone" }, 1,
                "A torch app has no reason to need your contacts or location. Only grant permissions an app genuinely needs to work."),

            QuizQuestion.TrueFalse("A password manager can help you create and remember a strong, unique password for every account.", true,
                "Password managers generate strong passwords and store them securely, so you only need to remember one master password."),

            new QuizQuestion("You receive an SMS saying you have won R10,000 and must pay a R250 \"release fee\" to claim it. What is this most likely to be?",
                new List<string> { "A genuine prize", "An advance-fee scam", "A bank error in your favour", "A tax refund" }, 1,
                "Real prizes never require a payment to release them. Asking for a fee upfront is a classic advance-fee scam.")

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
