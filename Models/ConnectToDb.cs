using Dapper;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PlannerApi.Models
{
    public class ConnectToDb : IDisposable
    {
        private readonly string connectionString = "Server=localhost;Port=5432;Database=Planner;User Id=postgres;Password=1111;";
        private NpgsqlConnection _connection;

        public static int progress = 0;
        public static int asincerrors = 0;
        public static bool stop = false;

        private NpgsqlConnection GetConnection()
        {
            if (_connection == null)
            {
                _connection = new NpgsqlConnection(connectionString);
            }
            return _connection;
        }

        public IEnumerable<resource> GetResources()
        {
            return GetConnection().Query<resource>("SELECT * FROM resource ORDER BY time_ins DESC");
        }

        public IEnumerable<task> GetTasks()
        {
            return GetConnection().Query<task>("SELECT * FROM task ORDER BY time_ins DESC");
        }

        public IEnumerable<shift> GetShifts()
        {
            return GetConnection().Query<shift>("SELECT * FROM shift ORDER BY time_ins DESC");
        }

        public IEnumerable<shift_task> GetShiftTasks()
        {
            return GetConnection().Query<shift_task>("SELECT * FROM shift_task ORDER BY time_ins DESC");
        }

        public bool PutResource(resource res)
        {
            var sql = @"INSERT INTO resource (uid, name, time_ins, company_id) 
                       VALUES (@uid, @name, @time_ins, @company_id)";
            return GetConnection().Execute(sql, res) > 0;
        }

        public bool PutTask(task task)
        {
            var sql = @"INSERT INTO task (uid, name, time_ins, time_pref_start, time_pref_finish, duration, company_id) 
                       VALUES (@uid, @name, @time_ins, @time_pref_start, @time_pref_finish, @duration, @company_id)";
            return GetConnection().Execute(sql, task) > 0;
        }

        public bool PutShift(shift shift)
        {
            var sql = @"INSERT INTO shift (uid, name, time_ins, resource_id, time_start, time_finish, time_free) 
                       VALUES (@uid, @name, @time_ins, @resource_id, @time_start, @time_finish, @time_free)";
            return GetConnection().Execute(sql, shift) > 0;
        }

        public bool ClearResources()
        {
            return GetConnection().Execute("DELETE FROM resource") > 0;
        }

        public bool ClearTasks()
        {
            return GetConnection().Execute("DELETE FROM task") > 0;
        }

        public bool ClearShifts()
        {
            return GetConnection().Execute("DELETE FROM shift") > 0;
        }

        public bool ClearShiftTasks()
        {
            return GetConnection().Execute("DELETE FROM shift_task") > 0;
        }

        public async Task StartScheduler(bool sinc = false)
        {
            await Task.Run(() =>
            {
                progress = 0;
                asincerrors = 0;
                stop = false;

                var tasks = GetTasks().ToList();
                var shifts = GetShifts().ToList();
                var shiftTasks = new List<shift_task>();

                for (int i = 0; i < tasks.Count; i++)
                {
                    if (stop) break;

                    var taskItem = tasks[i];
                    var availableShifts = shifts.Where(s =>
                        s.time_start <= taskItem.time_pref_finish &&
                        s.time_finish >= taskItem.time_pref_start).ToList();

                    foreach (var shift in availableShifts)
                    {
                        if (stop) break;

                        var st = new shift_task
                        {
                            shift_id = shift.uid,
                            shift_name = shift.name,
                            task_id = taskItem.uid,
                            task_name = taskItem.name,
                            time_ins = DateTime.UtcNow,
                            time_sched_start = shift.time_start > taskItem.time_pref_start ? shift.time_start : taskItem.time_pref_start,
                            time_sched_finish = shift.time_finish < taskItem.time_pref_finish ? shift.time_finish : taskItem.time_pref_finish,
                            idle_dur = 0
                        };

                        shiftTasks.Add(st);
                        progress = (int)((i + 1) / (double)tasks.Count * 100);
                        Thread.Sleep(100);
                    }
                }

                using (var connection = new NpgsqlConnection(connectionString))
                {
                    connection.Execute("DELETE FROM shift_task");
                    foreach (var st in shiftTasks)
                    {
                        connection.Execute(@"INSERT INTO shift_task (shift_id, shift_name, task_id, task_name, time_ins, time_sched_start, time_sched_finish, idle_dur) 
                                           VALUES (@shift_id, @shift_name, @task_id, @task_name, @time_ins, @time_sched_start, @time_sched_finish, @idle_dur)", st);
                    }
                }

                progress = 100;
            });
        }

        public void Dispose()
        {
            _connection?.Dispose();
        }
    }
}