using System.Globalization;
using System.Text.RegularExpressions;

namespace CybersecurityAwarenessBot.Bot
{
    /// <summary>
    /// What the user asked for when they typed a task or reminder request. A record is a short way to write a class that just holds values.
    /// </summary>
    /// <param name="Title">The task or empty if the user only gave a time</param>
    /// <param name="ReminderDate">The date worked out from phrases like "tomorrow" or "in 3 days", or null if no time was given</param>
    /// <param name="IsReminderRequest">True for "remind me..." and "set a reminder...", false for "add a task..."</param>
    internal record TaskCommand(string Title, DateTime? ReminderDate, bool IsReminderRequest);

    /// <summary>
    /// Simulates natural language processing for tasks and reminders. Regular expressions recognise the many ways people ask for the same thing, then pull out the task and any timeframe.
    /// </summary>
    internal class TaskCommandRecogniser
    {
        /// <summary>
        /// Ways of asking to add a task.
        /// </summary>
        private static readonly Regex[] AddTaskPatterns =
        {
            // "Add a task to enable 2FA", "Can you create a new task: review privacy settings", "Add task - Review privacy settings"
            new(@"^(?:please\s+)?(?:(?:can|could|would|will)\s+you\s+)?(?:please\s+)?(?:add|create|make|set\s+up|start)\s+(?:me\s+)?(?:a\s+|an\s+|another\s+|one\s+more\s+)?(?:new\s+)?task\b\s*(?:to|for|called|named|about|:|-)?\s*(?<title>.*)$", RegexOptions.IgnoreCase),
            // "Add 'update my antivirus' to my tasks", "Put backing up my files on my to-do list"
            new(@"^(?:please\s+)?(?:add|put)\s+(?<title>.+?)\s+(?:to|on|onto)\s+my\s+(?:tasks?|task\s+list|to-?do\s+list|list)$", RegexOptions.IgnoreCase),
            // "New task: back up my phone", "Task - Review privacy settings"
            new(@"^(?:new\s+)?task\s*[:\-]\s*(?<title>.+)$", RegexOptions.IgnoreCase)
        };

        /// <summary>
        /// Ways of asking for a reminder. These create a task that has a reminder.
        /// </summary>
        private static readonly Regex[] ReminderPatterns =
        {
            // "Remind me to update my password tomorrow", "Can you remind me in 3 days to review privacy settings?"
            new(@"\bremind\s+me\b\s*(?:to\s+|about\s+|that\s+i\s+(?:need|have)\s+to\s+)?(?<title>.*)$", RegexOptions.IgnoreCase),
            // "Add a reminder to check my privacy settings", "Set a reminder for Friday to back up my files"
            new(@"^(?:please\s+)?(?:(?:can|could|would|will)\s+you\s+)?(?:please\s+)?(?:set|add|create|make)\s+(?:me\s+)?(?:a\s+|an\s+)?reminder\b\s*(?:to|for|about|:|-)?\s*(?<title>.*)$", RegexOptions.IgnoreCase),
            // "Reminder: change my router password"
            new(@"^reminder\s*[:\-]\s*(?<title>.+)$", RegexOptions.IgnoreCase)
        };

        /// <summary>
        /// Number words, so "in two days" works as well as "in 2 days".
        /// </summary>
        private static readonly Dictionary<string, int> NumberWords = new()
        {
            ["a"] = 1,
            ["an"] = 1,
            ["one"] = 1,
            ["two"] = 2,
            ["three"] = 3,
            ["four"] = 4,
            ["five"] = 5,
            ["six"] = 6,
            ["seven"] = 7,
            ["eight"] = 8,
            ["nine"] = 9,
            ["ten"] = 10,
            ["eleven"] = 11,
            ["twelve"] = 12,
            ["a couple of"] = 2,
            ["a few"] = 3
        };

        /// <summary>
        /// Words that can come before a time phrase, e.g. "on Friday", "by tomorrow", "for next week". They are removed with the phrase.
        /// </summary>
        private const string Lead = @"(?:\b(?:on|by|for|before|at|this|in)\s+)?";

