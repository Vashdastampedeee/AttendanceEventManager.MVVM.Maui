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

        public static string GetDatabasePath()
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

        public async Task DeleteAllEventsData()
        {
            await databaseConnection.DeleteAllAsync<Event>();
            await databaseConnection.ExecuteAsync("DELETE FROM sqlite_sequence WHERE name = 'event'");
        }

        public async Task<Employee?> GetEmployeeIdNumber(string idNumber)
        {
            var query = "SELECT * FROM employee WHERE IdNumber = ?";
            var employees = await databaseConnection.QueryAsync<Employee>(query, idNumber);
            Debug.WriteLine($"[DatabaseService] ID Number {idNumber}");
            return employees.FirstOrDefault();
        }

        public async Task<bool> IsEmployeeAlreadyScanned(string idNumber, string eventName, string eventDate, string eventTime)
        {
            string query = "SELECT COUNT(*) FROM attendancelog WHERE IdNumber = ? AND Status = 'SUCCESS' AND EventName = ? AND EventDate = ? AND EventTime = ?";
            var result = await databaseConnection.ExecuteScalarAsync<int>(query, idNumber, eventName, eventDate, eventTime);
            return result > 0;
        }

        public async Task<bool> IsNotFoundLogAlreadyScanned(string idNumber, string eventName, string eventDate, string eventTime)
        {
            string query = "SELECT COUNT(*) FROM attendancelog WHERE IdNumber = ? AND Status = 'NOT FOUND' AND EventName = ? AND EventDate = ? AND EventTime = ?";
            int count = await databaseConnection.ExecuteScalarAsync<int>(query, idNumber, eventName, eventDate, eventTime);
            return count > 0;
        }

        public async Task InsertAttendanceLog(string idNumber, string name, string businessUnit, string status, string eventName, string eventCategory, string eventDate, string eventTime)
        {
            string timeStamp = DateTime.Now.ToString("MM/dd/yyyy h:mm:ss tt");
            string query = "INSERT INTO attendancelog (IdNumber, Name, BusinessUnit, Status, EventName, EventCategory, EventDate, EventTime, Timestamp) VALUES (?,?,?,?,?,?,?,?,?)";
            await databaseConnection.ExecuteAsync(query, idNumber, name, businessUnit, status, eventName, eventCategory, eventDate, eventTime, timeStamp);
            Debug.WriteLine($"[DatabaseService] Attendance log inserted: {idNumber}, {name}, {businessUnit}, {status}, {eventName}, {eventCategory}, {eventDate}, {eventTime} ,{timeStamp}");
        }

        public async Task<List<Event>> GetEventsPaginated(int startIndex, int pageSize)
        {
            string query = "SELECT * FROM event ORDER BY EventDate DESC LIMIT ? OFFSET ?";
            var events = await databaseConnection.QueryAsync<Event>(query, pageSize, startIndex);

            foreach (var eventData in events)
            {
                eventData.IsDefaultVisible = eventData.isSelected;
            }

            return events;
        }

        public async Task UseLatestEventByDefault()
        {
            await databaseConnection.ExecuteAsync("UPDATE event SET isSelected = 0");
            await databaseConnection.ExecuteAsync("UPDATE event SET isSelected = 1 WHERE Id = (SELECT Id FROM event ORDER BY EventDate DESC LIMIT 1)");
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

        public async Task<List<AttendanceLog>> GetAttendanceLogsPaginated(int startIndex, int pageSize)
        {
            string query = "SELECT * FROM attendancelog ORDER BY Timestamp DESC LIMIT ? OFFSET ?";
            return await databaseConnection.QueryAsync<AttendanceLog>(query, pageSize, startIndex);
        }

        public async Task<List<AttendanceLog>> SearchLogs(string searchText, int startIndex, int pageSize)
        {
            string query = "SELECT * FROM attendancelog WHERE IdNumber LIKE ? OR Name LIKE ? OR BusinessUnit LIKE ? OR Status LIKE ? OR EventName LIKE ? OR EventCategory LIKE ? OR EventDate LIKE ? OR EventTime LIKE ? ORDER BY Timestamp DESC LIMIT ? OFFSET ?";

            string searchPattern = $"%{searchText}%";

            return await databaseConnection.QueryAsync<AttendanceLog>(query, searchPattern, searchPattern, searchPattern, searchPattern, searchPattern, searchPattern, searchPattern, searchPattern, pageSize, startIndex);
        }

        public async Task<int> GetTotalEmployeeCountAsync() 
        {
            string query = "SELECT COUNT(*) FROM employee";
            return await databaseConnection.ExecuteScalarAsync<int>(query);
        }

        public async Task<int> GetTotalEmployeeCountByBUAsync(string businessUnit)
        {
            string query = "SELECT COUNT(*) FROM employee WHERE BusinessUnit = ?";
            return await databaseConnection.ExecuteScalarAsync<int>(query, businessUnit);
        }

        public async Task<int> GetPresentEmployeeCountForActiveEvent()
        {
            string query = "SELECT COUNT(DISTINCT Id) FROM attendancelog WHERE Status = 'SUCCESS' AND EventName = (SELECT EventName FROM event WHERE isSelected = 1)";
            return await databaseConnection.ExecuteScalarAsync<int>(query);
        }

        public async Task<int> GetPresentEmployeeCountByBUAsync(string businessUnit)
        {
            string query = "SELECT COUNT(DISTINCT Id) FROM attendancelog WHERE Status = 'SUCCESS' AND EventName = (SELECT EventName FROM event WHERE isSelected = 1) AND BusinessUnit = ?";
            return await databaseConnection.ExecuteScalarAsync<int>(query, businessUnit);
        }

        public async Task<List<EmployeeAttendanceStatus>> GetTotalScannedDataPaginated(int lastLoadedIndex, int pageSize)
        {
            string query = @"
                SELECT e.IdNumber, e.Name, e.BusinessUnit, 
                       CASE WHEN a.IdNumber IS NOT NULL THEN 'Present' ELSE 'Absent' END AS Status 
                FROM employee e 
                LEFT JOIN attendancelog a 
                    ON e.IdNumber = a.IdNumber 
                    AND a.EventName = (SELECT EventName FROM event WHERE isSelected = 1) 
                ORDER BY CASE WHEN a.IdNumber IS NOT NULL THEN 0 ELSE 1 END, e.Name 
                LIMIT ? OFFSET ?";

            return await databaseConnection.QueryAsync<EmployeeAttendanceStatus>(query, pageSize, lastLoadedIndex);
        }

        public async Task<List<EmployeeAttendanceStatus>> GetTotalScannedDataByBusinessUnitPaginated(string businessUnit, int lastLoadedIndex, int pageSize)
        {
            string query = @"
                SELECT e.IdNumber, e.Name, e.BusinessUnit, 
                       CASE WHEN a.IdNumber IS NOT NULL THEN 'Present' ELSE 'Absent' END AS Status 
                FROM employee e 
                LEFT JOIN attendancelog a 
                    ON e.IdNumber = a.IdNumber 
                    AND a.EventName = (SELECT EventName FROM event WHERE isSelected = 1) 
                WHERE e.BusinessUnit = ? 
                ORDER BY CASE WHEN a.IdNumber IS NOT NULL THEN 0 ELSE 1 END, e.Name 
                LIMIT ? OFFSET ?";

            return await databaseConnection.QueryAsync<EmployeeAttendanceStatus>(query, businessUnit, pageSize, lastLoadedIndex);
        }

        public async Task<List<EmployeeAttendanceStatus>> SearchTotalScannedData(string searchText, int lastLoadedIndex, int pageSize)
        {
            string query = @"
                SELECT e.IdNumber, e.Name, e.BusinessUnit, 
                       CASE WHEN a.IdNumber IS NOT NULL THEN 'Present' ELSE 'Absent' END AS Status 
                FROM employee e 
                LEFT JOIN attendancelog a 
                    ON e.IdNumber = a.IdNumber 
                    AND a.EventName = (SELECT EventName FROM event WHERE isSelected = 1)
                WHERE e.IdNumber LIKE ? OR e.Name LIKE ? OR e.BusinessUnit LIKE ? OR 
                      (CASE WHEN a.IdNumber IS NOT NULL THEN 'Present' ELSE 'Absent' END) LIKE ?
                ORDER BY CASE WHEN a.IdNumber IS NOT NULL THEN 0 ELSE 1 END, e.Name 
                LIMIT ? OFFSET ?";

            string searchPattern = $"%{searchText}%";

            return await databaseConnection.QueryAsync<EmployeeAttendanceStatus>(query, searchPattern, searchPattern, searchPattern, searchPattern, pageSize, lastLoadedIndex);
        }

        public async Task<List<EmployeeAttendanceStatus>> SearchTotalScannedDataByBusinessUnit(string businessUnit, string searchText, int lastLoadedIndex, int pageSize)
        {
            string query = @"
                SELECT e.IdNumber, e.Name, e.BusinessUnit, 
                       CASE WHEN a.IdNumber IS NOT NULL THEN 'Present' ELSE 'Absent' END AS Status 
                FROM employee e 
                LEFT JOIN attendancelog a 
                    ON e.IdNumber = a.IdNumber 
                    AND a.EventName = (SELECT EventName FROM event WHERE isSelected = 1)
                WHERE e.BusinessUnit = ? 
                AND (e.IdNumber LIKE ? OR e.Name LIKE ? OR 
                     (CASE WHEN a.IdNumber IS NOT NULL THEN 'Present' ELSE 'Absent' END) LIKE ?)
                ORDER BY CASE WHEN a.IdNumber IS NOT NULL THEN 0 ELSE 1 END, e.Name 
                LIMIT ? OFFSET ?";

            string searchPattern = $"%{searchText}%";

            return await databaseConnection.QueryAsync<EmployeeAttendanceStatus>(query, businessUnit, searchPattern, searchPattern, searchPattern, pageSize, lastLoadedIndex);
        }

    }
}
