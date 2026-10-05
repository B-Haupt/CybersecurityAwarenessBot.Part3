namespace CybersecurityAwarenessBot.Bot
{
    /// <summary>
    /// This class is used to check and clean up the input that is coming from the user. I put this in its own class so that the validation rules live in one
    /// place instead of being repeated everywhere when the user is giving input to the program.
    /// </summary>
    internal class InputValidator
    {
        /// <summary>
        /// Check the user has input something usable and returns false for null, empty or whitespace. I used string.IsNullOrWhiteSpace as it catches input that is only spaces.
        /// </summary>
        /// <param name="input">The text entered by the user.</param>
        /// <returns>False if the input is null, empty or whitespace.</returns>
        public bool IsValidInput(string input)
        {
            return !string.IsNullOrWhiteSpace(input);
        }

        /// <summary>
        /// Trims the extra space and then puts it to lowercase. This is done for the response lookup so that if the user types a word in all caps it would still match
        /// the response list. Also used replace to replace the word wi-fi with wifi if the user typed it in. Plus remove punctuation characters.
        /// </summary>
        /// <param name="input">User input.</param>
        /// <returns>The trimmed, lowercase version of the input.</returns>
        public string NormaliseInput(string input)
        {
            return input.Trim()
            .ToLower()
            .TrimEnd('.', '!', '?', ',')
            .Replace("wi-fi", "wifi")
            .Replace("wi fi", "wifi");
        }

        /// <summary>
        /// Pulls the name out of what the user typed. People often answer with a whole sentence such as "my name is Brittany", so the common lead-in phrases are
        /// stripped before the name is stored.
        /// </summary>
        /// <param name="input">The raw text the user typed.</param>
        /// <returns>The name on its own, capitalised.</returns>
        public string CleanName(string input)
        {
            string name = input.Trim();

            string[] leadIns = { "my name is", "my name's", "i am", "i'm", "it is", "it's", "call me", "this is" };

            foreach (string leadIn in leadIns)
            {
                if (name.StartsWith(leadIn, StringComparison.OrdinalIgnoreCase))
                {
                    name = name.Substring(leadIn.Length).Trim();
                    break;
                }
            }

            // Remove any trailing punctuation the user typed
            name = name.TrimEnd('.', '!', '?', ',');

            // Capitalise the first letter so "brittany" displays as "Brittany"
            if (name.Length > 0)
            {
                name = char.ToUpper(name[0]) + name.Substring(1);
            }
            return name;
        }

    }
}
