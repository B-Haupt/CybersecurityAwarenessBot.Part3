using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CybersecurityAwarenessBot.Features
{
    /// <summary>
    /// This class set how one action is recorded in the activity log. It records when the action happened and short description of what the chatbot did. 
    /// </summary>
    class ActivityEntry
    {
        /// <summary>
        /// This is set once when the entry is created. The date and time the action happened.
        /// </summary>
        public DateTime Time { get; }

        /// <summary>
        /// The short description of the action
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// This creates an entry stamped with the current time
        /// </summary>
        /// <param name="description">What the chatbot did</param>
        public ActivityEntry(string description) 
        {
            Time = DateTime.Now;
            Description = description;
        }

        /// <summary>
        /// Overrides the toString to format the entry for display
        /// </summary>
        /// <returns> The time followed by the description</returns>
        public override string ToString()
        {
            return $"{Time:HH:mm}  {Description}";
        }
    }
}
