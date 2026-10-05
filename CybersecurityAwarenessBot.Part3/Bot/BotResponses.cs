using System.Text.RegularExpressions;

namespace CybersecurityAwarenessBot.Bot
{
    /// <summary>
    /// Stores the chatbot's predefined responses and matches the user's input against them. A dictionary of lists is used so each topic can hold several tips, and
    /// new topics or tips can be added without changing the matching logic.
    /// </summary>
    internal class BotResponses
    {
        /// <summary>
        /// General questions about the chatbot itself. Kept separate from the cybersecurity topics so they never become the "current topic" that follow-up questions continue.
        /// </summary>
        private readonly Dictionary<string, string> _general = new()
        {
            ["how are you"] = "I am good. Thank you for asking.",
            ["your purpose"] = "My purpose is to provide you with safety tips to help you navigate the dangers online.",
            ["what can i ask"] = "You can ask me about password, phishing and browsing. I can also offer tips about public wifi, online scams, privacy, links in emails and app permissions." 
        };
        /// <summary>
        /// The dictionary maps each keyword to a list of responses. The keys are stored in lower case because the input is normalised before matching. Storing several
        /// responses per topic means the chatbot can pick a different tip each time, so the conversation stays varied rather than repeating the same line.
        /// </summary>
        private readonly Dictionary<string, List<string>> _responses = new()
        {
            // Key = keyword, Value = list of possible responses
            ["password"] = new List<string> {
                "Use a unique, long passphrase for every account and protect them all with multi-factor authentication (MFA).",
                "Never reuse a password across multiple accounts; if one site suffers a data breach, your other logins remain safe.",
                "Your password should include a blend of uppercase letters, lowercase letters, numbers, and special symbols if a system requires them.",
                "Use a reputable password manager to generate and securely store strong, unique passwords.",
                "Change passwords immediately if you suspect an account has been compromised or if a service reports a data breach."
            },
            ["phishing"] = new List<string> {
                "Check the sender's actual email address carefully and never click links or download attachments from unexpected messages.",
                "Avoid opening unexpected files, especially .zip, .exe, .js, or unverified office documents.",
                "Be wary of messages claiming dire consequences, account lockouts, or immediate action requirements.",
                "Be cautious of messages containing spelling mistakes, unusual formatting, or requests for sensitive information.",
                "Verify suspicious requests by contacting the person or organisation through a trusted phone number or official website."
            },
            ["browsing"] = new List<string> {
                "Before entering sensitive data, always check that the website URL starts with \"https\" and has the padlock icon to ensure your connection is fully encrypted.",
                "Turn on multi-factor authentication (MFA) to add an extra layer of protection to your accounts.",
                "Restrict site permissions and limit what data browsers and extensions can access.",
                "Keep your browser and its extensions updated to reduce the risk of security vulnerabilities being exploited.",
                "Only download software and files from trusted, official websites and avoid pirated or unfamiliar sources."
             },
            ["wifi"] = new List<string> {
                "Public Wifi networks lack strong encryption. This allows hackers to intercept your data, steal passwords, or spread malware.",
                "Do not log into online banking or shop with credit cards on public networks.",
                "Turn off AirDrop or network folder sharing so strangers cannot access your device files.",
                "Use your mobile data or a trusted VPN when accessing sensitive information on an unfamiliar public Wi-Fi network.",
                "Disable automatic connection to Wi-Fi networks so your device does not connect to potentially unsafe networks without your knowledge."
            },
            ["scam"] = new List<string> {
                "To protect yourself from online scams, always pause, stay calm, and independently verify any unexpected messages or requests before sharing personal information or clicking links.",
                "Do not click links or scan QR codes in unexpected texts or emails. Go directly to the official website instead.",
                "Be wary of urgent messages or callers demanding instant action to fix a \"crisis\" or \"locked account\".",
                "Never share passwords, PINs, one-time passwords (OTPs), or banking details with someone who contacts you unexpectedly.",
                "Research unfamiliar companies, offers, or investment opportunities independently before making payments or providing personal information."
            },
            ["link"] = new List<string> {
                "Do not follow any links in emails to reach Internet banking websites. Malicious software could redirect the link to a fake site.",
                "Place your mouse cursor over a link on a computer, or long-press it on a mobile device, to preview the actual URL.",
                "Navigate to sensitive portals, banks, or payment sites by typing the official URL directly into your browser.",
                "Check the domain name carefully for misspellings or unusual characters that could indicate a fake website.",
                "Avoid shortened or unfamiliar links when you cannot verify where they will lead before opening them."
             },
            ["permission"] = new List<string> {
                "Review app permissions before installing an application. For example it doesn't make sense for a torch app to need your contacts or location.",
                "Set sensitive permissions like location, microphone, or camera to \"Only while using the app\" or \"Ask every time.\"",
                "Regularly open your phone's Permission Manager to turn off access for apps you no longer use.",
                "Remove permissions from apps that no longer need access to sensitive information or device features.",
                "Only install apps from trusted sources and check the developer, reviews, and requested permissions before installing."
             },
            ["privacy"] = new List<string> {
                "Every social network is a treasure trove for scammers, who gather users private data. They can use this information for fraudulent activities. So it's a good idea to do a check-up on the security settings of your Facebook account, as well as for every other social network you use.",
                "Be very careful when you post any scans and photos online, especially when it comes to IDs, tickets and billing documents. It's also a bad idea to share information about your whereabouts and traveling schedule online.",
                "Review your social media privacy settings regularly and limit who can view your personal information and posts.",
                "Clear cookies and browsing data regularly, and review cookie settings to limit websites from tracking your online activity.",
                "Only provide personal information when it is necessary, and avoid sharing sensitive details such as your home address or identity number online."

            }
        };

