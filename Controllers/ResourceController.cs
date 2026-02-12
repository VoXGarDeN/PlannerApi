using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlannerApi.Models;
namespace PlannerApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Authorize]
    public class ResourceController : ControllerBase
    {
        private readonly ILogger<ResourceController> _logger;
        private readonly IConfiguration _configuration;

        public ResourceController(ILogger<ResourceController> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        [HttpGet(Name = "GetResources")]
        public IEnumerable<Models.Resource> Get()
        {
            using var db = new Models.ConnectToDb(_configuration);
            return db.GetResources().ToArray();
        }

        [HttpPut(Name = "PutResource")]
        public bool Put(Models.Resource res)
        {
            using var db = new Models.ConnectToDb(_configuration);
            return db.PutResource(res);
        }

        [HttpPost("ClearResources")]
        public bool Clear()
        {
            using var db = new Models.ConnectToDb(_configuration);
            return db.ClearResources();
        }

        [HttpPost("GenerateResources")]
        public bool Generate()
        {
            var time_ins = DateTime.UtcNow;
            var company_id = Guid.NewGuid();

            using var db = new Models.ConnectToDb(_configuration);
            for (int i = 0; i < 5; i++)
            {
                var res = new Models.Resource();
                res.company_id = company_id;
                res.name = i.ToString() + " " + i.ToString() + " " + i.ToString();
                res.time_ins = time_ins;
                res.uid = Guid.NewGuid();
                db.PutResource(res);
            }

            return true;
        }
    }
}