 
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
        /// A readable list of every topic the chatbot can talk about, so messages stay in step with the responses.
        /// </summary>
        public string TopicList => BotResponses.TopicList;

        /// <summary>
        /// Keywords to end the conversation
        /// </summary>
        private static readonly string[] ExitInput = { "exit", "quit", "bye", "end", "good bye", "goodbye" };

        /// <summary>
        /// Words/phrases that prompt the chatbot to continue with the topic already being spoken about
        /// </summary>
        private static readonly string[] FollowUpPhrases = { "tell me more", "another tip", "more tips", "explain more", "what else", "more info", 
                                                            "tell me another", "anything else", "more details", "elaborate", "give me more", "one more" };

        /// <summary>
        /// This is short follow-ups that only count when they are the whole message
        /// </summary>
        private static readonly string[] FollowUpExact = { "more", "go on", "continue", "next", "another" };

        /// <summary>
        /// Phrases that show the user didn't follow the last tip, so the chatbot explains the same topic another way
        /// </summary>
        private static readonly string[] ConfusionPhrases = { "don't understand", "dont understand", "don't get it", "dont get it",
                                                                "what do you mean", "explain that", "not sure what you mean" };

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

            // Increase question counter
            User.QuestionsAsked++;

            // Questions about what the chatbot remembers, e.g. "what is my favourite topic?"
            string? memoryAnswer = AnswerMemoryQuestion(input, matched);
            if (memoryAnswer != null)
            {
                return memoryAnswer;
            }

            // A follow-up or a confused reply with no new topic carries on with whatever topic was last spoken about
            bool isFollowUp = FollowUpPhrases.Any(phrase => input.Contains(phrase)) || FollowUpExact.Contains(input);
            bool isConfused = ConfusionPhrases.Any(phrase => input.Contains(phrase));
            if (matched == null && (isFollowUp || isConfused))
            {
                return HandleFollowUp(input, isConfused);
            }

            // The user is telling the chatbot which topic interests them, so store it and answer with a tip straight away.
            if (input.Contains("interested in") || input.Contains("favourite topic") || input.Contains("favorite topic"))
            {
                if (matched != null)
                {
                    User.FavouriteTopic = matched;
                    User.CurrentTopic = matched;

                    // A new favourite topic resets the recall so it is mentioned again the next time the user asks about something else
                    User.LastRecallAt = null;

                    return $"Great, I'll remember that you're interested in {_botResponses.GetDisplayName(matched)}, {User.Name}. "
                        + $"It's an important part of staying safe online.\n\n{_botResponses.GetTip(matched)}";
                }
            }

            // Only updates when a topic is actually found
            if (matched != null)
            {
                User.CurrentTopic = matched;
            }

            string reply = _botResponses.GetResponseMatch(input);

            string? feeling = DetectFeeling(input, matched ?? User.CurrentTopic);

            if (feeling != null)
            {
                if (matched == null)
                {
                    // The brief asks that confusion continues the current topic
                    if (User.CurrentTopic != null)
                    {
                        return feeling + "\n\n" + _botResponses.GetTip(User.CurrentTopic);
                    }

                    // Nothing discussed yet
                    return feeling + $"\n\nWhat would you like to know? I can help with {TopicList}.";
                }

                reply = feeling + "\n\n" + reply;
            }

            return reply + GetRecall(matched);
        }

        /// <summary>
        /// Answers questions about what the chatbot remembers: the user's name, their favourite topic, or everything at once.
        /// </summary>
        /// <param name="input">Normalised user input</param>
        /// <param name="matched">The topic in the message, or null</param>
        /// <returns>The answer, or null if the message isn't a memory question</returns>
        private string? AnswerMemoryQuestion(string input, string? matched)
        {
            // Only questions count, so "my name is..." or "my favourite topic is..." are not treated as questions
            bool isQuestion = input.StartsWith("what") || input.StartsWith("who") || input.Contains("do you know") || input.Contains("do you remember");
            if (!isQuestion)
            {
                return null;
            }

            if (input.Contains("my name") || input.Contains("who am i"))
            {
                return $"Your name is {User.Name}. I remember it so I can keep our chat personal.";
            }

            // Only when no topic is named, so "what? I'm interested in privacy" still stores a new favourite
            if (matched == null && (input.Contains("favourite topic") || input.Contains("favorite topic") || input.Contains("interested in")))
            {
                if (User.FavouriteTopic == null)
                {
                    return $"You haven't told me your favourite topic yet, {User.Name}. Try saying something like 'I'm interested in privacy'.";
                }

                // Make the favourite the current topic, so "tell me more" gives a tip on it
                User.CurrentTopic = User.FavouriteTopic;
                return $"You told me you're interested in {_botResponses.GetDisplayName(User.FavouriteTopic)}, {User.Name}. "
                    + "Say 'tell me more' if you'd like another tip on it.";
            }

            if (matched == null && (input.Contains("remember") || input.Contains("know about me")))
            {
                return BuildMemorySummary();
            }

            return null;
        }

        /// <summary>
        /// Lists everything the chatbot has stored about the user.
        /// </summary>
        /// <returns>A sentence summarising the user's profile</returns>
        private string BuildMemorySummary()
        {
            string summary = $"Here's what I remember: your name is {User.Name}";

            if (User.FavouriteTopic != null)
            {
                summary += $", you're interested in {_botResponses.GetDisplayName(User.FavouriteTopic)}";
            }

            if (User.CurrentTopic != null && User.CurrentTopic != User.FavouriteTopic)
            {
                summary += $", we were last talking about {_botResponses.GetDisplayName(User.CurrentTopic)}";
            }

            summary += $" and you've asked me {User.QuestionsAsked} question(s) so far.";
            return summary;
        }

        /// <summary>
        /// Continues the current topic. If the user said they didn't understand, the tip is introduced as another way of looking at it.
        /// </summary>
        /// <param name="input">Normalised user input</param>
        /// <param name="isConfused">True if the user said they didn't understand</param>
        /// <returns>Another tip on the current topic, or a question if there is no topic yet</returns>
        private string HandleFollowUp(string input, bool isConfused)
        {
            if (User.CurrentTopic == null)
            {
                return $"Happy to help, {User.Name}, but which topic would you like? You can ask me about {TopicList}.";
            }

            string tip = _botResponses.GetTip(User.CurrentTopic);

            if (isConfused)
            {
                return $"No problem, {User.Name}. Here's another way to look at {_botResponses.GetDisplayName(User.CurrentTopic)}:\n\n{tip}";
            }

            // A follow-up can carry a feeling too, e.g. "I'm worried, tell me more"
            string? feeling = DetectFeeling(input, User.CurrentTopic);
            return feeling == null ? tip : feeling + "\n\n" + tip;
        }

        /// <summary>
        /// Runs sentiment detection using the readable topic name.
        /// </summary>
        /// <param name="input">Normalised user input</param>
        /// <param name="topic">The topic key, or null</param>
        /// <returns>A supportive message, or null if no feeling was found</returns>
        private string? DetectFeeling(string input, string? topic)
        {
            string? display = topic == null ? null : _botResponses.GetDisplayName(topic);
            return _sentiment.Detect(input, User.Name, display);
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

