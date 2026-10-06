using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CybersecurityAwarenessBot.Features
{
    /// <summary>
    /// This class records the actions the chatbot takes. Every feature writes to the same log, and the log can then be shown
    /// </summary>
    internal class ActivityLog
    {
        /// <summary>
        /// This shows how many entries are shown. 
        /// </summary>
        public const int PageSize = 10;

        /// <summary>
        /// Every entry, oldest first.
        /// </summary>
        public readonly List<ActivityEntry> _entries = new();

        /// <summary>
        /// This raised whenever an entry is added.
        /// </summary>
        public event Action? EntryAdded;

        /// <summary>
        /// Total number of entries recorded.
        /// </summary>
        public int Count => _entries.Count;

        /// <summary>
        /// This records a new action and tell anything listening that the log has changed.
        /// </summary>
        /// <param name="description"></param>
        public void Add(string description) {
            _entries.Add(new ActivityEntry(description));
            EntryAdded?.Invoke();
        }

        /// <summary>
        /// This returns a page of entries, newest first.
        /// </summary>
        /// <param name="start">How many of the newest entries to skip</param>
        /// <param name="count">How many entries to return</param>
        /// <returns>Up to count entries, with the newest first</returns>
        public List<ActivityEntry> GetPage(int start, int count) {
            List<ActivityEntry> page = new();

            for (int i = _entries.Count - 1 - start; i >= 0 && page.Count < count; i--) {
                page.Add(_entries[i]);
            }

            return page;
        }
    }
}
