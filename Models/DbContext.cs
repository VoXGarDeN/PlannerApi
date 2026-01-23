using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PlannerApi.Models
{
    public class DbContext
    {
        private readonly ConnectToDb _db;

        public DbContext()
        {
            _db = new ConnectToDb();
        }

        public async Task<List<task>> GetTasksAsync()
        {
            return await Task.FromResult(_db.GetTasks().ToList());
        }

        public async Task<List<resource>> GetResourcesAsync()
        {
            return await Task.FromResult(_db.GetResources().ToList());
        }

        public async Task<List<shift>> GetShiftsAsync()
        {
            return await Task.FromResult(_db.GetShifts().ToList());
        }

        public async Task<List<shift_task>> GetShiftTasksAsync()
        {
            return await Task.FromResult(_db.GetShiftTasks().ToList());
        }
    }

    public class DashboardStats
    {
        public int TotalTasks { get; set; }
        public int ActiveTasks { get; set; }
        public int CompletedTasks { get; set; }
        public int TotalResources { get; set; }
        public int ActiveResources { get; set; }
        public int TotalShifts { get; set; }
        public int ActiveShifts { get; set; }
        public int ScheduledTasks { get; set; }
        public float ProductivityScore { get; set; }
        public int TasksThisWeek { get; set; }
        public float ResourcesUtilization { get; set; }
    }

    public class AnalyticsData
    {
        public Dictionary<string, DailyTaskData> DailyTasks { get; set; } = new();
        public Dictionary<string, int> TaskStatusDistribution { get; set; } = new();
        public Dictionary<string, float> ResourcePerformance { get; set; } = new();
        public Dictionary<int, int> PeakHours { get; set; } = new();
        public float TaskCompletionRate { get; set; }
    }

    public class DailyTaskData
    {
        public int Count { get; set; }
        public int Completed { get; set; }
    }

    public class Activity
    {
        public string Id { get; set; } = "";
        public string Type { get; set; } = "";
        public string Description { get; set; } = "";
        public DateTime Timestamp { get; set; }
        public string User { get; set; } = "";
    }

    public class DashboardManager
    {
        private readonly DbContext _dbContext;

        public DashboardManager(DbContext dbContext)
        {
            _dbContext = dbContext;
        }

        // Методы для работы с дашбордом
    }
}