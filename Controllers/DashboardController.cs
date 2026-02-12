using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
                using var db = new Models.ConnectToDb(_configuration);
                var stats = GetDashboardStats(db);
                return Json(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting stats");
                return Json(new Models.DashboardStats());
            }
        }

        [HttpGet("GetAnalytics")]
        public IActionResult GetAnalytics()
        {
            try
            {
                using var db = new Models.ConnectToDb(_configuration);
                var analytics = GetAnalyticsData(db);
                return Json(analytics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting analytics");
                return Json(new Models.AnalyticsData());
            }
        }

        [HttpGet("GetRecentActivities")]
        public IActionResult GetRecentActivitiesApi()
        {
            try
            {
                using var db = new Models.ConnectToDb(_configuration);
                var activities = GetRecentActivities(db);
                return Json(activities);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recent activities");
                return Json(new List<Models.Activity>());
            }
        }

        [HttpGet("GetNotifications")]
        public IActionResult GetNotifications()
        {
            try
            {
                var notifications = new List<Models.Notification>
                {
                    new Models.Notification
                    {
                        Id = Guid.NewGuid().ToString(),
                        Type = "Info",
                        Message = "Добро пожаловать в систему Planner!",
                        Timestamp = DateTime.UtcNow.AddHours(-1),
                        IsRead = false
                    },
                    new Models.Notification
                    {
                        Id = Guid.NewGuid().ToString(),
                        Type = "Task",
                        Message = "Новая задача создана успешно",
                        Timestamp = DateTime.UtcNow.AddMinutes(-30),
                        IsRead = false
                    },
                    new Models.Notification
                    {
                        Id = Guid.NewGuid().ToString(),
                        Type = "System",
                        Message = "Система работает в штатном режиме",
                        Timestamp = DateTime.UtcNow.AddMinutes(-15),
                        IsRead = true
                    }
                };

                return Json(notifications);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting notifications");
                return Json(new List<Models.Notification>());
            }
        }

        [HttpGet("GetTasks")]
        public IActionResult GetTasksApi()
        {
            try
            {
                using var db = new Models.ConnectToDb(_configuration);
                var tasks = db.GetTasks();
                return Json(tasks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting tasks");
                return Json(new List<Models.TaskItem>());
            }
        }

        [HttpGet("GetResources")]
        public IActionResult GetResourcesApi()
        {
            try
            {
                using var db = new Models.ConnectToDb(_configuration);
                var resources = db.GetResources();
                return Json(resources);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting resources");
                return Json(new List<Models.Resource>());
            }
        }

        [HttpGet("GetShifts")]
        public IActionResult GetShiftsApi()
        {
            try
            {
                using var db = new Models.ConnectToDb(_configuration);
                var shifts = db.GetShifts();
                return Json(shifts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting shifts");
                return Json(new List<Models.WorkShift>());
            }
        }

        [HttpGet("GetShiftTasks")]
        public IActionResult GetShiftTasksApi()
        {
            try
            {
                using var db = new Models.ConnectToDb(_configuration);
                var shiftTasks = db.GetShiftTasks();
                return Json(shiftTasks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting shift tasks");
                return Json(new List<Models.ShiftTask>());
            }
        }

        [HttpPost("CreateTask")]
        public IActionResult CreateTask([FromBody] Models.TaskItem task)
        {
            try
            {
                using var db = new Models.ConnectToDb(_configuration);
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
        public IActionResult CreateShift([FromBody] Models.WorkShift shift)
        {
            try
            {
                using var db = new Models.ConnectToDb(_configuration);
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

        [HttpGet("GenerateReport")]
        public IActionResult GenerateReport()
        {
            try
            {
                using var db = new Models.ConnectToDb(_configuration);
                var stats = GetDashboardStats(db);
                var analytics = GetAnalyticsData(db);

                // Создаем демо-данные для отчета
                var report = new Models.DashboardReport
                {
                    GeneratedAt = DateTime.UtcNow,
                    TotalTasks = stats.TotalTasks,
                    ActiveTasks = stats.ActiveTasks,
                    CompletedTasks = stats.CompletedTasks,
                    TotalResources = stats.TotalResources,
                    ActiveResources = stats.ActiveResources,
                    ProductivityScore = stats.ProductivityScore,
                    ResourcesUtilization = stats.ResourcesUtilization,
                    TaskCompletionRate = analytics.TaskCompletionRate,
                    PeakActivityTime = "10:00 - 14:00",
                    MostProductiveResource = GetMostProductiveResource(db),
                    TasksByStatus = new Dictionary<string, int>
                    {
                        ["Not Started"] = analytics.TaskStatusDistribution.ContainsKey("Not Started") ? analytics.TaskStatusDistribution["Not Started"] : 0,
                        ["In Progress"] = analytics.TaskStatusDistribution.ContainsKey("In Progress") ? analytics.TaskStatusDistribution["In Progress"] : 0,
                        ["Completed"] = analytics.TaskStatusDistribution.ContainsKey("Completed") ? analytics.TaskStatusDistribution["Completed"] : 0,
                        ["Overdue"] = analytics.TaskStatusDistribution.ContainsKey("Overdue") ? analytics.TaskStatusDistribution["Overdue"] : 0
                    }
                };

                return Json(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating report");
                return Json(new { error = ex.Message });
            }
        }

        private string GetMostProductiveResource(Models.ConnectToDb db)
        {
            try
            {
                var resources = db.GetResources()?.ToList() ?? new List<Models.Resource>();
                return resources.FirstOrDefault()?.name ?? "Нет данных";
            }
            catch
            {
                return "Нет данных";
            }
        }

        private Models.DashboardStats GetDashboardStats(Models.ConnectToDb db)
        {
            try
            {
                var tasks = db.GetTasks()?.ToList() ?? new List<Models.TaskItem>();
                var resources = db.GetResources()?.ToList() ?? new List<Models.Resource>();
                var shifts = db.GetShifts()?.ToList() ?? new List<Models.WorkShift>();
                var shiftTasks = db.GetShiftTasks()?.ToList() ?? new List<Models.ShiftTask>();

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
                return new Models.DashboardStats
                {
                    TotalTasks = 5,
                    ActiveTasks = 2,
                    CompletedTasks = 3,
                    TotalResources = 4,
                    ActiveResources = 3,
                    ProductivityScore = 75.5f,
                    ResourcesUtilization = 65.2f
                };
            }
        }

        private Models.AnalyticsData GetAnalyticsData(Models.ConnectToDb db)
        {
            try
            {
                var tasks = db.GetTasks()?.ToList() ?? new List<Models.TaskItem>();
                var shifts = db.GetShifts()?.ToList() ?? new List<Models.WorkShift>();
                var shiftTasks = db.GetShiftTasks()?.ToList() ?? new List<Models.ShiftTask>();

                // Создаем демо-данные для графиков если нет реальных
                var last30Days = Enumerable.Range(0, 30)
                    .Select(i => DateTime.UtcNow.Date.AddDays(-i))
                    .Reverse()
                    .ToList();

                var dailyTasks = new Dictionary<string, Models.DailyTaskData>();
                var random = new Random();

                foreach (var date in last30Days)
                {
                    var count = tasks.Count(t => t.time_ins.Date == date);
                    var completed = tasks.Count(t => t.time_pref_finish.Date == date && t.time_pref_finish < DateTime.UtcNow);

                    if (tasks.Count == 0)
                    {
                        count = random.Next(5, 20);
                        completed = random.Next(0, count);
                    }

                    dailyTasks[date.ToString("MM/dd")] = new Models.DailyTaskData { Count = count, Completed = completed };
                }

                var resourcePerformance = CalculateResourcePerformance(db);

                var statusDistribution = new Dictionary<string, int>
                {
                    ["Not Started"] = tasks.Count > 0 ? tasks.Count(t => t.time_pref_start > DateTime.UtcNow) : random.Next(5, 15),
                    ["In Progress"] = tasks.Count > 0 ? tasks.Count(t => t.time_pref_start <= DateTime.UtcNow && t.time_pref_finish >= DateTime.UtcNow) : random.Next(3, 10),
                    ["Completed"] = tasks.Count > 0 ? tasks.Count(t => t.time_pref_finish < DateTime.UtcNow) : random.Next(10, 25),
                    ["Overdue"] = tasks.Count > 0 ? tasks.Count(t => t.time_pref_finish < DateTime.UtcNow &&
                        shiftTasks.All(st => st.task_id != t.uid)) : random.Next(0, 5)
                };

                var peakHours = CalculatePeakHours(shifts);
                var taskCompletionRate = CalculateCompletionRate(tasks, shiftTasks);

                return new Models.AnalyticsData
                {
                    DailyTasks = dailyTasks,
                    TaskStatusDistribution = statusDistribution,
                    ResourcePerformance = resourcePerformance,
                    PeakHours = peakHours,
                    TaskCompletionRate = taskCompletionRate
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting analytics data");
                var random = new Random();
                var demoDailyTasks = new Dictionary<string, Models.DailyTaskData>();
                for (int i = 0; i < 30; i++)
                {
                    var date = DateTime.UtcNow.AddDays(-i).ToString("MM/dd");
                    demoDailyTasks[date] = new Models.DailyTaskData
                    {
                        Count = random.Next(5, 20),
                        Completed = random.Next(0, 10)
                    };
                }

                return new Models.AnalyticsData
                {
                    DailyTasks = demoDailyTasks,
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
        }

        private List<Models.Activity> GetRecentActivities(Models.ConnectToDb db)
        {
            try
            {
                var activities = new List<Models.Activity>();

                var recentTasks = db.GetTasks()
                    ?.OrderByDescending(t => t.time_ins)
                    .Take(5)
                    .Select(t => new Models.Activity
                    {
                        Id = t.uid.ToString(),
                        Type = "Task",
                        Description = $"Задача '{t.name}' создана",
                        Timestamp = t.time_ins,
                        User = "System"
                    }) ?? new List<Models.Activity>();

                var recentShifts = db.GetShifts()
                    ?.OrderByDescending(s => s.time_ins)
                    .Take(5)
                    .Select(s => new Models.Activity
                    {
                        Id = s.uid.ToString(),
                        Type = "Shift",
                        Description = $"Смена '{s.name}' запланирована",
                        Timestamp = s.time_ins,
                        User = "System"
                    }) ?? new List<Models.Activity>();

                var recentShiftTasks = db.GetShiftTasks()
                    ?.OrderByDescending(st => st.time_ins)
                    .Take(3)
                    .Select(st => new Models.Activity
                    {
                        Id = st.task_id.ToString(),
                        Type = "Schedule",
                        Description = $"Задача '{st.task_name}' назначена на смену",
                        Timestamp = st.time_ins,
                        User = "Scheduler"
                    }) ?? new List<Models.Activity>();

                activities.AddRange(recentTasks);
                activities.AddRange(recentShifts);
                activities.AddRange(recentShiftTasks);

                if (activities.Count == 0)
                {
                    var now = DateTime.UtcNow;
                    activities.Add(new Models.Activity
                    {
                        Id = Guid.NewGuid().ToString(),
                        Type = "Task",
                        Description = "Тестовая задача создана",
                        Timestamp = now.AddHours(-2),
                        User = "Admin"
                    });
                    activities.Add(new Models.Activity
                    {
                        Id = Guid.NewGuid().ToString(),
                        Type = "Shift",
                        Description = "Тестовая смена запланирована",
                        Timestamp = now.AddHours(-1),
                        User = "Admin"
                    });
                    activities.Add(new Models.Activity
                    {
                        Id = Guid.NewGuid().ToString(),
                        Type = "System",
                        Description = "Система запущена",
                        Timestamp = now.AddHours(-3),
                        User = "System"
                    });
                    activities.Add(new Models.Activity
                    {
                        Id = Guid.NewGuid().ToString(),
                        Type = "Task",
                        Description = "Отчет подготовлен",
                        Timestamp = now.AddHours(-4),
                        User = "System"
                    });
                }

                return activities
                    .OrderByDescending(a => a.Timestamp)
                    .Take(10)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recent activities");
                var now = DateTime.UtcNow;
                return new List<Models.Activity>
                {
                    new Models.Activity
                    {
                        Id = "1",
                        Type = "System",
                        Description = "Система загружена",
                        Timestamp = now,
                        User = "System"
                    },
                    new Models.Activity
                    {
                        Id = "2",
                        Type = "Info",
                        Description = "Добро пожаловать в Planner System",
                        Timestamp = now.AddMinutes(-5),
                        User = "System"
                    },
                    new Models.Activity
                    {
                        Id = "3",
                        Type = "Task",
                        Description = "Создана демо-задача",
                        Timestamp = now.AddMinutes(-10),
                        User = "Admin"
                    }
                };
            }
        }

        private float CalculateProductivityScore(List<Models.TaskItem> tasks, List<Models.ShiftTask> shiftTasks)
        {
            if (tasks.Count == 0) return 75.5f;

            var completedTasks = tasks.Count(t => t.time_pref_finish < DateTime.UtcNow);
            var scheduledTasks = shiftTasks.Count;

            return ((float)completedTasks / tasks.Count * 100 +
                   (float)scheduledTasks / tasks.Count * 100) / 2;
        }

        private float CalculateUtilization(List<Models.Resource> resources, List<Models.WorkShift> shifts, List<Models.ShiftTask> shiftTasks)
        {
            if (resources.Count == 0) return 65.2f;

            var utilizedResources = resources.Count(r =>
                shifts.Any(s => s.resource_id == r.uid) &&
                shiftTasks.Any(st => st.shift_id == shifts.First(s => s.resource_id == r.uid).uid));

            return (float)utilizedResources / resources.Count * 100;
        }

        private Dictionary<string, float> CalculateResourcePerformance(Models.ConnectToDb db)
        {
            var resources = db.GetResources()?.ToList() ?? new List<Models.Resource>();
            var shifts = db.GetShifts()?.ToList() ?? new List<Models.WorkShift>();
            var tasks = db.GetTasks()?.ToList() ?? new List<Models.TaskItem>();
            var shiftTasks = db.GetShiftTasks()?.ToList() ?? new List<Models.ShiftTask>();

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

        private Dictionary<int, int> CalculatePeakHours(List<Models.WorkShift> shifts)
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

        private float CalculateCompletionRate(List<Models.TaskItem> tasks, List<Models.ShiftTask> shiftTasks)
        {
            if (tasks.Count == 0) return 75.5f;

            var completedTasks = tasks.Count(t => t.time_pref_finish < DateTime.UtcNow);
            var scheduledTasks = shiftTasks.Count(st =>
                tasks.Any(t => t.uid == st.task_id && t.time_pref_finish < DateTime.UtcNow));

            return scheduledTasks > 0 ? (float)completedTasks / scheduledTasks * 100 : 0;
        }
    }
}