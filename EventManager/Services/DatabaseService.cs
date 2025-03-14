using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using EventManager.Models;
using SQLite;
using Microsoft.Maui.Storage;

namespace EventManager.Services
{
    public class DatabaseService
    {
        private readonly SQLiteAsyncConnection databaseConnection;
        private const string databaseFileName = "dbtest.db";

        public DatabaseService()
        {
            string dbPath = GetDatabasePath();
            databaseConnection = new SQLiteAsyncConnection(dbPath);
            Debug.WriteLine($"[DatabaseService] Database initialized at: {dbPath}");
        }

        public async Task InitializeTablesAsync()
        {
            if (!Preferences.Get("DatabaseInitialize", false))
            {
                Debug.WriteLine("[DatabaseService] Creating tables");
                await databaseConnection.CreateTableAsync<AttendanceLog>();
                await databaseConnection.CreateTableAsync<Event>();
                Preferences.Set("DatabaseInitialize", true);
            }
            else
            {
                Debug.WriteLine("[DatabaseService] Tables already created");
            }
        }

        public SQLiteAsyncConnection GetDatabaseConnection()
        {
            return databaseConnection;
        }

        public string GetDatabasePath()
        {
            string folderPath = FileSystem.AppDataDirectory;
            string dbPath = Path.Combine(folderPath, databaseFileName);

            Debug.WriteLine($"[DatabaseService] Checking if database exists at: {dbPath}");

            if (!File.Exists(dbPath))
            {
                Debug.WriteLine("[DatabaseService] Database not found. Copying from embedded resources...");

                var assembly = Assembly.GetExecutingAssembly();
                string resourceName = "EventManager.Resources.Raw.dbtest.db";

                using (Stream resourceStream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (resourceStream == null)
                    {
                        Debug.WriteLine($"[DatabaseService] ERROR: Embedded database '{resourceName}' not found.");
                        throw new FileNotFoundException($"Embedded resource '{resourceName}' not found.");
                    }

                    using (FileStream fileStream = new FileStream(dbPath, FileMode.Create, FileAccess.Write))
                    {
                        resourceStream.CopyTo(fileStream);
                        Debug.WriteLine("[DatabaseService] Database copied successfully.");
                    }
                }
            }
            else
            {
                Debug.WriteLine("[DatabaseService] Database already exists. No need to copy.");
            }

            return dbPath;
        }

        public async Task DeleteAllEmployeesData()
        {
            await databaseConnection.DeleteAllAsync<Employee>();
            await databaseConnection.ExecuteAsync("DELETE FROM sqlite_sequence WHERE name='employee'");
            Debug.WriteLine("[DatabaseService] All employee records deleted.");
        }

        public async Task<Employee?> GetEmployeeIdNumber(string idNumber)
        {
            var query = "SELECT * FROM employee WHERE IdNumber = ?";
            var employees = await databaseConnection.QueryAsync<Employee>(query, idNumber);
            Debug.WriteLine($"[DatabaseService] ID Number {idNumber}");
            return employees.FirstOrDefault();
        }

        public async Task<bool> IsEmployeeAlreadyScanned(string idNumber)
        {
            string query = "SELECT COUNT(*) FROM attendancelog WHERE IdNumber = ?";
            var result = await databaseConnection.ExecuteScalarAsync<int>(query, idNumber);
            return result > 0;
        }

        public async Task InsertAttendanceLog(string idNumber, string name, string businessUnit, string status, string eventName, string eventCategory, string eventDate, string eventTime)
        {
            string timeStamp = DateTime.Now.ToString("MM/dd/yyyy h:mm:ss tt");
            string query = "INSERT INTO attendancelog (IdNumber, Name, BusinessUnit, Status, EventName, EventCategory, EventDate, EventTime, Timestamp) VALUES (?,?,?,?,?,?,?,?,?)";
            await databaseConnection.ExecuteAsync(query, idNumber, name, businessUnit, status, eventName, eventCategory, eventDate, eventTime, timeStamp);
            Debug.WriteLine($"[DatabaseService] Attendance log inserted: {idNumber}, {name}, {businessUnit}, {status}, {eventName}, {eventCategory}, {eventDate}, {eventTime} ,{timeStamp}");
        }

