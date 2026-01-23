using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PlannerApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Authorize]
    public class TaskController : ControllerBase
    {
        private readonly ILogger<TaskController> _logger;

        public TaskController(ILogger<TaskController> logger)
        {
            _logger = logger;
        }

        [HttpGet(Name = "GetTasks")]
        public IEnumerable<Models.task> Get()
        {
            using var db = new Models.ConnectToDb();
            return db.GetTasks().ToArray();
        }

        [HttpPut(Name = "PutTask")]
        public bool Put(Models.task task)
        {
            using var db = new Models.ConnectToDb();
            return db.PutTask(task);
        }

        [HttpPost("ClearTasks")]
        public bool Clear()
        {
            using var db = new Models.ConnectToDb();
            return db.ClearTasks();
        }

        [HttpPost("GenerateTasks")]
        public bool Generate()
        {
            var time_ins = DateTime.UtcNow;
            var company_id = Guid.NewGuid();

            DateTime startDate = new DateTime(time_ins.Year, time_ins.Month, 1);
            DateTime endDate = startDate.AddMonths(1).AddDays(-1);

            TimeSpan timeSpan = endDate - startDate;

            using var db = new Models.ConnectToDb();
            for (int i = 0; i < 10; i++)
            {
                var task = new Models.task();
                task.company_id = company_id;
                task.name = i.ToString() + " " + i.ToString() + " " + i.ToString();
                task.time_ins = time_ins;
                task.uid = Guid.NewGuid();
                Random random = new Random();
                TimeSpan newSpan = new TimeSpan(0, random.Next(0, (int)timeSpan.TotalMinutes), 0);
                DateTime newDate = startDate + newSpan;
                task.time_pref_start = newDate;
                Random random1 = new Random();
                TimeSpan timeSpan1 = endDate - newDate;
                TimeSpan newSpan1 = new TimeSpan(0, random1.Next(0, (int)timeSpan1.TotalMinutes), 0);
                DateTime newDate1 = newDate + newSpan1;
                task.time_pref_finish = newDate1;
                task.duration = (int)newSpan.TotalMinutes;

                db.PutTask(task);
            }

            return true;
        }
    }
}