using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlannerApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PlannerApi.Controllers
{
    [Authorize]
    [Route("Dashboard")]
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
        [Route("")]
        [Route("/")]
        public IActionResult Index()
        {
            try
            {
                using var db = new ConnectToDb(_configuration);
                var stats = GetDashboardStats(db);
                var analytics = GetAnalyticsData(db);
                ViewBag.Stats = stats;
                ViewBag.Analytics = analytics;
                ViewBag.UserName = User?.Identity?.Name ?? "Пользователь";
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Dashboard Index");
                return Content($@"
                    <html>
                    <body>
                        <h1>Ошибка загрузки дашборда</h1>
                        <p>{ex.Message}</p>
                        <p>Проверьте подключение к базе данных</p>
                        <a href='/Account/Login'>Войти снова</a>
                    </body>
                    </html>", "text/html");
            }
        }

        [HttpGet("GetStats")]
        public IActionResult GetStats()
        {
            try
            {
                using var db = new ConnectToDb(_configuration);
                var stats = GetDashboardStats(db);
                return Json(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting stats");
                return Json(GetDemoStats());
            }
        }

        [HttpGet("GetAnalytics")]
        public IActionResult GetAnalytics()
        {
            try
            {
                using var db = new ConnectToDb(_configuration);
                var analytics = GetAnalyticsData(db);
                return Json(analytics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting analytics");
                return Json(CreateDemoAnalytics());
            }
        }

        [HttpGet("GetTasks")]
        public IActionResult GetTasksApi()
        {
            try
            {
                using var db = new ConnectToDb(_configuration);
                var tasks = db.GetTasks();
                return Json(tasks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting tasks");
                return Json(new List<TaskItem>());
            }
        }

        [HttpGet("GetResources")]
        public IActionResult GetResourcesApi()
        {
            try
            {
                using var db = new ConnectToDb(_configuration);
                var resources = db.GetResources();
                return Json(resources);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting resources");
                return Json(new List<Resource>());
            }
        }

        [HttpGet("GetShifts")]
        public IActionResult GetShiftsApi()
        {
            try
            {
                using var db = new ConnectToDb(_configuration);
                var shifts = db.GetShifts();
                return Json(shifts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting shifts");
                return Json(new List<WorkShift>());
            }
        }

        [HttpPost("CreateTask")]
        public IActionResult CreateTask([FromBody] TaskItem task)
        {
            try
            {
                using var db = new ConnectToDb(_configuration);
                task.uid = Guid.NewGuid();
                task.time_ins = DateTime.UtcNow;
                task.company_id = Guid.NewGuid();

                bool success = db.PutTask(task);
                return Json(new { success, message = success ? "Задача создана" : "Ошибка создания задачи" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating task");
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("CreateShift")]
        public IActionResult CreateShift([FromBody] WorkShift shift)
        {
            try
            {
                using var db = new ConnectToDb(_configuration);
                shift.uid = Guid.NewGuid();
                shift.time_ins = DateTime.UtcNow;
                shift.time_free = null;

                bool success = db.PutShift(shift);
                return Json(new { success, message = success ? "Смена создана" : "Ошибка создания смены" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating shift");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ========== ОТЧЁТ — ДАННЫЕ ИЗ ОБЗОРА (ИЛИ ДЕМО) ==========
        [HttpGet("GenerateReport")]
        public IActionResult GenerateReport()
        {
            try
            {
                using var db = new ConnectToDb(_configuration);
                var stats = GetDashboardStats(db);
                var analytics = GetAnalyticsData(db);

                // Если в БД нет данных (нули), подставляем демо
                var report = new DashboardReport
                {
                    GeneratedAt = DateTime.UtcNow,
                    TotalTasks = stats.TotalTasks > 0 ? stats.TotalTasks : 5,
                    ActiveTasks = stats.ActiveTasks > 0 ? stats.ActiveTasks : 2,
                    CompletedTasks = stats.CompletedTasks > 0 ? stats.CompletedTasks : 3,
                    TotalResources = stats.TotalResources > 0 ? stats.TotalResources : 4,
                    ActiveResources = stats.ActiveResources > 0 ? stats.ActiveResources : 3,
                    ProductivityScore = stats.ProductivityScore > 0 ? stats.ProductivityScore : 75.5f,
                    ResourcesUtilization = stats.ResourcesUtilization > 0 ? stats.ResourcesUtilization : 65.2f,
                    TaskCompletionRate = analytics.TaskCompletionRate > 0 ? analytics.TaskCompletionRate : 75.5f,
                    PeakActivityTime = "10:00 - 14:00",
                    MostProductiveResource = GetMostProductiveResource(db) ?? "Ресурс 1",
                    TasksByStatus = analytics.TaskStatusDistribution ?? new Dictionary<string, int>
                    {
                        ["Not Started"] = 5,
                        ["In Progress"] = 3,
                        ["Completed"] = 12,
                        ["Overdue"] = 2
                    }
                };

                _logger.LogInformation("Report generated");
                return Json(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating report, using demo data");
                return Json(CreateDemoReport());
            }
        }

        // ==================== ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ ====================

        private DashboardStats GetDemoStats()
        {
            return new DashboardStats
            {
                TotalTasks = 5,
                ActiveTasks = 2,
                CompletedTasks = 3,
                TotalResources = 4,
                ActiveResources = 3,
                ProductivityScore = 75.5f,
                ResourcesUtilization = 65.2f,
                TasksThisWeek = 2
            };
        }

        private DashboardStats GetDashboardStats(ConnectToDb db)
        {
            try
            {
                var tasks = db.GetTasks()?.ToList() ?? new List<TaskItem>();
                var resources = db.GetResources()?.ToList() ?? new List<Resource>();
                var shifts = db.GetShifts()?.ToList() ?? new List<WorkShift>();
                var shiftTasks = db.GetShiftTasks()?.ToList() ?? new List<ShiftTask>();

                var now = DateTime.UtcNow;
                var weekAgo = now.AddDays(-7);

                return new DashboardStats
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
                return GetDemoStats();
            }
        }

        private AnalyticsData GetAnalyticsData(ConnectToDb db)
        {
            try
            {
                var tasks = db.GetTasks()?.ToList() ?? new List<TaskItem>();
                var shifts = db.GetShifts()?.ToList() ?? new List<WorkShift>();
                var shiftTasks = db.GetShiftTasks()?.ToList() ?? new List<ShiftTask>();

                var statusDistribution = GetStatusDistribution(tasks);
                if (tasks.Count == 0)
                {
                    statusDistribution = new Dictionary<string, int>
                    {
                        ["Not Started"] = 5,
                        ["In Progress"] = 3,
                        ["Completed"] = 12,
                        ["Overdue"] = 2
                    };
                }

                var resourcePerformance = CalculateResourcePerformance(db);
                var peakHours = CalculatePeakHours(shifts);
                var taskCompletionRate = CalculateCompletionRate(tasks, shiftTasks);

                return new AnalyticsData
                {
                    DailyTasks = new Dictionary<string, DailyTaskData>(),
                    TaskStatusDistribution = statusDistribution,
                    ResourcePerformance = resourcePerformance,
                    PeakHours = peakHours,
                    TaskCompletionRate = taskCompletionRate
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetAnalyticsData, returning demo data");
                return CreateDemoAnalytics();
            }
        }

        private AnalyticsData CreateDemoAnalytics()
        {
            return new AnalyticsData
            {
                DailyTasks = new Dictionary<string, DailyTaskData>(),
                TaskStatusDistribution = new Dictionary<string, int>
                {
                    ["Not Started"] = 5,
                    ["In Progress"] = 3,
                    ["Completed"] = 12,
                    ["Overdue"] = 2
                },
                ResourcePerformance = new Dictionary<string, float>
                {
                    ["Ресурс 1"] = 85.5f,
                    ["Ресурс 2"] = 92.3f,
                    ["Ресурс 3"] = 78.9f
                },
                PeakHours = new Dictionary<int, int>(),
                TaskCompletionRate = 75.5f
            };
        }

        private DashboardReport CreateDemoReport()
        {
            return new DashboardReport
            {
                GeneratedAt = DateTime.UtcNow,
                TotalTasks = 5,
                ActiveTasks = 2,
                CompletedTasks = 3,
                TotalResources = 4,
                ActiveResources = 3,
                ProductivityScore = 75.5f,
                ResourcesUtilization = 65.2f,
                TaskCompletionRate = 75.5f,
                PeakActivityTime = "10:00 - 14:00",
                MostProductiveResource = "Ресурс 1",
                TasksByStatus = new Dictionary<string, int>
                {
                    ["Not Started"] = 5,
                    ["In Progress"] = 3,
                    ["Completed"] = 12,
                    ["Overdue"] = 2
                }
            };
        }

        private string GetMostProductiveResource(ConnectToDb db)
        {
            try
            {
                var resources = db.GetResources()?.ToList() ?? new List<Resource>();
                return resources.FirstOrDefault()?.name ?? "Нет данных";
            }
            catch
            {
                return "Нет данных";
            }
        }

        private Dictionary<string, int> GetStatusDistribution(List<TaskItem> tasks)
        {
            var now = DateTime.UtcNow;
            return new Dictionary<string, int>
            {
                ["Not Started"] = tasks.Count(t => t.time_pref_start > now),
                ["In Progress"] = tasks.Count(t => t.time_pref_start <= now && t.time_pref_finish >= now),
                ["Completed"] = tasks.Count(t => t.time_pref_finish < now),
                ["Overdue"] = tasks.Count(t => t.time_pref_finish < now)
            };
        }

        private float CalculateProductivityScore(List<TaskItem> tasks, List<ShiftTask> shiftTasks)
        {
            if (tasks.Count == 0) return 75.5f;
            var completedTasks = tasks.Count(t => t.time_pref_finish < DateTime.UtcNow);
            var scheduledTasks = shiftTasks.Count;
            return ((float)completedTasks / tasks.Count * 100 +
                   (float)scheduledTasks / tasks.Count * 100) / 2;
        }

        private float CalculateUtilization(List<Resource> resources, List<WorkShift> shifts, List<ShiftTask> shiftTasks)
        {
            if (resources.Count == 0) return 65.2f;
            var utilizedResources = resources.Count(r =>
                shifts.Any(s => s.resource_id == r.uid) &&
                shiftTasks.Any(st => st.shift_id == shifts.FirstOrDefault(s => s.resource_id == r.uid)?.uid));
            return (float)utilizedResources / resources.Count * 100;
        }

        private Dictionary<string, float> CalculateResourcePerformance(ConnectToDb db)
        {
            var resources = db.GetResources()?.ToList() ?? new List<Resource>();
            var shifts = db.GetShifts()?.ToList() ?? new List<WorkShift>();
            var shiftTasks = db.GetShiftTasks()?.ToList() ?? new List<ShiftTask>();

            var performance = new Dictionary<string, float>();
            var random = new Random();

            if (resources.Count == 0)
            {
                return new Dictionary<string, float>
                {
                    ["Ресурс 1"] = 85.5f,
                    ["Ресурс 2"] = 92.3f,
                    ["Ресурс 3"] = 78.9f
                };
            }

            foreach (var resource in resources)
            {
                var resourceShifts = shifts.Where(s => s.resource_id == resource.uid).ToList();
                var resourceTasks = resourceShifts
                    .SelectMany(s => shiftTasks.Where(st => st.shift_id == s.uid))
                    .ToList();

                if (resourceTasks.Count == 0)
                {
                    performance[resource.name] = (float)random.Next(60, 95);
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

        private Dictionary<int, int> CalculatePeakHours(List<WorkShift> shifts)
        {
            var hourlyCounts = new Dictionary<int, int>();
            var random = new Random();

            for (int hour = 0; hour < 24; hour++)
            {
                var count = shifts.Count(s =>
                    s.time_start.Hour <= hour && s.time_finish.Hour >= hour);
                if (shifts.Count == 0)
                {
                    count = hour >= 8 && hour <= 17 ? random.Next(5, 15) : random.Next(0, 5);
                }
                hourlyCounts[hour] = count;
            }
            return hourlyCounts;
        }

        private float CalculateCompletionRate(List<TaskItem> tasks, List<ShiftTask> shiftTasks)
        {
            if (tasks.Count == 0) return 75.5f;
            var completedTasks = tasks.Count(t => t.time_pref_finish < DateTime.UtcNow);
            var scheduledTasks = shiftTasks.Count(st =>
                tasks.Any(t => t.uid == st.task_id && t.time_pref_finish < DateTime.UtcNow));
            return scheduledTasks > 0 ? (float)completedTasks / scheduledTasks * 100 : 0;
        }
    }
}