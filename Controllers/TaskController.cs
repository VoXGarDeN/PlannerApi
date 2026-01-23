using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PlannerApi.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize]
public class TaskController : ControllerBase
{
    private ConnectToDb _db;
    private readonly ILogger<TaskController> _logger;

    public TaskController(ILogger<TaskController> logger)
    {
        _logger = logger;

        _db = new ConnectToDb();
    }

    [HttpGet(Name = "GetTasks")]
    public IEnumerable<task> Get()
    {
        return _db.GetTasks().ToArray();
    }

    [HttpGet("Statistics")]
    public IActionResult GetStatistics()
    {
        var tasks = _db.GetTasks().ToArray();
        var now = DateTime.UtcNow;

        var stats = new
        {
            TotalTasks = tasks.Length,
            ActiveTasks = tasks.Count(t =>
                t.time_pref_finish > now && t.time_pref_start <= now),
            CompletedTasks = tasks.Count(t => t.time_pref_finish <= now),
            OverdueTasks = tasks.Count(t => t.time_pref_finish < now),
            UpcomingTasks = tasks.Count(t => t.time_pref_start > now)
        };

        return Ok(stats);
    }
    [HttpPut(Name = "PutTask")]
    public bool Put(task task)
    {
        return _db.PutTask(task);
    }   

    [HttpPost("ClearTasks")]
    public bool Clear()
    {
        return _db.ClearTasks();
    }   

    [HttpPost("GenerateTasks")]
    public bool Generate()
    {
        var time_ins=DateTime.UtcNow;
        var company_id =Guid.NewGuid();

        DateTime startDate = new DateTime(time_ins.Year, time_ins.Month, 1);
        DateTime endDate = startDate.AddMonths(1).AddDays(-1);

        TimeSpan timeSpan = endDate - startDate;

        for (int i=0; i<10; i++) {
            var task=new task();
            task.company_id=company_id;
            task.name=i.ToString()+" "+i.ToString()+" "+i.ToString();
            task.time_ins=time_ins;
            task.uid=Guid.NewGuid();
            Random random = new Random();
            TimeSpan newSpan = new TimeSpan(0, random.Next(0, (int)timeSpan.TotalMinutes), 0);
            DateTime newDate = startDate + newSpan;
            task.time_pref_start=newDate;
            Random random1 = new Random();
            TimeSpan timeSpan1 = endDate - newDate;            
            TimeSpan newSpan1 = new TimeSpan(0, random1.Next(0, (int)timeSpan1.TotalMinutes), 0);
            DateTime newDate1 = newDate + newSpan1;
            task.time_pref_finish=newDate1;
            task.duration=(int)newSpan.TotalMinutes;

            _db.PutTask(task);
        } 

        return true;
    }       


}