        public async Task<List<AttendanceLog>> GetAttendanceLogsPaginated(string eventName, string eventCategory, string eventDate, string eventTime, int startIndex, int pageSize)
        {
            string query = "SELECT * FROM attendancelog WHERE EventName = ? AND EventCategory = ? AND EventDate = ? AND EventTime = ? ORDER BY Timestamp DESC LIMIT ? OFFSET ?";
            return await databaseConnection.QueryAsync<AttendanceLog>(query, eventName, eventCategory, eventDate, eventTime, pageSize, startIndex);
        }

        public async Task InsertEvent(string eventName, string eventCategory, byte[] eventImage, string eventDate, string eventFromTime, string eventToTime)
        {
            string query = "INSERT INTO event (EventName, EventCategory, EventImage, EventDate, EventFromTime, EventToTime, isSelected) VALUES (?, ?, ?, ?, ?, ?, ?)";
            await databaseConnection.ExecuteAsync(query, eventName, eventCategory, eventImage, eventDate, eventFromTime, eventToTime, false);
            Debug.WriteLine($"[DatabaseService] Event inserted: {eventName}, {eventCategory}, {eventImage?.Length ?? 0} {eventDate}, {eventFromTime} - {eventToTime}");
        }

        public async Task<List<Event>> GetEventsPaginated(int startIndex, int pageSize)
        {
            string query = "SELECT * FROM event ORDER BY Id DESC LIMIT ? OFFSET ?";
            var events = await databaseConnection.QueryAsync<Event>(query, pageSize, startIndex);

            foreach (var eventData in events)
            {
                eventData.IsDefaultVisible = eventData.isSelected;
            }

            return events;
        }

        public async Task DeleteSelectedEvent(int eventId)
        {
            await databaseConnection.ExecuteAsync("DELETE FROM event WHERE Id = ?", eventId);
            Debug.WriteLine($"[DatabaseService] Delete event: {eventId}");
        }
        public async Task UpdateSelectedEvent(int eventId, string eventName, string eventCategory, byte[] eventImage, string eventDate, string eventFromTime, string eventToTime)
        {
            string query = "UPDATE event SET EventName = ?, EventCategory = ?, EventImage = ?, EventDate = ?, EventFromTime = ?, EventToTime = ? WHERE Id = ?";
            await databaseConnection.ExecuteAsync(query, eventName, eventCategory, eventImage, eventDate, eventFromTime, eventToTime, eventId);
            Debug.WriteLine($"[DatabaseService] Event Updated: {eventName}, {eventCategory}, {eventImage?.Length ?? 0}, {eventDate}, {eventFromTime}, {eventToTime} Where Id = {eventId}");
        }
        public async Task<Event?> GetEventById(int eventId)
        {
            string query = "SELECT * FROM event WHERE Id = ?";
            var eventList = await databaseConnection.QueryAsync<Event>(query, eventId);
            return eventList.FirstOrDefault();
        }
        public async Task UseSelectedEvent(int eventId)
        {
            await databaseConnection.ExecuteAsync("UPDATE event SET isSelected = 0");
            await databaseConnection.ExecuteAsync("UPDATE event SET isSelected = 1 WHERE Id =?", eventId);
        }
        public async Task<Event?> GetSelectedEvent()
        {
            string query = "SELECT * FROM event WHERE isSelected = 1";
            var eventList = await databaseConnection.QueryAsync<Event>(query);
            return eventList.FirstOrDefault();
        }
        public async Task<List<Event>> SetFilterEvents(string category, bool sortByOrder, int startIndex, int pageSize)
        {
            string query = "SELECT * FROM event";
            List<object> parameters = new List<object>();

            if (!string.IsNullOrEmpty(category) && category != "ALL")
            {
                query += " WHERE EventCategory = ?";
                parameters.Add(category);
            }

            query += sortByOrder ? " ORDER BY Id DESC" : " ORDER BY Id ASC";
            query += " LIMIT ? OFFSET ?";

            parameters.Add(pageSize);
            parameters.Add(startIndex);

            return await databaseConnection.QueryAsync<Event>(query, parameters.ToArray());
        }

