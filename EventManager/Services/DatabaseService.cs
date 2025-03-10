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
            await databaseConnection.ExecuteAsync("UPDATE sqlite_sequence SET seq = 0 WHERE name='employee'");
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

        public async Task InsertAttendanceLog(string idNumber, string name, string businessUnit, string status)
        {
            string timeStamp = DateTime.Now.ToString("MM/dd/yyyy h:mm:ss tt");
            string query = "INSERT INTO attendancelog (IdNumber, Name, BusinessUnit, Status, Timestamp) VALUES (?,?,?,?,?)";
            await databaseConnection.ExecuteAsync(query, idNumber, name, businessUnit, status, timeStamp);
            Debug.WriteLine($"[DatabaseService] Attendance log inserted: {idNumber}, {name}, {businessUnit}, {status} ,{timeStamp}");
        }

        public async Task<List<AttendanceLog>> GetAttendanceLogsPaginated(int startIndex, int pageSize)
        {
            string query = "SELECT * FROM attendancelog ORDER BY Timestamp DESC LIMIT ? OFFSET ?";
            return await databaseConnection.QueryAsync<AttendanceLog>(query, pageSize, startIndex);
        }

        public async Task InsertIntoEvent(string eventName, string eventCategory, byte[] eventImage, string eventDate, string eventFromTime, string eventToTime)
        {
            string query = "INSERT INTO event (EventName, EventCategory, EventImage, EventDate, EventFromTime, EventToTime, isSelected) VALUES (?, ?, ?, ?, ?, ?, ?)";
            await databaseConnection.ExecuteAsync(query, eventName, eventCategory, eventImage, eventDate, eventFromTime, eventToTime, false);
            Debug.WriteLine($"[DatabaseService] Event inserted: {eventName}, {eventCategory}, {eventImage?.Length ?? 0} {eventDate}, {eventFromTime} - {eventToTime}");
        }

        public async Task<List<Event>> GetEventsPaginated(int startIndex, int pageSize)
        {
            string query = "SELECT * FROM event ORDER BY Id DESC LIMIT ? OFFSET ?";
            return await databaseConnection.QueryAsync<Event>(query, pageSize, startIndex);
        }

    }
}
