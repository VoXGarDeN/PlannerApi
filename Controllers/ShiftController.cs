using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PlannerApi.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize]
public class ShiftController : ControllerBase
{
    private ConnectToDb _db;
    private readonly ILogger<ShiftController> _logger;

    public ShiftController(ILogger<ShiftController> logger)
    {
        _logger = logger;

        _db = new ConnectToDb();
    }

    [HttpGet(Name = "GetShifts")]
    public IEnumerable<shift> Get()
    {
        return _db.GetShifts().ToArray();
    }

    [HttpPut(Name = "PutShift")]
    public bool Put(shift shift)
    {
        return _db.PutShift(shift);
    } 

    [HttpPost("ClearShifts")]
    public bool Clear()
    {
        return _db.ClearShifts();
    }   

    [HttpPost("GenerateShifts")]
    public bool Generate()
    {
        var time_ins=DateTime.UtcNow;

        DateTime startDate = new DateTime(time_ins.Year, time_ins.Month, 1);
        DateTime endDate = startDate.AddMonths(1).AddDays(-1);

        TimeSpan timeSpan = endDate - startDate;
        var resources=_db.GetResources();
        foreach(var res in resources){
            for (int i=0; i<2; i++) {
                var shift=new shift();
                shift.name=res.name+" "+i.ToString()+" "+i.ToString();
                shift.time_ins=time_ins;
                shift.uid=Guid.NewGuid();
                shift.resource_id=res.uid;
                Random random = new Random();
                TimeSpan newSpan = new TimeSpan(0, random.Next(0, (int)timeSpan.TotalMinutes), 0);
                DateTime newDate = startDate + newSpan;
                shift.time_start=newDate;
                Random random1 = new Random();
                TimeSpan timeSpan1 = endDate - newDate;            
                TimeSpan newSpan1 = new TimeSpan(0, random1.Next(0, (int)timeSpan1.TotalMinutes), 0);
                DateTime newDate1 = newDate + newSpan1;
                shift.time_finish=newDate1;
                _db.PutShift(shift);
            } 
        }

        return true;
    }       

}
