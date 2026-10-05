using System;

namespace CybersecurityAwarenessBot.Bot
{
    /// <summary>
    /// Class is used to detect the emotion in the user's message and build a supportive reply to go with the cybersecurity tip. A delegate is used so that each sentiment's reply is
    /// stored as a method in a dictionary.
    /// </summary>
    internal class SentimentAnalyser
    {

        /// <summary>
        /// Set out the shape of a method that builds a sentiment reply - it takes in the user's name and the topic being discussed and return the text to place before the tip.
        /// </summary>
        /// <param name="name">User's name</param>
        /// <param name="topic">Topic being discussed</param>
        /// <returns>Text to place before the cybersecurity tip</returns>
        public delegate string SentimentResponse(string name, string topic);

        /// <summary>
        /// Maps the words that signals each sentiment to the method that builds the matching reply
        /// </summary>
        private readonly Dictionary<string[], SentimentResponse> _sentiments;

        public SentimentAnalyser()
        {
            _sentiments = new Dictionary<string[], SentimentResponse>
            {
                [new[] { "worried", "scared", "nervous", "afraid", "anxious" }] = BuildWorriedReply,
                [new[] { "frustrated", "confused", "annoyed", "overwhelmed" }] = BuildFrustratedReply,
                // Removed 'interested' as it is used in my memory statement
                [new[] { "keen", "curious", "want to learn" }] = BuildCuriousReply

            };
        }

        /// <summary>
        /// Searches the user's message for words that signal a sentiment
        /// </summary>
        /// <param name="input">Normalised user input</param>
        /// <param name="name">User's name</param>
        /// <param name="topic">Topic being discussed or null if unknown</param>
        /// <returns>A supporting message or null if no sentiment was found</returns>
        public string? Detect(string input, string name, string? topic)
        {
            foreach (var entry in _sentiments)
            {
                foreach (var word in entry.Key)
                {
                    if (input.Contains(word))
                    {
                        //entry.Value holds a method
                        return entry.Value(name, topic ?? "this");
                    }

                }
            }
            return null;
        }

        private string BuildWorriedReply(string name, string topic)
        {
            return $"It's completely normal to feel that way, {name}. " + $"{Capitalise(topic)} catches a lot of people out, and being cautious is the right instinct. "
                + "Here's something that will help:";
        }

        private string BuildFrustratedReply(string name, string topic)
        {
            return $"I completely understand, {name}. {Capitalise(topic)} can feel overwhelming to try and stay ahead of. " + "It's important to take it one step at a time:";
        }

        private string BuildCuriousReply(string name, string topic)
        {
            return $"Well done on being curious, {name}. It's important to learn about {topic} as it is one of the best ways to stay safe. "
                + "Here is some information about it:";
        }

        /// <summary>
        /// Method to capitalise the first letter of a word
        /// </summary>
        /// <param name="word">Word that need to be capitalised</param>
        /// <returns>Capitalised word</returns>
        private static string Capitalise(string word)
        {

            if (string.IsNullOrEmpty(word))
            {
                return word;
            }

            return char.ToUpper(word[0]) + word.Substring(1);
        }
    }
}
