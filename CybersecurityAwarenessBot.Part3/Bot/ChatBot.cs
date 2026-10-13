using CybersecurityAwarenessBot.Features;
using CybersecurityAwarenessBot.Database;
using MySqlConnector;
using System.Text.RegularExpressions;


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
        /// The shared activity log. Passed in by the window so the chatbot, quiz and task assistant all record to the same log.
        /// </summary>
        private readonly ActivityLog _log;
        
        /// <summary>
        /// How many log entries have been shown in the chat so far.
        /// </summary>
        private int _logShown;

        /// <summary>
        /// True if the last reply was the activity log, so "show more" means more of the log rather than another tip.
        /// </summary>
        private bool _lastReplyWasLog;

        /// <summary>
        /// Saves tasks and reminders that the user asks for in the chat. It is the same repository the Tasks tab uses, so both always agree.
        /// </summary>
        private readonly TaskRepository _tasks;

        /// <summary>
        /// Recognises task and reminder requests, however they are worded.
        /// </summary>
        private readonly TaskCommandRecogniser _taskCommands = new();

        /// <summary>
        /// The task the chatbot just asked "Would you like to set a reminder?" about, so the user's next answer ("yes, in 3 days") is applied to it.
        /// Null when the chatbot isn't waiting for an answer.
        /// </summary>
        private TaskItem? _pendingReminderTask;

        /// <summary>
        /// The user's details, it is public so that the window can read the name for message labels and set it when the user first enters it.
        /// </summary>
        public UserProfile User { get; } = new();

        /// <summary>
        /// A readable list of every topic the chatbot can talk about, so messages stay in step with the responses.
        /// </summary>
        public string TopicList => BotResponses.TopicList;

        /// <summary>
        /// Phrases that ask to see the activity log
        /// </summary>
        private static readonly string[] LogPhrases = { "activity log", "what have you done", "show log", "show the log", "show me the log",
            "recent actions", "show history" };

        /// <summary>
        /// Creates the chatbot with the activity log it should record its actions in, and the repository it saves tasks to.
        /// </summary>
        /// <param name="log">The shared activity log</param>
        /// <param name="tasks">The shared task repository</param>
        public ChatBot(ActivityLog log, TaskRepository tasks)
        {
            _log = log;
            _tasks = tasks;
        }

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
        public async Task<string> GetReplyAsync(string userInput)
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

            // "show more" straight after the log means more of the log, not another tip
            bool continuingLog = _lastReplyWasLog;
            _lastReplyWasLog = false;
            if (continuingLog && (input == "more" || input.Contains("show more")))
            {
                return ShowLog(fromStart: false);
            }

            // "Show activity log" or "What have you done for me?"
            if (LogPhrases.Any(phrase => input.Contains(phrase)))
            {
                return ShowLog(fromStart: true);
            }
            // "Add a task to enable 2FA" or "Remind me to update my password tomorrow", however it is worded
            TaskCommand? command = _taskCommands.Recognise(userInput);

            // An answer to "Would you like to set a reminder?", e.g. "yes, in 3 days" or "no thanks"
            if (_pendingReminderTask != null && (command == null || command.Title.Length == 0))
            {
                string? followUp = await HandleReminderFollowUpAsync(input, command);
                if (followUp != null)
                {
                    return followUp;
                }
            }

            if (command != null)
            {
                return await HandleTaskCommandAsync(command);
            }

            // Mentions tasks or reminders but isn't a request, so explain how instead of "I didn't understand"
            if (matched == null && (input.Contains("task") || input.Contains("remind")))
            {
                return "I can keep track of cybersecurity tasks for you. Try 'Add a task to enable two-factor authentication' or "
                    + "'Remind me to update my password tomorrow'. You can also see and manage your tasks in the Tasks tab.";
            }
            // Questions about what the chatbot remembers
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

                    _log.Add($"Remembered favourite topic: {_botResponses.GetDisplayName(matched)}");

                    return $"Great, I'll remember that you're interested in {_botResponses.GetDisplayName(matched)}, {User.Name}. "
                        + $"It's an important part of staying safe online.\n\n{_botResponses.GetTip(matched)}";
                }
            }

            // Only updates when a topic is actually found. A new topic is logged as a keyword-detection action.
            if (matched != null)
            {
                if (matched != User.CurrentTopic)
                {
                    _log.Add($"Recognised topic: {_botResponses.GetDisplayName(matched)}");
                }

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
        /// Adds the task the user asked for. If they gave a timeframe the reminder is set straight away; if not, the chatbot asks whether they want one.
        /// </summary>
        /// <param name="command">What the user asked for</param>
        /// <returns>A confirmation, a question about the reminder, or a message explaining what went wrong</returns>
        private async Task<string> HandleTaskCommandAsync(TaskCommand command)
        {
            _pendingReminderTask = null;

            // "Add a task" or "Remind me" on its own
            if (command.Title.Length == 0)
            {
                return command.IsReminderRequest
                    ? "What would you like me to remind you about? For example: 'Remind me to update my password tomorrow'."
                    : "What's the task? For example: 'Add a task to enable two-factor authentication'.";
            }

            try
            {
                // The description comes from the topic the title mentions, so the reply stays consistent with the chatbot's tips
                string description = _botResponses.GetTaskDescription(command.Title);
                TaskItem task = await _tasks.AddAsync(command.Title, description, command.ReminderDate);

                if (task.ReminderDate != null)
                {
                    string when = DescribeWhen(task.ReminderDate.Value);
                    return command.IsReminderRequest
                        ? $"Reminder set for '{task.Title}' {when}. I've added it to your tasks."
                        : $"Task added: '{task.Title}', with a reminder {when}.";
                }

                // No timeframe was given, so ask for one and remember which task the answer is for
                _pendingReminderTask = task;
                return command.IsReminderRequest
                    ? $"I've added '{task.Title}' to your tasks. When would you like me to remind you? You can say something like 'tomorrow', 'in 3 days' or '20 October'."
                    : $"Task added: '{task.Title}' with the description \"{description}\" Would you like to set a reminder for this task? "
                      + "You can say something like 'yes, in 3 days' or 'no thanks'.";
            }
            catch (ArgumentException ex)
            {
                return $"I couldn't add that task: {ex.Message}";
            }
            catch (MySqlException ex)
            {
                return DatabaseConnection.DescribeError(ex);
            }
        }

        /// <summary>
        /// Handles the user's answer after the chatbot asked "Would you like to set a reminder?".
        /// </summary>
        /// <param name="input">Normalised user input</param>
        /// <param name="command">The recognised command, if the answer was worded as one, e.g. "yes, remind me in 3 days"</param>
        /// <returns>The reply, or null if the user has moved on to something else</returns>
        private async Task<string?> HandleReminderFollowUpAsync(string input, TaskCommand? command)
        {
            TaskItem task = _pendingReminderTask!;
            DateTime? date = command?.ReminderDate ?? _taskCommands.FindTimeframe(input).Date;

            // "Yes, remind me in 3 days", "tomorrow", "next Friday"
            if (date != null)
            {
                _pendingReminderTask = null;
                try
                {
                    // False means the task was deleted in the Tasks tab before the user answered
                    if (!await _tasks.SetReminderAsync(task, date))
                    {
                        return $"I couldn't find '{task.Title}' any more. It may have been deleted in the Tasks tab.";
                    }

                    return $"Got it! I'll remind you about '{task.Title}' {DescribeWhen(date.Value)}.";
                }
                catch (ArgumentException ex)
                {
                    return $"I couldn't set that reminder: {ex.Message}";
                }
                catch (MySqlException ex)
                {
                    return DatabaseConnection.DescribeError(ex);
                }
            }

            // "No thanks", "not now"
            if (Regex.IsMatch(input, @"^(?:no|nope|nah|not now|not yet|skip|don't|dont)\b"))
            {
                _pendingReminderTask = null;
                return $"No problem, I've saved '{task.Title}' without a reminder. You can see it in the Tasks tab.";
            }

            // "Yes" or "remind me" without a time, so ask when (and keep waiting for the answer)
            if (Regex.IsMatch(input, @"^(?:yes|yeah|yep|yup|sure|ok|okay|please|definitely|of course)\b") || command != null)
            {
                return "When would you like me to remind you? You can say something like 'tomorrow', 'in 3 days', 'next week' or '20 October'.";
            }

            // Anything else means the user has moved on, so stop waiting and reply to it normally
            _pendingReminderTask = null;
            return null;
        }

        /// <summary>
        /// Describes a reminder date in a friendly way, e.g. "tomorrow (13 Oct 2026)", "in 3 days (15 Oct 2026)" or "on 20 Nov 2026".
        /// </summary>
        /// <param name="date">The reminder date</param>
        /// <returns>The description</returns>
        private static string DescribeWhen(DateTime date)
        {
            int days = (date.Date - DateTime.Today).Days;
            string exact = date.ToString("d MMM yyyy");

            return days switch
            {
                0 => $"today ({exact})",
                1 => $"tomorrow ({exact})",
                < 14 => $"in {days} days ({exact})",
                _ => $"on {exact}"
            };
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
        /// Lists the most recent actions from the activity log, newest first, one page at a time.
        /// </summary>
        /// <param name="fromStart">True to start from the newest action, false to carry on from where the last page ended</param>
        /// <returns>A numbered list of actions, or a message if there is nothing (more) to show</returns>
        private string ShowLog(bool fromStart)
        {
            if (fromStart)
            {
                _logShown = 0;
            }

            if (_log.Count == 0)
            {
                return $"I haven't done anything for you yet, {User.Name}. Try asking me about a topic, or try the quiz or task tabs.";
            }

            List<ActivityEntry> page = _log.GetPage(_logShown, ActivityLog.PageSize);

            if (page.Count == 0)
            {
                return "That's everything in the log. Say 'show activity log' to see the most recent actions again.";
            }

            string text = fromStart ? "Here's a summary of my recent actions, newest first:" : "Here are some earlier actions:";

            for (int i = 0; i < page.Count; i++)
            {
                text += $"\n{_logShown + i + 1}. {page[i]}";
            }

            _logShown += page.Count;
            _lastReplyWasLog = true;

            // Only offer more if there is more
            if (_logShown < _log.Count)
            {
                text += "\n\nSay 'show more' to see earlier actions.";
            }

            return text;
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

