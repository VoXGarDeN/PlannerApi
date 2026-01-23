using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PlannerApi.Controllers;

[ApiController]
[Authorize]
[Route("[controller]")]
[ApiExplorerSettings(IgnoreApi = true)] 
public class HomeController : ControllerBase
{
    private ConnectToDb _db;
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
        _db = new ConnectToDb();
    }
}