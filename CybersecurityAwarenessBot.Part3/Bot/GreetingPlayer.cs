using System.Media;
using System.IO;

namespace CybersecurityAwarenessBot.Bot
{
    /// <summary>
    /// Class that calls the voice greeting when the application is started. Does not write its own error messages as Part 2 has no console. It returns a message
    /// describing what happens and the window decides how it will be displayed.
    /// </summary>
    internal class GreetingPlayer
    {
        /// <summary>
        /// Stating the path to get greeting Bot file
        /// </summary>
        private const string GreetingPathway = @"Media\Bot.wav";

        /// <summary>
        /// This method plays the voice greeting. It is written so that if the audio file is missing or can't play, that a warning message will be shown instead of the
        /// program just crashing. 
        /// </summary>
        /// <returns>
        /// Returns null if the greeting played is successfully, otherwise it returns a message explaining that the audio did not work.
        /// </returns>
        public string? PlayGreeting()
        {
            try
            {
                // Checking if the file exists and if not it return a warning message.
                if (!File.Exists(GreetingPathway))
                {
                    return "Audio File not found: Voice greeting skipped.";

                }

                // Creating a SoundPlayer Object to play the WAV file and "using" disposes the player automatically when the method ends
                using var player = new SoundPlayer(GreetingPathway);
                player.Load();
                // Ensure message and logo doesn't appear until the greeting is finished.
                player.PlaySync();
                return null;

            }
            catch (Exception ex)
            {
                // Catches any error that may arise
                return "Voice greeting could not be played. Error: " + ex.Message;
            }
        }
    }
}