        /// <summary>
        /// The month names and short forms accepted in dates like "20 October" or "3 Nov 2026".
        /// </summary>
        private const string Months = @"jan(?:uary)?|feb(?:ruary)?|mar(?:ch)?|apr(?:il)?|may|june?|july?|aug(?:ust)?|sep(?:t(?:ember)?)?|oct(?:ober)?|nov(?:ember)?|dec(?:ember)?";

        /// <summary>
        /// Checks whether the user is asking to add a task or set a reminder, and if so works out the task and any reminder date.
        /// </summary>
        /// <param name="userInput">What the user typed, before it was lower-cased</param>
        /// <returns>The command, or null if the message isn't a task or reminder request</returns>
        public TaskCommand? Recognise(string userInput)
        {
            string text = userInput.Trim().TrimEnd('.', '!', '?');

            // Reminder requests are checked first
            foreach (Regex pattern in ReminderPatterns)
            {
                Match match = pattern.Match(text);
                if (match.Success)
                {
                    return BuildCommand(match.Groups["title"].Value, isReminder: true);
                }
            }

            foreach (Regex pattern in AddTaskPatterns)
            {
                Match match = pattern.Match(text);
                if (match.Success)
                {
                    return BuildCommand(match.Groups["title"].Value, isReminder: false);
                }
            }

            return null;
        }

        /// <summary>
        /// Looks for a timeframe anywhere in the text
        /// </summary>
        /// <param name="text">The text to search</param>
        /// <returns>The date the timeframe means (or null if there isn't one), and the text with the timeframe removed</returns>
        public (DateTime? Date, string Remainder) FindTimeframe(string text)
        {
            DateTime today = DateTime.Today;

            // Checked in this order so "day after tomorrow" isn't read as just "tomorrow"
            var rules = new List<(Regex Pattern, Func<Match, DateTime?> ToDate)>
            {
                (new Regex(Lead + @"\b(?:the\s+)?day\s+after\s+tomorrow\b", RegexOptions.IgnoreCase), m => today.AddDays(2)),
                (new Regex(Lead + @"\btomorrow\b", RegexOptions.IgnoreCase), m => today.AddDays(1)),
                (new Regex(Lead + @"\b(?:today|tonight|later\s+today)\b(?!')", RegexOptions.IgnoreCase), m => today),
                (new Regex(@"\b(?:in|within)\s+(?<n>\d{1,3}|a\s+couple\s+of|a\s+few|an?|one|two|three|four|five|six|seven|eight|nine|ten|eleven|twelve)\s+(?<unit>days?|weeks?|months?)(?:\s+time)?\b", RegexOptions.IgnoreCase),
                    m => AddUnits(today, ReadNumber(m.Groups["n"].Value), m.Groups["unit"].Value)),
                (new Regex(Lead + @"\bnext\s+(?<unit>week|month)\b", RegexOptions.IgnoreCase), m => AddUnits(today, 1, m.Groups["unit"].Value)),
                (new Regex(Lead + @"\b(?:next\s+)?(?<day>monday|tuesday|wednesday|thursday|friday|saturday|sunday)\b", RegexOptions.IgnoreCase),
                    m => NextWeekday(today, m.Groups["day"].Value)),
                (new Regex(Lead + @"(?:\bthe\s+)?\b(?<date>\d{1,2}(?:st|nd|rd|th)?\s+(?:of\s+)?(?:" + Months + @")(?:\s+\d{4})?)\b", RegexOptions.IgnoreCase),
                    m => ParseDate(m.Groups["date"].Value, today)),
                (new Regex(Lead + @"\b(?<date>\d{4}-\d{1,2}-\d{1,2}|\d{1,2}/\d{1,2}/\d{4})\b", RegexOptions.IgnoreCase),
                    m => ParseDate(m.Groups["date"].Value, today))
            };

            foreach (var rule in rules)
            {
                Match match = rule.Pattern.Match(text);
                if (match.Success)
                {
                    DateTime? date = rule.ToDate(match);
                    if (date != null)
                    {
                        string remainder = text.Remove(match.Index, match.Length);
                        return (date, remainder);
                    }
                }
            }

            return (null, text);
        }

