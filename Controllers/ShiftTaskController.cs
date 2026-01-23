using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PlannerApi.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize]
public class ShiftTaskController : ControllerBase
{
    private ConnectToDb _db;
    private readonly ILogger<ShiftTaskController> _logger;

    public ShiftTaskController(ILogger<ShiftTaskController> logger)
    {
        _logger = logger;

        _db = new ConnectToDb();
    }

    [HttpGet("GetShiftTasks")]
    public IEnumerable<shift_task> Get()
    {
        return _db.GetShiftTasks().ToArray();
    }

    [HttpPost("ClearShiftTasks")]
    public bool Clear()
    {
        return _db.ClearShiftTasks();
    }       

    [HttpPost("StartScheduler")]
    public async Task<ActionResult> Start(bool sinc=false)
    {
        await _db.StartScheduler(sinc);
        return Ok();
    }     

    [HttpPost("StopScheduler")]
    public IActionResult Stop()
    {
        ConnectToDb.stop=true;
        return Ok();
    }   
    [HttpGet("Progress")]
    public IActionResult Progress()
    {

        
        // Random random = new Random();
        // int randomValue = random.Next(0, 101);

        return new JsonResult(new { progress = ConnectToDb.progress, asincerrors = ConnectToDb.asincerrors });

    }    




}
