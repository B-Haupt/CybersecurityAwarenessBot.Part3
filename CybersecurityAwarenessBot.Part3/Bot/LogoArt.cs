namespace CybersecurityAwarenessBot.Bot
{
    /// <summary>
    /// Class where the Logo Display Art is stored. This class only supplies the art as text and the window is responsible for displaying it.
    /// </summary>
    internal class LogoArt
    {
        /// <summary>
        /// Art work is the title with a small shield next to it. A raw string is used so that backslashes don't need to be added.
        /// </summary>
        private const string Logo = """
            ╔═╗┬ ┬┌┐ ┌─┐┬─┐┌─┐┌─┐┌─┐┬ ┬┬─┐┬┌┬┐┬ ┬  ╔═╗┬ ┬┌─┐┬─┐┌─┐┌┐┌┌─┐┌─┐┌─┐  ╔╗ ┌─┐┌┬┐  /\
            ║  └┬┘├┴┐├┤ ├┬┘└─┐├┤ │  │ │├┬┘│ │ └┬┘  ╠═╣│││├─┤├┬┘├┤ │││├┤ └─┐└─┐  ╠╩╗│ │ │  |<>|
            ╚═╝ ┴ └─┘└─┘┴└─└─┘└─┘└─┘└─┘┴└─┴ ┴  ┴   ╩ ╩└┴┘┴ ┴┴└─└─┘┘└┘└─┘└─┘└─┘  ╚═╝└─┘ ┴   \/
            """;

        /// <summary>
        /// Method returns the art logo so that the caller can display it.
        /// </summary>
        public string GetLogo()
        {
            return Logo;
        }




    }
}