        /// <summary>
        /// Turns the captured text into a command: takes out the timeframe, then tidies what is left into a title.
        /// </summary>
        private TaskCommand BuildCommand(string captured, bool isReminder)
        {
            (DateTime? date, string remainder) = FindTimeframe(captured);
            return new TaskCommand(CleanTitle(remainder), date, isReminder);
        }

        /// <summary>
        /// Tidies the words left over into a task title
        /// </summary>
        private static string CleanTitle(string text)
        {
            // Collapse the gaps left where the timeframe was removed
            string title = Regex.Replace(text, @"\s+", " ").Trim();

            // Remove joining words and politeness left at either end
            title = Regex.Replace(title, @"^(?:(?:to|about|for|that|i\s+need\s+to|i\s+have\s+to|please|and|-|:)\s+)+", "", RegexOptions.IgnoreCase);
            title = Regex.Replace(title, @"(?:\s+(?:please|thanks|thank\s+you|to|and|for|on|by))+$", "", RegexOptions.IgnoreCase);
            title = title.Trim(' ', '.', ',', '!', '?', '-', ':', '\'', '"');

            // Capitalise the first letter, like a task title
            return title.Length == 0 ? "" : char.ToUpper(title[0]) + title.Substring(1);
        }

        /// <summary>
        /// Reads "3", "three", "a" or "a couple of" as a number.
        /// </summary>
        private static int ReadNumber(string text)
        {
            string word = Regex.Replace(text.ToLowerInvariant(), @"\s+", " ");
            return int.TryParse(word, out int number) ? number : NumberWords[word];
        }

        /// <summary>
        /// Adds days, weeks or months to a date.
        /// </summary>
        private static DateTime AddUnits(DateTime start, int amount, string unit)
        {
            string lower = unit.ToLowerInvariant();
            if (lower.StartsWith("week"))
            {
                return start.AddDays(7 * amount);
            }

            if (lower.StartsWith("month"))
            {
                return start.AddMonths(amount);
            }

            return start.AddDays(amount);
        }

        /// <summary>
        /// The next date that falls on the given weekday
        /// </summary>
        private static DateTime NextWeekday(DateTime today, string dayName)
        {
            DayOfWeek target = Enum.Parse<DayOfWeek>(dayName, ignoreCase: true);
            int days = ((int)target - (int)today.DayOfWeek + 7) % 7;
            return today.AddDays(days == 0 ? 7 : days);
        }

        /// <summary>
        /// Reads a written date such as "20 October", "3rd of Nov 2026", "2026-10-20" or "20/10/2026". A date without a year that has already passed means next year.
        /// </summary>
        private static DateTime? ParseDate(string text, DateTime today)
        {
            // Remove "st", "nd", "rd", "th" and "of", e.g. "3rd of November" becomes "3 November"
            string cleaned = Regex.Replace(text, @"(?<=\d)(?:st|nd|rd|th)\b|\bof\b", "", RegexOptions.IgnoreCase);
            cleaned = Regex.Replace(cleaned, @"\s+", " ").Trim();

            // "Sept" isn't a standard short form, so it becomes "Sep" before parsing
            cleaned = Regex.Replace(cleaned, @"\bsept\b", "Sep", RegexOptions.IgnoreCase);

            string[] withYear = { "d MMMM yyyy", "d MMM yyyy", "yyyy-M-d", "d/M/yyyy" };
            string[] withoutYear = { "d MMMM", "d MMM" };

            if (DateTime.TryParseExact(cleaned, withYear, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out DateTime full))
            {
                return full.Date;
            }

            if (DateTime.TryParseExact(cleaned, withoutYear, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out DateTime partial))
            {
                DateTime thisYear = new(today.Year, partial.Month, partial.Day);
                return thisYear < today ? thisYear.AddYears(1) : thisYear;
            }

            return null;
        }
    }
}