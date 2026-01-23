using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace PlannerApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Authorize]
    public class ShiftTaskController : ControllerBase
    {
        private readonly ILogger<ShiftTaskController> _logger;

        public ShiftTaskController(ILogger<ShiftTaskController> logger)
        {
            _logger = logger;
        }

        [HttpGet("GetShiftTasks")]
        public IEnumerable<Models.shift_task> Get()
        {
            using var db = new Models.ConnectToDb();
            return db.GetShiftTasks().ToArray();
        }

        [HttpPost("ClearShiftTasks")]
        public bool Clear()
        {
            using var db = new Models.ConnectToDb();
            return db.ClearShiftTasks();
        }

        [HttpPost("StartScheduler")]
        public async Task<ActionResult> Start(bool sinc = false)
        {
            using var db = new Models.ConnectToDb();
            await db.StartScheduler(sinc);
            return Ok();
        }

        [HttpPost("StopScheduler")]
        public IActionResult Stop()
        {
            Models.ConnectToDb.stop = true;
            return Ok();
        }

        [HttpGet("Progress")]
        public IActionResult Progress()
        {
            return new JsonResult(new
            {
                progress = Models.ConnectToDb.progress,
                asincerrors = Models.ConnectToDb.asincerrors
            });
        }
    }
}