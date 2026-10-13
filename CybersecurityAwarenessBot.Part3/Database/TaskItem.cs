using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CybersecurityAwarenessBot.Database
{
    /// <summary>
    /// This is one cybersecurity task as stored in a row of the tasks table. The properties are read-only as a task is only changed through the TaskRepository.
    /// This ensures that the window shows always matches what is in the database.
    /// </summary>
    internal class TaskItem
    {

        /// <summary>
        /// Task's unique number, which will be given by MySQL. This will be used to find the row again when it is completed, changed or deleted.
        /// </summary>
        public int Id { get; }

        /// <summary>
        /// Short name for the task
        /// </summary>
        public string Title { get; }

        /// <summary>
        /// Details about the task
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// Date you must be reminded on or null if the user didn't set a reminder
        /// </summary>
        public DateTime? ReminderDate { get; }

        /// <summary>
        /// This becomes true once the user has marked the task as completed.
        /// </summary>
        public bool IsCompleted { get; }

        /// <summary>
        /// When the task was added.
        /// </summary>
        public DateTime CreatedAt { get; }

        /// <summary>
        /// Creates a task from the values stored in the database.
        /// </summary>

        public TaskItem(int id, string title, string description, DateTime? reminderDate, bool isCompleted, DateTime createdAt) 
        {
            Id = id;
            Title = title;
            Description = description;
            ReminderDate = reminderDate;
            IsCompleted = isCompleted;
            CreatedAt = createdAt;
        }

        /// <summary>
        /// Reminder for display
        /// </summary>
        public string ReminderText => ReminderDate == null ? "No reminder" : "Reminder: " + ReminderDate.Value.ToString("d MMM yyyy");

        /// <summary>
        /// Statues for display.
        /// </summary>
        public string StatusText => IsCompleted ? "Completed" : "Pending";

        /// <summary>
        /// This is true if the task isn't completed and its reminder date is today or has already passed, so the user is reminded about it. 
        /// </summary>
        public bool IsDue => !IsCompleted && ReminderDate != null && ReminderDate.Value.Date <= DateTime.Today;
    }
}