        public async Task<List<Event>> SearchEvents(string searchText, int startIndex, int pageSize)
        {
            string query = "SELECT * FROM event WHERE EventName LIKE ? OR EventCategory LIKE ? OR EventDate LIKE ? OR EventFromTime LIKE ? OR EventToTime LIKE ? ORDER BY Id DESC LIMIT ? OFFSET ?";

            string searchPattern = $"%{searchText}%";

            return await databaseConnection.QueryAsync<Event>(query, searchPattern, searchPattern, searchPattern, searchPattern, searchPattern, pageSize, startIndex);
        }
        public async Task<List<string>> GetDistinctLogValues(string columnName)
        {
            string query = $"SELECT DISTINCT {columnName} FROM attendancelog ORDER BY {columnName} ASC";
            var results = await databaseConnection.QueryScalarsAsync<string>(query);
            return results.Where(value => !string.IsNullOrEmpty(value)).ToList();
        }

        public async Task<List<string>> GetFilteredCategories(string eventName)
        {
            string query = "SELECT DISTINCT EventCategory FROM attendancelog WHERE EventName = ? ORDER BY EventCategory ASC";
            var result = await databaseConnection.QueryScalarsAsync<string>(query, eventName);
            return result.Where(value => !string.IsNullOrEmpty(value)).ToList();
        }

        public async Task<List<string>> GetFilteredDates(string eventName, string eventCategory)
        {
            string query = "SELECT DISTINCT EventDate FROM attendancelog WHERE EventName = ? AND EventCategory = ? ORDER BY EventDate ASC";
            var result = await databaseConnection.QueryScalarsAsync<string>(query, eventName, eventCategory);
            return result.Where(value => !string.IsNullOrEmpty(value)).ToList();
        }

        public async Task<List<string>> GetFilteredTimes(string eventName, string eventCategory, string eventDate)
        {
            string query = "SELECT DISTINCT EventTime FROM attendancelog WHERE EventName = ? AND EventCategory = ? AND EventDate = ? ORDER BY EventTime ASC";
            var result = await databaseConnection.QueryScalarsAsync<string>(query, eventName, eventCategory, eventDate);
            return result.Where(value => !string.IsNullOrEmpty(value)).ToList();
        }

        public async Task<List<AttendanceLog>> GetFilteredLogs(LogFilter filter, int startIndex, int pageSize)
        {
            string query = "SELECT * FROM attendancelog WHERE 1=1";
            List<object> parameters = new List<object>();

            if (!string.IsNullOrEmpty(filter.Name))
            {
                query += " AND EventName = ?";
                parameters.Add(filter.Name);
            }
            if (!string.IsNullOrEmpty(filter.Category))
            {
                query += " AND EventCategory = ?";
                parameters.Add(filter.Category);
            }
            if (!string.IsNullOrEmpty(filter.Date))
            {
                query += " AND EventDate = ?";
                parameters.Add(filter.Date);
            }
            if (!string.IsNullOrEmpty(filter.Time))
            {
                query += " AND EventTime = ?";
                parameters.Add(filter.Time);
            }

            query += " ORDER BY Timestamp DESC LIMIT ? OFFSET ?";
            parameters.Add(pageSize);
            parameters.Add(startIndex);

            return await databaseConnection.QueryAsync<AttendanceLog>(query, parameters.ToArray());
        }

        public async Task<List<AttendanceLog>> GetFilteredLogsForExport(LogFilter filter)
        {
            string query = "SELECT * FROM attendancelog WHERE 1=1";
            List<object> parameters = new List<object>();

            if (!string.IsNullOrEmpty(filter.Name))
            {
                query += " AND EventName = ?";
                parameters.Add(filter.Name);
            }
            if (!string.IsNullOrEmpty(filter.Category))
            {
                query += " AND EventCategory = ?";
                parameters.Add(filter.Category);
            }
            if (!string.IsNullOrEmpty(filter.Date))
            {
                query += " AND EventDate = ?";
                parameters.Add(filter.Date);
            }
            if (!string.IsNullOrEmpty(filter.Time))
            {
                query += " AND EventTime = ?";
                parameters.Add(filter.Time);
            }

            query += " ORDER BY Timestamp DESC";

            return await databaseConnection.QueryAsync<AttendanceLog>(query, parameters.ToArray());
        }

    }
}
