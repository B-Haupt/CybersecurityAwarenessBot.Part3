
namespace CybersecurityAwarenessBot.Bot
{
    /// <summary>
    /// The chatbot logic. This class takes the user's input and normalises it and returns a reply. No interface code here.
    /// </summary>
    internal class ChatBot
    {

        private readonly InputValidator _validator = new();
        private readonly BotResponses _botResponses = new();
        private readonly SentimentAnalyser _sentiment = new();

        /// <summary>
        /// The user's details, it is public so that the window can read the name for message labels and set it when the user first enters it.
        /// </summary>
        public UserProfile User { get; } = new();

        /// <summary>
        /// Keywords to end the conversation
        /// </summary>
        private static readonly string[] ExitInput = { "exit", "quit", "bye", "end", "good bye", "goodbye" };

        /// <summary>
        /// Words/phrases that prompt the chatbot to continue with the topic already being spoken about
        /// </summary>
        private static readonly string[] FollowUpPhrases = { "tell me more", "another tip", "more tips", "explain more", "go on", "what else", "more info", "tell me another", "anything else" };

        /// <summary>
        /// Takes in the user input and then returns the bot's reply.
        /// </summary>
        /// <param name="userInput">Raw text the user entered</param>
        /// <returns>Chatbot reply that is ready to be displayed in the chat</returns>
        public string GetReply(string userInput)
        {

            string input = _validator.NormaliseInput(userInput);

            string? matched = _botResponses.FindTopic(input);

            // A farewell is answered with a closing message rather than a tip. The window stays open so the user can carry on if they want to.
            if (ExitInput.Contains(input))
            {
                return $"Stay safe out there, {User.Name}! You asked {User.QuestionsAsked} question(s) today.";
            }

            // A follow-up question carries on with whatever topic was last spoken about
            if (FollowUpPhrases.Any(phrase => input.Contains(phrase)) && matched == null)
            {
                User.QuestionsAsked++;

                if (User.CurrentTopic != null)
                {
                    return _botResponses.GetResponseMatch(User.CurrentTopic);
                }

                return $"Happy to say more, {User.Name}, but which topic would you like? You can ask me about passwords, phishing, safe browsing, privacy, " +
                       "public Wi-Fi, scams, links in emails or app permissions.";
            }

            // Increase question counter
            User.QuestionsAsked++;

            // The user is telling the chatbot which topic interests them, so store it and answer with a tip straight away.
            if (input.Contains("interested in") || input.Contains("favourite topic"))
            {
                if (matched != null)
                {
                    User.FavouriteTopic = matched;
                    User.CurrentTopic = matched;
                    User.LastRecallAt = null;

                    return $"Great, I'll remember that you're interested in {_botResponses.GetDisplayName(matched)}, {User.Name}. It's an important part of staying safe online.\n\n{_botResponses.GetResponseMatch(input)}";
                }
            }

            // Only updates when a topic is actually found
            if (matched != null) 
            {
                User.CurrentTopic = matched;
            }

            string reply = _botResponses.GetResponseMatch(input);

            string? topicForFeeling = matched ?? User.CurrentTopic;
            string? feeling = _sentiment.Detect(input, User.Name, topicForFeeling == null ? null : _botResponses.GetDisplayName(topicForFeeling));

            if (feeling != null)
            {
                if (matched == null)
                {
                    // The brief asks that confusion continues the current topic
                    if (User.CurrentTopic != null)
                    {
                        return feeling + "\n\n" + _botResponses.GetResponseMatch(User.CurrentTopic);
                    }

                    // Nothing discussed yet
                    return feeling + "\n\nWhat's on your mind? I can help with passwords, phishing, " +
                           "safe browsing, privacy, public Wi-Fi, scams, links in emails or app permissions.";
                }

                reply = feeling + "\n\n" + reply;
            }

            return reply + GetRecall(matched);
        }

        /// <summary>
        /// Refers back to the user's favourite topic the first time they ask about a different topic, then at most once every three questions so it doesn't repeat on every message.
        /// </summary>
        /// <param name="matched">The topic in the current message or null if no topic is chosen</param>
        /// <returns>The recall line to add to the end of the reply, or an empty string if no recall is needed</returns>        
        private string GetRecall(string? matched)
        {
            if (User.FavouriteTopic == null || matched == null || matched == User.FavouriteTopic)
            {
                return "";
            }

            if (User.LastRecallAt != null && User.QuestionsAsked - User.LastRecallAt < 3)
            {
                return "";
            }

            User.LastRecallAt = User.QuestionsAsked;

            return $"\n\nBy the way {User.Name}, as someone interested in {_botResponses.GetDisplayName(User.FavouriteTopic)}, "
                + _botResponses.GetRecallTip(User.FavouriteTopic);
        }
    }
}