        /// <summary>
        /// Created a default response in case the user's input has no match. It also tells the user what topics are available to ask the chatbot.
        /// </summary>
        private readonly string _defaultResponse = "I didn't quite understand. Could you please rephrase the question?"
            + "\nYou can ask me about password safety, phishing or safe browsing. I can also offer tips about public Wifi, privacy, online scams, links in emails and app permissions.";


        /// <summary>
        /// This is used to pick a random tip from a topic's list. Declared once as a field rather than creating it inside the method as creating several Random
        /// objects in quick succession can produce the same sequence of numbers.
        /// </summary>
        private readonly Random _random = new();

        /// <summary>
        /// Tracks which responses have already been shown for each topic, so the chatbot works through all of its tips before repeating any. Once a topic's tips are
        /// exhausted the list is cleared and the cycle starts again.
        /// </summary>
        private readonly Dictionary<string, List<int>> _shown = new();

        /// <summary>
        /// Readable names for each topic key, used whenever the chatbot mentions a topic.
        /// </summary>
        private readonly Dictionary<string, string> _displayNames = new()
        {
            ["password"] = "password safety",
            ["phishing"] = "phishing",
            ["browsing"] = "safe browsing",
            ["wifi"] = "public Wi-Fi",
            ["scam"] = "online scams",
            ["link"] = "links in emails",
            ["permission"] = "app permissions",
            ["privacy"] = "privacy"
        };

        /// <summary>
        /// A personalised tip for each topic, used when the chatbot refers back to the user's favourite topic.
        /// </summary>
        private readonly Dictionary<string, string> _recallTips = new()
        {
            ["password"] = "you might want to check whether any of your accounts still share the same password.",
            ["phishing"] = "remember to double-check the sender of any email that asks you to act quickly.",
            ["browsing"] = "it's worth checking which browser extensions you have installed and removing any you don't use.",
            ["wifi"] = "try switching off automatic Wi-Fi connections on your phone.",
            ["scam"] = "remember that no real bank will ever ask you for your OTP or PIN.",
            ["link"] = "get into the habit of hovering over links before you click them.",
            ["permission"] = "it's a good time to review which apps can access your location.",
            ["privacy"] = "you might want to review the security settings on your social media accounts."
        };

        /// <summary>
        /// Converts a topic key into its readable name.
        /// </summary>
        /// <param name="key">The topic keyword.</param>
        /// <returns>The readable name, or the key itself if there is no entry.</returns>
        public string GetDisplayName(string key)
        {
            return _displayNames.TryGetValue(key, out string? name) ? name : key;
        }

        /// <summary>
        /// Returns the personalised recall tip for a topic.
        /// </summary>
        public string GetRecallTip(string topic)
        {
            return _recallTips.TryGetValue(topic, out string? tip) ? tip : "it's worth reviewing your habits regularly.";
        }

        /// <summary>
        /// Searches the dictionary for the first keyword contained in the user's input and returns one of that topic's tips.
        /// </summary>
        /// <param name="input">This is the user's message, which is already trimmed and converted to lower case by the InputValidator.</param>
        /// <returns> 
        /// A tip for the first matching keyword, chosen at random from the tips not yet shown for that topic. Once all of a topic's tips have been used the cycle
        /// restarts and a short message is added to say so. If no keyword matches, the default response is returned.
        /// </returns>
        public string GetResponseMatch(string input)
        {
            // Check for general questions first
            foreach (var item in _general)
            {
                if (input.Contains(item.Key))
                {
                    return item.Value;
                }
            }

            // Check each keyword in turn and return as soon as one is found 
            foreach (var item in _responses)
            {
                if (Regex.IsMatch(input, $@"\b{Regex.Escape(item.Key)}s?\b"))
                {
                    List<string> options = item.Value;

                    // Only one option, so there is nothing to vary
                    if (options.Count == 1)
                    {
                        return options[0];
                    }

                    // First time a topic is asked about, this creates an empty record of which of its tips have been shown.
                    if (!_shown.ContainsKey(item.Key))
                    {
                        _shown[item.Key] = new List<int>();
                    }

                    List<int> used = _shown[item.Key];
                    string prefix = "";

                    // All tips have been shown for the topic, so record is cleared and the cycle starts again, letting the user know the tips are repeating
                    if (used.Count >= options.Count)
                    {
                        used.Clear();
                        prefix = "I've shared all my tips on that, here they are again:\n\n";
                    }

                    // Build a list of the tips not yet shown, then pick randomly from those
                    List<int> remaining = new();
                    for (int i = 0; i < options.Count; i++)
                    {
                        if (!used.Contains(i))
                        {
                            remaining.Add(i);
                        }
                    }

                    // Picking from the tips not shown
                    int index = remaining[_random.Next(remaining.Count)];
                    used.Add(index);

                    return prefix + options[index];

                }
            }
            // if nothing is found then it returns the default response
            return _defaultResponse;
        }

        /// <summary>
        /// This method returns the first topic keyword found in the user's input, or null if none match. Ensure the chatbot record which topic is being discussed without having to 
        /// work it out from the response text.
        /// </summary>
        /// <param name="input">Normalized user input</param>
        /// <returns>Matched keyword or null</returns>
        public string? FindTopic(string input) {
            foreach (var item in _responses)
            {
                if (Regex.IsMatch(input, $@"\b{Regex.Escape(item.Key)}s?\b"))
                {
                    return item.Key;
                }
            }
            return null;
        }

    }
}
