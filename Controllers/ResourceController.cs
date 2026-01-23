using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PlannerApi.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize]
public class ResourceController : ControllerBase
{

    private ConnectToDb _db;
    private readonly ILogger<ResourceController> _logger;

    public ResourceController(ILogger<ResourceController> logger)
    {
        _logger = logger;

        _db = new ConnectToDb();
    }

    [HttpGet(Name = "GetResources")]
    public IEnumerable<resource> Get()
    {
        return _db.GetResources().ToArray();
    }

    [HttpPut(Name = "PutResource")]
    public bool Put(resource res)
    {
        return _db.PutResource(res);
    }    

    [HttpPost("ClearResources")]
    public bool Clear()
    {
        return _db.ClearResources();
    }   

    [HttpPost("GenerateResources")]
    public bool Generate()
    {
        var time_ins=DateTime.UtcNow;
        var company_id =Guid.NewGuid();
        for (int i=0; i<5; i++) {
            var res=new resource();
            res.company_id=company_id;
            res.name=i.ToString()+" "+i.ToString()+" "+i.ToString();
            res.time_ins=time_ins;
            res.uid=Guid.NewGuid();
            _db.PutResource(res);
        } 

        return true;
    }      


}
