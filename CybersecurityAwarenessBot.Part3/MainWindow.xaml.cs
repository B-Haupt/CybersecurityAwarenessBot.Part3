using CybersecurityAwarenessBot.Bot;
using CybersecurityAwarenessBot.Database;
using CybersecurityAwarenessBot.Features;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace CybersecurityAwarenessBot.Part3
{
    /// <summary>
    /// This class is responsible for the user interface. Meaning it deals with the displaying of messages, reading what the user types and passing it to the bot. All the chatbot logic
    /// stays in the Bot classes.
    /// </summary>
    public partial class MainWindow : Window
    {

        private readonly GreetingPlayer _greeting = new();
        private readonly LogoArt _logo = new();
        private readonly InputValidator _validator = new();
        private readonly ChatBot _bot;

        /// <summary>
        /// This runs the quiz. This shares the activity log, so the quiz starts and results appear in the log.
        /// </summary>
        private readonly QuizManager _quiz;

        /// <summary>
        /// The activity log shared by every feature, so all actions appear in one place
        /// </summary>
        private readonly ActivityLog _log = new();


        /// <summary>
        /// True until the user has given their name. Set it so that while it is true, whatever the user types in is stored as their name instead of treated like a question.
        /// </summary>
        private bool _awaitingName = true;

        /// <summary>
        /// How many log entries the Activity Log tab is showing.
        /// </summary>
        private int _logTabShown = ActivityLog.PageSize;

        /// <summary>
        /// Sets up the window and its controls, and display the ASCII art logo
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            _bot = new ChatBot(_log);
            _quiz = new QuizManager(_log);
            AsciiHeader.Text = _logo.GetLogo();
            // Refresh the activity log tab whenever anything is logged
            _log.EntryAdded += RefreshLogTab;
            RefreshLogTab();
        }

        /// <summary>
        /// Function is to add a rounded message bubble to the chat panel and scrolls to the bottom. User message is aligned to the right in accent colour and the chatbot is on the left.
        /// </summary>
        /// <param name="sender">Who is speaking, used as a label</param>
        /// <param name="message">The text to display.</param>
        /// <param name="isUser">True for user's messages and false for chatbot</param>
        private void AddMessage(string sender, string message, bool isUser)
        {
            var senderLabel = new TextBlock
            {
                Text = sender,
                FontWeight = FontWeights.Bold,
                FontSize = 11,
                Margin = new Thickness(0, 0, 0, 4),
                Foreground = isUser ? (Brush)FindResource("BgDark") : (Brush)FindResource("AccentGreen")
            };

            var messageText = new TextBlock
            {
                Text = message,
                TextWrapping = TextWrapping.Wrap,
                FontSize = 14,
                Foreground = isUser ? (Brush)FindResource("BgDark") : (Brush)FindResource("TextLight")
            };

            var content = new StackPanel();
            content.Children.Add(senderLabel);
            content.Children.Add(messageText);

            var bubble = new Border
            {
                Child = content,
                Background = isUser ? (Brush)FindResource("AccentGreen") : (Brush)FindResource("BgDark"),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(12, 8, 12, 8),
                Margin = new Thickness(0, 0, 0, 10),
                MaxWidth = 560,
                HorizontalAlignment = isUser ? HorizontalAlignment.Right
                                             : HorizontalAlignment.Left
            };
            ChatPanel.Children.Add(bubble);
            ChatScroll.ScrollToEnd();
        }

        /// <summary>
        /// Runs once the window has been drawn. Plays the voice greeting on a background thread so the window stays responsive, then asks the user for their name.
        /// Input is disabled until the greeting finishes so the text greeting still follows the voice greeting.
        /// </summary>
        /// <param name="sender">The window raising the event.</param>
        /// <param name="e">Event data</param>
        private async void Window_ContentRendered(object? sender, EventArgs e)
        {
            InputBox.IsEnabled = false;
            SendButton.IsEnabled = false;

            // Task.Run moves the greeting off the UI thread, and await waits for it to finish without freezing the window
            string? error = await Task.Run(() => _greeting.PlayGreeting());
            if (error != null)
            {
                AddMessage("Bot", error, false);
            }

            InputBox.IsEnabled = true;
            SendButton.IsEnabled = true;

            AddMessage("Bot", "Hello! Welcome to the Cybersecurity Awareness Bot. What is your name?", false);
            InputBox.Focus();

            // Check the task database in the background and report the result on the Tasks tab
            string? databaseError = await DatabaseConnection.TestConnectionAsync();
            TasksStatus.Text = databaseError ?? "Connected to the task database. Your tasks will appear here.";
        }



        /// <summary>
        /// Deals with the send button being clicked
        /// </summary>
        /// <param name="sender">The button raising the event.</param>
        /// <param name="e">Event data</param>
        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            SendMessage();
        }

        /// <summary>
        ///  The user can click the enter button to send a input
        /// </summary>
        /// <param name="sender">Text box raising the event</param>
        /// <param name="e">Event data used to check which key was pressed</param>
        private void InputBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                SendMessage();
            }
        }

        /// <summary>
        /// Function that checks if user has enter something - if blank/white space then it returns. Shows the user and bots responses.
        /// </summary>
        private void SendMessage()
        {
            string userInput = InputBox.Text;

            // Checks if input is blank and gives a reply that the user hasn't typed anything in
            if (!_validator.IsValidInput(userInput))
            {
                string prompt = _awaitingName
                    ? "You haven't typed anything yet. Please tell me your name so I can personalise our chat."
                    : "You haven't typed anything yet. Ask me about passwords, phishing or safe browsing, or say 'tell me more' to continue the last topic.";

                AddMessage("Bot", prompt, false);
                InputBox.Clear();
                InputBox.Focus();
                return;
            }

            if (_awaitingName)
            {
                string name = _validator.CleanName(userInput);

                AddMessage("You", userInput, true);
                InputBox.Clear();
                InputBox.Focus();

                // Ask again if nothing usable was left, e.g. the user typed only "my name is"
                if (name.Length == 0)
                {
                    AddMessage("Bot", "Sorry, I didn't catch your name. What should I call you?", false);
                    return;
                }

                _bot.User.Name = name;
                _awaitingName = false;

                AddMessage("Bot", $"Welcome, {name}! I'm a Cybersecurity Awareness Bot. You can ask me about {_bot.TopicList}. "
                    + "You can also say 'tell me more' for another tip on the same topic.", false);
                return;
            }

            string label = string.IsNullOrWhiteSpace(_bot.User.Name) ? "You" : _bot.User.Name;
            AddMessage(label, userInput, true);

            AddMessage("Bot", _bot.GetReply(userInput), false);

            InputBox.Clear();
            InputBox.Focus();
        }

        /// <summary>
        /// Redraws the Activity Log tab with the most recent entries, newest first, and only shows "Show more" when there are older entries to show.
        /// </summary>
        private void RefreshLogTab()
        {
            LogList.ItemsSource = _log.GetPage(0, _logTabShown);

            int showing = Math.Min(_logTabShown, _log.Count);
            LogSummary.Text = _log.Count == 0
                ? "The chatbot's actions will be recorded here."
                : $"Showing the {showing} most recent of {_log.Count} actions, newest first.";

            LogEmpty.Visibility = _log.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            ShowMoreButton.Visibility = _log.Count > _logTabShown ? Visibility.Visible : Visibility.Collapsed;
        }

        /// <summary>
        /// Shows the next page of older entries in the Activity Log tab.
        /// </summary>
        /// <param name="sender">The button raising the event.</param>
        /// <param name="e">Event data</param>
        private void ShowMoreButton_Click(object sender, RoutedEventArgs e)
        {
            _logTabShown += ActivityLog.PageSize;
            RefreshLogTab();
        }

        /// <summary>
        /// Starts a new quiz and shows the first question. Used by both the Start quiz and Play again buttons.
        /// </summary>
        /// <param name="sender">The button raising the event.</param>
        /// <param name="e">Event data</param>
        private void StartQuizButton_Click(object sender, RoutedEventArgs e)
        {
            _quiz.Start();

            QuizStartPanel.Visibility = Visibility.Collapsed;
            QuizResultPanel.Visibility = Visibility.Collapsed;
            QuizQuestionPanel.Visibility = Visibility.Visible;

            ShowQuestion();
        }

        /// <summary>
        /// Displays the current question and creates one button per answer option. Two buttons for true/false, four for multiple choice.
        /// </summary>
        private void ShowQuestion()
        {
            QuizQuestion question = _quiz.CurrentQuestion;

            string type = question.IsTrueFalse ? "True or false" : "Multiple choice";
            QuizProgress.Text = $"Question {_quiz.QuestionNumber} of {_quiz.TotalQuestions}  |  {type}";
            QuizScore.Text = $"Score: {_quiz.Score}";
            QuestionText.Text = question.Prompt;

            // Remove the previous question's buttons, then add one for each option
            OptionsPanel.Children.Clear();
            for (int i = 0; i < question.Options.Count; i++)
            {
                var button = new Button
                {
                    Content = new TextBlock { Text = question.GetOptionLabel(i), TextWrapping = TextWrapping.Wrap },
                    Tag = i,
                    Style = (Style)FindResource("QuizOptionStyle")
                };
                button.Click += OptionButton_Click;
                OptionsPanel.Children.Add(button);
            }

            FeedbackBorder.Visibility = Visibility.Collapsed;
            NextButton.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// Checks the chosen answer, colours the correct option green and a wrong choice red, and shows the explanation.
        /// </summary>
        /// <param name="sender">The answer button that was clicked.</param>
        /// <param name="e">Event data</param>
        private void OptionButton_Click(object sender, RoutedEventArgs e)
        {
            var clicked = (Button)sender;
            int choice = (int)clicked.Tag;

            var (isCorrect, feedback) = _quiz.SubmitAnswer(choice);
            int correct = _quiz.CurrentQuestion.CorrectIndex;

            // Lock the answers and show which was right
            foreach (Button button in OptionsPanel.Children)
            {
                button.IsEnabled = false;
                int index = (int)button.Tag;

                if (index == correct)
                {
                    button.Background = (Brush)FindResource("AccentGreen");
                    button.Foreground = (Brush)FindResource("BgDark");
                }
                else if (index == choice)
                {
                    button.Background = (Brush)FindResource("AccentRed");
                    button.Foreground = (Brush)FindResource("BgDark");
                }
            }

            FeedbackText.Text = feedback;
            FeedbackBorder.Background = (Brush)FindResource(isCorrect ? "AccentGreen" : "AccentRed");
            FeedbackBorder.Visibility = Visibility.Visible;

            QuizScore.Text = $"Score: {_quiz.Score}";

            // On the last question the button leads to the results instead
            NextButton.Content = _quiz.QuestionNumber == _quiz.TotalQuestions ? "See my results" : "Next question";
            NextButton.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// Moves to the next question, or shows the final score and feedback when the quiz is finished.
        /// </summary>
        /// <param name="sender">The button raising the event.</param>
        /// <param name="e">Event data</param>
        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            if (_quiz.NextQuestion())
            {
                ShowQuestion();
                return;
            }

            QuizQuestionPanel.Visibility = Visibility.Collapsed;
            QuizResultPanel.Visibility = Visibility.Visible;

            FinalScoreText.Text = $"{_quiz.Score} / {_quiz.TotalQuestions}";
            FinalFeedbackText.Text = _quiz.GetFinalFeedback();
        }
    }
}