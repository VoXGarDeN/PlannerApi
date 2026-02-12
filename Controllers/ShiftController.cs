using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PlannerApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Authorize]
    public class ShiftController : ControllerBase
    {
        private readonly ILogger<ShiftController> _logger;
        private readonly IConfiguration _configuration;

        public ShiftController(ILogger<ShiftController> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        [HttpGet(Name = "GetShifts")]
        public IEnumerable<Models.WorkShift> Get()
        {
            using var db = new Models.ConnectToDb(_configuration);
            return db.GetShifts().ToArray();
        }

        [HttpPut(Name = "PutShift")]
        public bool Put(Models.WorkShift shift)
        {
            using var db = new Models.ConnectToDb(_configuration);
            return db.PutShift(shift);
        }

        [HttpPost("ClearShifts")]
        public bool Clear()
        {
            using var db = new Models.ConnectToDb(_configuration);
            return db.ClearShifts();
        }

        [HttpPost("GenerateShifts")]
        public bool Generate()
        {
            var time_ins = DateTime.UtcNow;

            DateTime startDate = new DateTime(time_ins.Year, time_ins.Month, 1);
            DateTime endDate = startDate.AddMonths(1).AddDays(-1);

            TimeSpan timeSpan = endDate - startDate;

            using var db = new Models.ConnectToDb(_configuration);
            var resources = db.GetResources();

            foreach (var res in resources)
            {
                for (int i = 0; i < 2; i++)
                {
                    var shift = new Models.WorkShift();
                    shift.name = res.name + " " + i.ToString() + " " + i.ToString();
                    shift.time_ins = time_ins;
                    shift.uid = Guid.NewGuid();
                    shift.resource_id = res.uid;
                    Random random = new Random();
                    TimeSpan newSpan = new TimeSpan(0, random.Next(0, (int)timeSpan.TotalMinutes), 0);
                    DateTime newDate = startDate + newSpan;
                    shift.time_start = newDate;
                    Random random1 = new Random();
                    TimeSpan timeSpan1 = endDate - newDate;
                    TimeSpan newSpan1 = new TimeSpan(0, random1.Next(0, (int)timeSpan1.TotalMinutes), 0);
                    DateTime newDate1 = newDate + newSpan1;
                    shift.time_finish = newDate1;
                    db.PutShift(shift);
                }
            }

            return true;
        }
    }
}