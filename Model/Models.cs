using System;
using System.Collections.Generic;

namespace PlannerApi.Models
{
    // Основные сущности
    public class Resource
    {
        public Guid uid { get; set; }
        public string name { get; set; } = "";
        public DateTime time_ins { get; set; }
        public Guid company_id { get; set; }
    }

    public class TaskItem // Переименован из Task, чтобы избежать конфликта с System.Threading.Tasks.Task
    {
        public Guid uid { get; set; }
        public string name { get; set; } = "";
        public DateTime time_ins { get; set; }
        public DateTime time_pref_start { get; set; }
        public DateTime time_pref_finish { get; set; }
        public int duration { get; set; }
        public Guid company_id { get; set; }
    }

    public class WorkShift // Переименован из Shift, чтобы избежать конфликта
    {
        public Guid uid { get; set; }
        public string name { get; set; } = "";
        public DateTime time_ins { get; set; }
        public Guid resource_id { get; set; }
        public DateTime time_start { get; set; }
        public DateTime time_finish { get; set; }
        public DateTime? time_free { get; set; }
    }

    public class ShiftTask
    {
        public Guid shift_id { get; set; }
        public string shift_name { get; set; } = "";
        public Guid task_id { get; set; }
        public string task_name { get; set; } = "";
        public DateTime time_ins { get; set; }
        public DateTime? time_sched_start { get; set; }
        public DateTime? time_sched_finish { get; set; }
        public int? idle_dur { get; set; }
    }

    // Модели для дашборда
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

    public class DashboardReport
    {
        public DateTime GeneratedAt { get; set; }
        public int TotalTasks { get; set; }
        public int ActiveTasks { get; set; }
        public int CompletedTasks { get; set; }
        public int TotalResources { get; set; }
        public int ActiveResources { get; set; }
        public float ProductivityScore { get; set; }
        public float ResourcesUtilization { get; set; }
        public float TaskCompletionRate { get; set; }
        public string PeakActivityTime { get; set; } = "";
        public string MostProductiveResource { get; set; } = "";
        public Dictionary<string, int> TasksByStatus { get; set; } = new();
    }

    public class DailyTaskData
    {
        public int Count { get; set; }
        public int Completed { get; set; }
    }

    public class AnalyticsData
    {
        public Dictionary<string, DailyTaskData> DailyTasks { get; set; } = new();
        public Dictionary<string, int> TaskStatusDistribution { get; set; } = new();
        public Dictionary<string, float> ResourcePerformance { get; set; } = new();
        public Dictionary<int, int> PeakHours { get; set; } = new();
        public float TaskCompletionRate { get; set; }
    }

    public class Activity
    {
        public string Id { get; set; } = "";
        public string Type { get; set; } = "";
        public string Description { get; set; } = "";
        public DateTime Timestamp { get; set; }
        public string User { get; set; } = "";
    }

    public class Notification
    {
        public string Id { get; set; } = "";
        public string Type { get; set; } = "";
        public string Message { get; set; } = "";
        public DateTime Timestamp { get; set; }
        public bool IsRead { get; set; }
    }
}