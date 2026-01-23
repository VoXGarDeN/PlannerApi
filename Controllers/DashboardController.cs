using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PlannerApi.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly ILogger<DashboardController> _logger;
        private readonly IConfiguration _configuration;

        public DashboardController(ILogger<DashboardController> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        [HttpGet]
        [Route("/Dashboard")]
        [Route("/")]
        public IActionResult Index()
        {
            using var db = new Models.ConnectToDb(_configuration);
            var stats = GetDashboardStats(db);
            var analytics = GetAnalyticsData(db);
            var recentActivities = GetRecentActivities(db);

            ViewBag.Stats = stats;
            ViewBag.Analytics = analytics;
            ViewBag.RecentActivities = recentActivities;
            ViewBag.UserName = User?.Identity?.Name ?? "Пользователь";

            return View();
        }

        [HttpGet("GetStats")]
        public IActionResult GetStats()
        {
            using var db = new Models.ConnectToDb(_configuration);
            var stats = GetDashboardStats(db);
            return Json(stats);
        }

        [HttpGet("GetAnalytics")]
        public IActionResult GetAnalytics()
        {
            using var db = new Models.ConnectToDb(_configuration);
            var analytics = GetAnalyticsData(db);
            return Json(analytics);
        }

        [HttpGet("GetRecentActivities")]
        public IActionResult GetRecentActivitiesApi()
        {
            using var db = new Models.ConnectToDb(_configuration);
            var activities = GetRecentActivities(db);
            return Json(activities);
        }

        private Models.DashboardStats GetDashboardStats(Models.ConnectToDb db)
        {
            try
            {
                var tasks = db.GetTasks().ToList();
                var resources = db.GetResources().ToList();
                var shifts = db.GetShifts().ToList();
                var shiftTasks = db.GetShiftTasks().ToList();

                var now = DateTime.UtcNow;
                var weekAgo = now.AddDays(-7);

                return new Models.DashboardStats
                {
                    TotalTasks = tasks.Count,
                    ActiveTasks = tasks.Count(t => t.time_pref_start <= now && t.time_pref_finish >= now),
                    CompletedTasks = tasks.Count(t => t.time_pref_finish < now),
                    TotalResources = resources.Count,
                    ActiveResources = resources.Count(r => shifts.Any(s => s.resource_id == r.uid &&
                        s.time_start <= now && s.time_finish >= now)),
                    TotalShifts = shifts.Count,
                    ActiveShifts = shifts.Count(s => s.time_start <= now && s.time_finish >= now),
                    ScheduledTasks = shiftTasks.Count,
                    ProductivityScore = CalculateProductivityScore(tasks, shiftTasks),
                    TasksThisWeek = tasks.Count(t => t.time_ins >= weekAgo),
                    ResourcesUtilization = CalculateUtilization(resources, shifts, shiftTasks)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting dashboard stats");
                return new Models.DashboardStats();
            }
        }

        private Models.AnalyticsData GetAnalyticsData(Models.ConnectToDb db)
        {
            try
            {
                var tasks = db.GetTasks().ToList();
                var shifts = db.GetShifts().ToList();
                var shiftTasks = db.GetShiftTasks().ToList();

                var last30Days = Enumerable.Range(0, 30)
                    .Select(i => DateTime.UtcNow.Date.AddDays(-i))
                    .Reverse()
                    .ToList();

                var dailyTasks = new Dictionary<string, Models.DailyTaskData>();
                foreach (var date in last30Days)
                {
                    var count = tasks.Count(t => t.time_ins.Date == date);
                    var completed = tasks.Count(t => t.time_pref_finish.Date == date && t.time_pref_finish < DateTime.UtcNow);
                    dailyTasks[date.ToString("MM/dd")] = new Models.DailyTaskData { Count = count, Completed = completed };
                }

                var resourcePerformance = CalculateResourcePerformance(db);

                return new Models.AnalyticsData
                {
                    DailyTasks = dailyTasks,
                    TaskStatusDistribution = new Dictionary<string, int>
                    {
                        ["Not Started"] = tasks.Count(t => t.time_pref_start > DateTime.UtcNow),
                        ["In Progress"] = tasks.Count(t => t.time_pref_start <= DateTime.UtcNow && t.time_pref_finish >= DateTime.UtcNow),
                        ["Completed"] = tasks.Count(t => t.time_pref_finish < DateTime.UtcNow),
                        ["Overdue"] = tasks.Count(t => t.time_pref_finish < DateTime.UtcNow &&
                            shiftTasks.All(st => st.task_id != t.uid))
                    },
                    ResourcePerformance = resourcePerformance,
                    PeakHours = CalculatePeakHours(shifts),
                    TaskCompletionRate = CalculateCompletionRate(tasks, shiftTasks)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting analytics data");
                return new Models.AnalyticsData();
            }
        }

        private List<Models.Activity> GetRecentActivities(Models.ConnectToDb db)
        {
            try
            {
                var activities = new List<Models.Activity>();

                var recentTasks = db.GetTasks()
                    .OrderByDescending(t => t.time_ins)
                    .Take(5)
                    .Select(t => new Models.Activity
                    {
                        Id = t.uid.ToString(),
                        Type = "Task",
                        Description = $"Задача '{t.name}' создана",
                        Timestamp = t.time_ins,
                        User = "System"
                    });

                var recentShifts = db.GetShifts()
                    .OrderByDescending(s => s.time_ins)
                    .Take(5)
                    .Select(s => new Models.Activity
                    {
                        Id = s.uid.ToString(),
                        Type = "Shift",
                        Description = $"Смена '{s.name}' запланирована",
                        Timestamp = s.time_ins,
                        User = "System"
                    });

                activities.AddRange(recentTasks);
                activities.AddRange(recentShifts);

                return activities
                    .OrderByDescending(a => a.Timestamp)
                    .Take(10)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recent activities");
                return new List<Models.Activity>();
            }
        }

        private float CalculateProductivityScore(List<Models.Task> tasks, List<Models.ShiftTask> shiftTasks)
        {
            if (tasks.Count == 0) return 0;

            var completedTasks = tasks.Count(t => t.time_pref_finish < DateTime.UtcNow);
            var scheduledTasks = shiftTasks.Count;

            return ((float)completedTasks / tasks.Count * 100 +
                   (float)scheduledTasks / tasks.Count * 100) / 2;
        }

        private float CalculateUtilization(List<Models.Resource> resources, List<Models.Shift> shifts, List<Models.ShiftTask> shiftTasks)
        {
            if (resources.Count == 0) return 0;

            var utilizedResources = resources.Count(r =>
                shifts.Any(s => s.resource_id == r.uid) &&
                shiftTasks.Any(st => st.shift_id == shifts.First(s => s.resource_id == r.uid).uid));

            return (float)utilizedResources / resources.Count * 100;
        }

        private Dictionary<string, float> CalculateResourcePerformance(Models.ConnectToDb db)
        {
            var resources = db.GetResources().ToList();
            var shifts = db.GetShifts().ToList();
            var tasks = db.GetTasks().ToList();
            var shiftTasks = db.GetShiftTasks().ToList();

            var performance = new Dictionary<string, float>();

            foreach (var resource in resources)
            {
                var resourceShifts = shifts.Where(s => s.resource_id == resource.uid).ToList();
                var resourceTasks = resourceShifts
                    .SelectMany(s => shiftTasks.Where(st => st.shift_id == s.uid))
                    .ToList();

                if (resourceTasks.Count == 0)
                {
                    performance[resource.name] = 0;
                    continue;
                }

                var totalDuration = resourceTasks.Sum(st =>
                    (st.time_sched_finish - st.time_sched_start).GetValueOrDefault().TotalHours);
                var idleTime = resourceTasks.Sum(st => st.idle_dur ?? 0) / 60.0;

                performance[resource.name] = totalDuration > 0 ?
                    (float)((totalDuration - idleTime) / totalDuration * 100) : 0;
            }

            return performance;
        }

        private Dictionary<int, int> CalculatePeakHours(List<Models.Shift> shifts)
        {
            var hourlyCounts = new Dictionary<int, int>();

            for (int hour = 0; hour < 24; hour++)
            {
                hourlyCounts[hour] = shifts.Count(s =>
                    s.time_start.Hour <= hour && s.time_finish.Hour >= hour);
            }

            return hourlyCounts;
        }

        private float CalculateCompletionRate(List<Models.Task> tasks, List<Models.ShiftTask> shiftTasks)
        {
            if (tasks.Count == 0) return 0;

            var completedTasks = tasks.Count(t => t.time_pref_finish < DateTime.UtcNow);
            var scheduledTasks = shiftTasks.Count(st =>
                tasks.Any(t => t.uid == st.task_id && t.time_pref_finish < DateTime.UtcNow));

            return scheduledTasks > 0 ? (float)completedTasks / scheduledTasks * 100 : 0;
        }
    }
}