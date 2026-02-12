using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using PlannerApi.Models;
namespace PlannerApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Authorize]
    public class ShiftTaskController : ControllerBase
    {
        private readonly ILogger<ShiftTaskController> _logger;
        private readonly IConfiguration _configuration;

        public ShiftTaskController(ILogger<ShiftTaskController> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        [HttpGet("GetShiftTasks")]
        public IEnumerable<Models.ShiftTask> Get()
        {
            using var db = new Models.ConnectToDb(_configuration);
            return db.GetShiftTasks().ToArray();
        }

        [HttpPost("ClearShiftTasks")]
        public bool Clear()
        {
            using var db = new Models.ConnectToDb(_configuration);
            return db.ClearShiftTasks();
        }

        [HttpPost("StartScheduler")]
        public async Task<IActionResult> Start(bool sinc = false)
        {
            using var db = new Models.ConnectToDb(_configuration);
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