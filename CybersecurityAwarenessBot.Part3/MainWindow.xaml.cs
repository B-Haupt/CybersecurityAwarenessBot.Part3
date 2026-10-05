using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using CybersecurityAwarenessBot.Bot;

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
        private readonly ChatBot _bot = new();
        private readonly InputValidator _validator = new();
        /// <summary>
        /// True until the user has given their name. Set it so that while it is true, whatever the user types in is stored as their name instead of treated like a question.
        /// </summary>
        private bool _awaitingName = true;

        /// <summary>
        /// Sets up the window and its controls
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
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
        /// Runs when the window opens. Displays the logo, plays the greeting and then asks the user for their name.
        /// </summary>
        /// <param name="sender">The window raising the event.</param>
        /// <param name="e">Event data</param>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Store the art in the header
            AsciiHeader.Text = _logo.GetLogo();

            // Play the voice greeting and report it in the chat if it doesn't play
            string? error = _greeting.PlayGreeting();
            if (error != null)
            {
                AddMessage("Bot", error, false);
            }


            AddMessage("Bot", "Hello! Welcome to the Cybersecurity Awareness Bot. What is your name?", false);

            InputBox.Focus();

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
                AddMessage("Bot", "You haven't typed anything yet. Ask me about passwords, phishing or safe browsing, or say 'tell me more' to continue the last topic.", false);
                InputBox.Clear();
                InputBox.Focus();
                return;
            }

            if (_awaitingName)
            {
                _bot.User.Name = _validator.CleanName(userInput);
                _awaitingName = false;

                AddMessage("You", userInput, true);

                AddMessage("Bot", $"Welcome, {_bot.User.Name}! I'm a Cybersecurity Awareness Bot. You can ask me about password safety, phishing, safe browsing, "
                    + "public wifi, privacy, online scams, links in emails and app permissions. You can also say 'tell me more' for another tip on the same topic.", false);

                InputBox.Clear();
                InputBox.Focus();
                return;
            }

            string label = string.IsNullOrWhiteSpace(_bot.User.Name) ? "You" : _bot.User.Name;
            AddMessage(label, userInput, true);

            AddMessage("Bot", _bot.GetReply(userInput), false);

            InputBox.Clear();
            InputBox.Focus();
        }
    }
}