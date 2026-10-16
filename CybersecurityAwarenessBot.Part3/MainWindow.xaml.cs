using CybersecurityAwarenessBot.Bot;
using CybersecurityAwarenessBot.Database;
using CybersecurityAwarenessBot.Features;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using MySqlConnector;

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
        /// This saves and loads the user's tasks in MySQL.
        /// </summary>
        private readonly TaskRepository _tasks;

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
            _tasks = new TaskRepository(_log);
            _quiz = new QuizManager(_log);
            _bot = new ChatBot(_log, _tasks, _quiz);
            AsciiHeader.Text = _logo.GetLogo();
            // Refresh the activity log tab whenever anything is logged
            _log.EntryAdded += RefreshLogTab;
            RefreshLogTab();
            // Reload the Tasks tab whenever a task is added or changed, whether from the tab or from the chat
            _tasks.TasksChanged += RefreshTasksTab;
            // Open the Quiz tab when the user asks for the quiz in the chat, e.g. "quiz me"
            _bot.QuizRequested += OpenQuizTab;

            ReminderPicker.DisplayDateStart = DateTime.Today;
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
            Task tasksLoading = LoadTasksAsync();
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

            await tasksLoading;
        }



        /// <summary>
        /// Deals with the send button being clicked
        /// </summary>
        /// <param name="sender">The button raising the event.</param>
        /// <param name="e">Event data</param>
        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            await SendMessageAsync();
        }

        /// <summary>
        ///  The user can click the enter button to send a input
        /// </summary>
        /// <param name="sender">Text box raising the event</param>
        /// <param name="e">Event data used to check which key was pressed</param>
        private async void InputBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                await SendMessageAsync();
            }
        }

        /// <summary>
        /// Function that checks if user has enter something - if blank/white space then it returns. Shows the user and bots responses.
        /// It is async because a task or reminder typed in the chat is saved to the database before the chatbot replies.
        /// </summary>
        private async Task SendMessageAsync()
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
                    + "You can also say 'tell me more' for another tip on the same topic, add a task or reminder (e.g. 'Remind me to update my password tomorrow'), " 
                    + "say 'show my tasks', 'start the quiz' or 'show activity log'.", false);
                return;
            }

            string label = string.IsNullOrWhiteSpace(_bot.User.Name) ? "You" : _bot.User.Name;
            AddMessage(label, userInput, true);

            // Cleared before waiting for the reply, so the user's message doesn't sit in the box while a task is saved
            InputBox.Clear();
            InputBox.Focus();

            string reply = await _bot.GetReplyAsync(userInput);
            AddMessage("Bot", reply, false);
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
        /// Runs when the Start quiz or Play again button is clicked.
        /// </summary>
        /// <param name="sender">The button raising the event.</param>
        /// <param name="e">Event data</param>
        private void StartQuizButton_Click(object sender, RoutedEventArgs e)
        {
            StartQuiz();
        }

        /// <summary>
        /// Runs when the user asks for the quiz in the chat. Switches to the Quiz tab, and starts a new game unless one is already in progress.
        /// </summary>
        private void OpenQuizTab()
        {
            MainTabs.SelectedItem = QuizTab;

            if (!_quiz.IsRunning)
            {
                StartQuiz();
            }
        }

        /// <summary>
        /// Starts a new game and shows the first question. Used by the Start quiz and Play again buttons, and by the chat.
        /// </summary>
        private void StartQuiz()
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

        /// <summary>
        /// Reads every task from the database and shows them in the Tasks tab, with a count of pending and completed tasks.
        /// If the database can't be reached, the tab explains what to do instead of the program crashing.
        /// </summary>
        private async Task LoadTasksAsync()
        {
            try
            {
                List<TaskItem> tasks = await _tasks.GetAllAsync();
                TaskList.ItemsSource = tasks;

                int pending = tasks.Count(task => !task.IsCompleted);
                TasksSummary.Text = tasks.Count == 0
                    ? "Keep track of cybersecurity actions you want to take, with an optional reminder."
                    : $"{pending} pending, {tasks.Count - pending} completed.";

                TasksEmpty.Text = "No tasks yet. Add one above, for example 'Enable two-factor authentication'.";
                TasksEmpty.Visibility = tasks.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            catch (MySqlException ex)
            {
                TaskList.ItemsSource = null;
                TasksSummary.Text = "Your tasks couldn't be loaded.";
                TasksEmpty.Text = DatabaseConnection.DescribeError(ex);
                TasksEmpty.Visibility = Visibility.Visible;
            }
        }

        /// <summary>
        /// Runs when the TasksChanged event is raised, so a task added in the chat appears in the Tasks tab straight away.
        /// async void is only used here because it is an event handler.
        /// </summary>
        private async void RefreshTasksTab()
        {
            await LoadTasksAsync();
        }

        /// <summary>
        ///  Saves a new task from the form, then clears the form. The list reloads itself through the TasksChanged event.
        /// </summary>
        /// <param name="sender">The button raising the event.</param>
        /// <param name="e">Event data</param>
        private async void AddTaskButton_Click(object sender, RoutedEventArgs e)
        {
            // Disabled while saving, so a double-click can't add the task twice
            AddTaskButton.IsEnabled = false;

            try
            {
                TaskItem task = await _tasks.AddAsync(TaskTitleBox.Text, TaskDescriptionBox.Text, ReminderPicker.SelectedDate);

                TaskTitleBox.Clear();
                TaskDescriptionBox.Clear();
                ReminderPicker.SelectedDate = null;

                string reminder = task.ReminderDate == null ? "" : $" I'll remind you on {task.ReminderDate:d MMM yyyy}.";
                ShowTaskMessage($"Task added: '{task.Title}'.{reminder}", isError: false);    
            }
            catch (ArgumentException ex)
            {
                // Validation problems
                ShowTaskMessage(ex.Message, isError: true);
            }
            catch (MySqlException ex)
            {
                ShowTaskMessage(DatabaseConnection.DescribeError(ex), isError: true);
            }
            finally
            {
                AddTaskButton.IsEnabled = true;
            }
        }

        /// <summary>
        /// Marks the clicked task as completed.
        /// </summary>
        /// <param name="sender">The task's Complete button, whose Tag holds the TaskItem.</param>
        /// <param name="e">Event data</param>
        private async void CompleteTask_Click(object sender, RoutedEventArgs e)
        {
            // The button's Tag holds the task for its row. "is not" checks it really is a TaskItem before using it.
            if (((Button)sender).Tag is not TaskItem task)
            {
                return;
            }
            await RunTaskActionAsync(() => _tasks.MarkCompletedAsync(task), $"Well done! '{task.Title}' is marked as completed.");
        }

        /// <summary>
        /// Deletes the clicked task, after asking the user to confirm.
        /// </summary>
        /// <param name="sender">The task's Delete button, whose Tag holds the TaskItem.</param>
        /// <param name="e">Event data</param>
        private async void DeleteTask_Click(object sender, RoutedEventArgs e)
        {
            // The button's Tag holds the task for its row. "is not" checks it really is a TaskItem before using it.
            if (((Button)sender).Tag is not TaskItem task)
            {
                return;
            }

            MessageBoxResult answer = MessageBox.Show($"Delete the task '{task.Title}'?", "Delete task", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (answer != MessageBoxResult.Yes)
            {
                return;
            }

            await RunTaskActionAsync(() => _tasks.DeleteAsync(task), $"Deleted '{task.Title}'.");
        }

        /// <summary>
        /// Runs a database action on one task, shows the result, and reloads the list. Used by both Complete and Delete so the error handling is written once.
        /// </summary>
        /// <param name="action">The action to run. It returns true if the task was found and changed.</param>
        /// <param name="successMessage">The message to show if it worked</param>
        private async Task RunTaskActionAsync(Func<Task<bool>> action, string successMessage)
        {
            try
            {
                bool changed = await action();
                ShowTaskMessage(changed ? successMessage : "That task no longer exists, so the list has been refreshed.", isError: !changed);
                // A successful change already reloads the list through the TasksChanged event, so only reload here if nothing changed
                if (!changed)
                {
                    await LoadTasksAsync();
                }

            }
            catch (MySqlException ex)
            {
                ShowTaskMessage(DatabaseConnection.DescribeError(ex), isError: true);
            }
        }

        /// <summary>
        /// Shows a message beside the Add task button: green for success, red for a problem.
        /// </summary>
        /// <param name="message">The text to show</param>
        /// <param name="isError">True for a problem, false for success</param>
        private void ShowTaskMessage(string message, bool isError)
        {
            TaskMessage.Text = message;
            TaskMessage.Foreground = (Brush)FindResource(isError ? "AccentRed" : "AccentGreen");
            TaskMessage.Visibility = Visibility.Visible;
        }
    }
}