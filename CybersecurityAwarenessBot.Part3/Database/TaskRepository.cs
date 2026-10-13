using CybersecurityAwarenessBot.Features;
using MySqlConnector;

namespace CybersecurityAwarenessBot.Database
{
    /// <summary>
    /// This class reads and changes the user's tasks in the MySQL database: Create (AddAsync), Read (GetAllAsync, GetDueRemindersAsync), Update (MarkCompletedAsync,
    /// SetReminderAsync) and Delete (DeleteAsync). The query uses parameters, to ensure that what the user types is never treated as part of the SQL
    /// </summary>
    internal class TaskRepository
    {
        /// <summary>
        /// Longest title the task table can hold is 100
        /// </summary>
        public const int MaxTitleLength = 100;

        /// <summary>
        /// This is the longest description the tasks table can hold.
        /// </summary>
        public const int MaxDescriptionLength = 500;

        /// <summary>
        /// Columns every SELECT reads, in the order ReadTask expects them. 
        /// </summary>
        private const string Columns = "id, title, description, reminder_date, is_completed, created_at";

        private readonly ActivityLog _log;

        /// <summary>
        /// Raised after a task is added, completed, changed or deleted, so the Tasks tab can refresh itself, whether the change came from the tab or the chat.
        /// </summary>
        public event Action? TasksChanged;

        /// <summary>
        /// The creates the repository with the activity log it should record task action in.
        /// </summary>
        /// <param name="log">The shared activity log</param>
        public TaskRepository(ActivityLog log) 
        {
            _log = log; 
        }

        /// <summary>
        /// This reads every task. First the pending tasks, then the completed ones. From newest first withineach group.
        /// </summary>
        /// <returns>All user's tasks</returns>
        public async Task<List<TaskItem>> GetAllAsync() {
            string sql = $"SELECT {Columns} FROM tasks ORDER BY is_completed, created_at DESC, id DESC";
            return await ReadTasksAsync(sql, null);
        }

        /// <summary>
        /// This reads the pending tasks whose reminder date is today or has already passed.
        /// </summary>
        /// <returns> Tasks the user should be reminded about, earliest reminder first</returns>
        public async Task<List<TaskItem>> GetDueRemindersAsync() 
        {
            string sql = $"SELECT {Columns} FROM tasks WHERE is_completed = FALSE AND reminder_date IS NOT NULL AND reminder_date <= @today ORDER BY reminder_date, id";
            return await ReadTasksAsync(sql, DateTime.Today);
        }

        /// <summary>
        /// This add a new task to the database and records it in the activity log.
        /// </summary>
        /// <param name="title">Task title</param>
        /// <param name="description">Details about task</param>
        /// <param name="reminderDate">Date to be reminded on or null for no reminder</param>
        /// <returns>Task as it was saved</returns>
        public async Task<TaskItem> AddAsync(string title, string description, DateTime? reminderDate) 
        {
            title = title.Trim();
            description = description.Trim();
            DateTime? reminder = reminderDate?.Date;

            ValidateTask(title, description , reminder);

            using MySqlConnection connection = DatabaseConnection.Create();
            await connection.OpenAsync();

            using MySqlCommand command = new("INSERT INTO tasks (title, description, reminder_date) VALUES (@title, @description, @reminder)", connection);

            command.Parameters.AddWithValue("@title", title);
            command.Parameters.AddWithValue("@description", description);
            command.Parameters.AddWithValue("@reminder", ToDatabaseValue(reminder));
            await command.ExecuteNonQueryAsync();

            TaskItem task = new((int)command.LastInsertedId, title, description, reminder, false, DateTime.Now);

            string reminderNote = reminder.HasValue ? $"reminder set for {FormatDate(reminder.Value)}" : "no reminder";
            _log.Add($"Task added: '{title}' ({reminderNote})");
            TasksChanged?.Invoke();

            return task;
        }


        /// <summary>
        /// Marks a task as completed.
        /// </summary>
        /// <param name="task">The task to complete</param>
        /// <returns>True if the task was found and updated, false if it no longer exists</returns>
        public async Task<bool> MarkCompletedAsync(TaskItem task)
        {
            bool updated = await ExecuteAsync("UPDATE tasks SET is_completed = TRUE WHERE id = @id", ("@id", task.Id));
            if (updated)
            {
                _log.Add($"Task completed: '{task.Title}'");
                TasksChanged?.Invoke();
            }
            return updated;
        }

