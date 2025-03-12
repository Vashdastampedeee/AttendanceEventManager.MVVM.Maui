using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Views;
using EventManager.Models;
using EventManager.Utilities;
using EventManager.ViewModels.Popups;
using EventManager.Views.Popups;
using Microsoft.Data.SqlClient;
using Mopups.Services;
using SQLite;

namespace EventManager.Services
{
    public class SqlServerService
    {
        private readonly SQLiteAsyncConnection database;
        private readonly DatabaseService databaseService;
        private readonly string sqlServerConnStr = "Server=192.168.4.11,1433;Database=TimeKeeping;User Id=WMS;Password=WMS@dmin;TrustServerCertificate=True;";
        public SqlServerService(DatabaseService databaseService) 
        {
            database = databaseService.GetDatabaseConnection();
            this.databaseService = databaseService;
        }

        public async Task SyncEmployeesFromSQLServer()
        {
            async Task ExecuteSync()
            {
                var syncDataViewModel = new SyncDataViewModel();
                var syncData = new SyncData(syncDataViewModel);

                await MopupService.Instance.PushAsync(syncData);

                try
                {
                    using (var sqlConn = new SqlConnection(sqlServerConnStr))
                    {
                        await sqlConn.OpenAsync();
                        await syncDataViewModel.UpdateProgress("Connected to server...");

                        await databaseService.DeleteAllEmployeesData();
                        await syncDataViewModel.UpdateProgress("Clearing old employee data...");

                        int totalEmployees = await GetTotalEmployees(sqlConn);
                        await syncDataViewModel.UpdateProgress($"Total employees to fetch: {totalEmployees}...");

                        using (var sqlCmd = new SqlCommand(GetEmployeeQuery(), sqlConn))
                        using (var reader = await sqlCmd.ExecuteReaderAsync())
                        {
                            var employees = new List<Employee>();
                            int count = 0;

                            while (await reader.ReadAsync())
                            {
                                employees.Add(new Employee
                                {
                                    IdNumber = reader["IDNo"].ToString(),
                                    Name = reader["EmpName"].ToString(),
                                    BusinessUnit = reader["BusinessUnit"].ToString(),
                                    IdPhoto = reader["Photo"] as byte[]
                                });

                                count++;
                                await syncDataViewModel.UpdateProgress($"Fetching {count}/{totalEmployees} employees...");
                            }

                            await database.RunInTransactionAsync(tran =>
                            {
                                tran.InsertAll(employees);
                            });

                            await syncDataViewModel.UpdateProgress("Saving data to local database...");
                        }
                    }

                    await syncDataViewModel.UpdateProgress("Sync completed successfully!");
                    await ToastHelper.ShowToast($"SQL Server sync succesful!", ToastDuration.Long);
                }
                catch (Exception ex)
                {
                    await syncDataViewModel.UpdateProgress($"Sql server connection failed!");
                    await ToastHelper.ShowToast($"SQL Server sync failed!", ToastDuration.Long);
                }
                finally
                {
                    await Task.Delay(500);
                    await MopupService.Instance.PopAsync();
                }
            }

            await ExecuteSync();

        }

        private async Task<int> GetTotalEmployees(SqlConnection sqlConn)
        {
            using (var countCmd = new SqlCommand(
                "SELECT COUNT(*) FROM TimeKeeping..Employees WHERE JobStatus = 'ACTIVE' OR JobStatusCurrent = 'ACTIVE'", sqlConn))
            {
                return (int)await countCmd.ExecuteScalarAsync();
            }
        }

        private string GetEmployeeQuery()
        {
            return @"SELECT 
                IDNo, 
                EmpName = CONCAT(LastName, ', ', FirstName, 
                    CASE WHEN TRIM(MiddleName) = '' OR MiddleName IS NULL THEN '' ELSE ' ' END, 
                    SUBSTRING(TRIM(MiddleName), 1, 1), 
                    CASE WHEN TRIM(MiddleName) = '' OR MiddleName IS NULL THEN '' ELSE '.' END), 
                BusinessUnit, 
                Photo 
            FROM TimeKeeping..Employees 
            WHERE JobStatus = 'ACTIVE' OR JobStatusCurrent = 'ACTIVE'";
        }
    }

}

