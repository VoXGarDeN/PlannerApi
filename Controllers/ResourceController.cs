using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PlannerApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Authorize]
    public class ResourceController : ControllerBase
    {
        private readonly ILogger<ResourceController> _logger;

        public ResourceController(ILogger<ResourceController> logger)
        {
            _logger = logger;
        }

        [HttpGet(Name = "GetResources")]
        public IEnumerable<Models.resource> Get()
        {
            using var db = new Models.ConnectToDb();
            return db.GetResources().ToArray();
        }

        [HttpPut(Name = "PutResource")]
        public bool Put(Models.resource res)
        {
            using var db = new Models.ConnectToDb();
            return db.PutResource(res);
        }

        [HttpPost("ClearResources")]
        public bool Clear()
        {
            using var db = new Models.ConnectToDb();
            return db.ClearResources();
        }

        [HttpPost("GenerateResources")]
        public bool Generate()
        {
            var time_ins = DateTime.UtcNow;
            var company_id = Guid.NewGuid();

            using var db = new Models.ConnectToDb();
            for (int i = 0; i < 5; i++)
            {
                var res = new Models.resource();
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