        /// <summary>
        /// Sets, changes or removes a task's reminder date.
        /// </summary>
        /// <param name="task">The task to change</param>
        /// <param name="reminderDate">The new reminder date, or null to remove the reminder</param>
        /// <returns>True if the task was found and updated, false if it no longer exists</returns>
        /// <exception cref="ArgumentException">The reminder date is in the past</exception>
        public async Task<bool> SetReminderAsync(TaskItem task, DateTime? reminderDate)
        {
            DateTime? reminder = reminderDate?.Date;
            if (reminder < DateTime.Today)
            {
                throw new ArgumentException("The reminder date can't be in the past.");
            }

            bool updated = await ExecuteAsync("UPDATE tasks SET reminder_date = @reminder WHERE id = @id",
                ("@reminder", ToDatabaseValue(reminder)), ("@id", task.Id));
            if (updated)
            {
                _log.Add(reminder.HasValue
                    ? $"Reminder set: '{task.Title}' on {FormatDate(reminder.Value)}"
                    : $"Reminder removed: '{task.Title}'");
                TasksChanged?.Invoke();
            }
            return updated;
        }

        /// <summary>
        /// Deletes a task from the database.
        /// </summary>
        /// <param name="task">The task to delete</param>
        /// <returns>True if the task was found and deleted, false if it no longer exists</returns>
        public async Task<bool> DeleteAsync(TaskItem task)
        {
            bool deleted = await ExecuteAsync("DELETE FROM tasks WHERE id = @id", ("@id", task.Id));
            if (deleted)
            {
                _log.Add($"Task deleted: '{task.Title}'");
                TasksChanged?.Invoke();
            }
            return deleted;
        }

        /// <summary>
        /// Checks a new task before it is saved, so the user gets a clear message instead of a database error.
        /// </summary>
        private static void ValidateTask(string title, string description, DateTime? reminder)
        {
            if (title.Length == 0)
            {
                throw new ArgumentException("Please give the task a title.");
            }

            if (title.Length > MaxTitleLength)
            {
                throw new ArgumentException($"The title can be at most {MaxTitleLength} characters.");
            }

            if (description.Length > MaxDescriptionLength)
            {
                throw new ArgumentException($"The description can be at most {MaxDescriptionLength} characters.");
            }

            if (reminder < DateTime.Today)
            {
                throw new ArgumentException("The reminder date can't be in the past.");
            }
        }

        /// <summary>
        /// Runs an UPDATE or DELETE with the given parameters.
        /// </summary>
        /// <param name="sql">The statement to run</param>
        /// <param name="parameters">Each parameter's name and value</param>
        /// <returns>True if a row was changed</returns>
        private static async Task<bool> ExecuteAsync(string sql, params (string Name, object Value)[] parameters)
        {
            using MySqlConnection connection = DatabaseConnection.Create();
            await connection.OpenAsync();

            using MySqlCommand command = new(sql, connection);
            foreach (var parameter in parameters)
            {
                command.Parameters.AddWithValue(parameter.Name, parameter.Value);
            }

            int rowsChanged = await command.ExecuteNonQueryAsync();
            return rowsChanged > 0;
        }

        /// <summary>
        /// Runs a SELECT and turns every row into a TaskItem.
        /// </summary>
        /// <param name="sql">The query, which selects Columns and may use @today</param>
        /// <param name="today">The value for @today, if the query uses it</param>
        /// <returns>The tasks the query found</returns>
        private static async Task<List<TaskItem>> ReadTasksAsync(string sql, DateTime? today)
        {
            List<TaskItem> tasks = new();

            using MySqlConnection connection = DatabaseConnection.Create();
            await connection.OpenAsync();

            using MySqlCommand command = new(sql, connection);
            if (today.HasValue)
            {
                command.Parameters.AddWithValue("@today", today.Value);
            }

            using MySqlDataReader reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                // The numbers are the column positions in Columns: 0 id, 1 title, 2 description, 3 reminder_date, 4 is_completed, 5 created_at
                DateTime? reminder = reader.IsDBNull(3) ? null : reader.GetDateTime(3);
                tasks.Add(new TaskItem(reader.GetInt32(0), reader.GetString(1), reader.GetString(2), reminder, reader.GetBoolean(4), reader.GetDateTime(5)));
            }

            return tasks;
        }

        /// <summary>
        /// Converts an optional reminder date into a value for a query parameter.
        /// </summary>
        private static object ToDatabaseValue(DateTime? reminder)
        {
            return reminder.HasValue ? reminder.Value : DBNull.Value;
        }

        /// <summary>
        /// Formats a date the same way everywhere in the task assistant
        /// </summary>
        private static string FormatDate(DateTime date)
        {
            return date.ToString("d MMM yyyy");
        }


    }
}
