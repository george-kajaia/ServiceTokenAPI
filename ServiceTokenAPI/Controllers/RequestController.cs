using ServiceTokenAPI.Entities;
using ServiceTokenAPI.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ServiceTokenAPI.DBContext;
using System.Net.Mime;
using ServiceTokenAPI.Dto;

namespace ServiceTokenAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
public class RequestController(
    ServiceTokenDbContext db,
    IOptionsSnapshot<GeneralOptions> generalOptions) : ControllerBase
{
    private readonly GeneralOptions generalOptions = generalOptions.Value;

    [HttpGet("Get")]
    public async Task<ActionResult<List<Request>>> Get(int CompanyId = -1, RequestStatus status = RequestStatus.None)
    {
        var c = await db.Requests.AsNoTracking().Where(
            x => (x.CompanyId == CompanyId || CompanyId == -1) &&
                 (x.Status == status || status == RequestStatus.None)
            ).ToListAsync();

        return Ok(c);
    }

    [HttpPost("Create")]
    public async Task<ActionResult<Request>> Create([FromBody] RequestDto Request)
    {
        Request request = new Request
        {
            CompanyId = Request.CompanyId,
            ProdId = Request.ProdId,
            RegDate = DateTime.UtcNow,
            Status = RequestStatus.Created,
            TotalCount = Request.TotalCount,
            Price = Request.Price
};

        await db.Requests.AddAsync(request);

        await db.SaveChangesAsync();

        return Ok(request);
    }

    [HttpPost("Approve")]
    public async Task<ActionResult<Request>> Approve(int requestId)
    {
        var Request = await db.Requests.FirstOrDefaultAsync(x => x.Id == requestId && x.Status == RequestStatus.Created);
        if (Request is null) return NotFound();

        Request.Status = RequestStatus.Approved;
        Request.ApproveDate = DateTime.UtcNow;

        var product = await db.Products.AsNoTracking().Where(x => x.Id == Request.ProdId).SingleAsync();

        var platformPublicKey = "";

        List<ServiceToken> serviceTokens = new List<ServiceToken>();
        List<Operation> serviceTokenOperations = new List<Operation>();

        for (int i = 0; i < Request.TotalCount; i++)
        {
            serviceTokens.Add(
                new ServiceToken
                {
                    Id = Guid.NewGuid().ToString(),
                    RowVersion = DateTime.UtcNow,
                    CompanyId = Request.CompanyId,
                    RequestId = Request.Id,
                    ProdId = Request.ProdId,
                    StartDate = DateTime.UtcNow,
                    EndDate = null,
                    Status = ServiceTokenStatus.Available,
                    Count = 0,
                    TotalCount = Request.TotalCount,
                    ScheduleType = product.ScheduleType,                    

                    OwnerType = OwnerType.Company,
                    OwnerPublicKey = platformPublicKey
                }
                );

            serviceTokenOperations.Add(
                new Operation
                {
                    ServiceTokenId = serviceTokens[i].Id,
                    OpType = OpType.Issue,
                    OpDate = DateTime.UtcNow,
                    OwnerPublicKey = platformPublicKey
                }
                );
        }
        ;

        await db.ServiceTokens.AddRangeAsync(serviceTokens);

        await db.Operations.AddRangeAsync(serviceTokenOperations);

        await db.SaveChangesAsync();

        return Ok(Request);
    }

    /*
    [HttpDelete("Delete")]
    public async Task<IActionResult> Delete(int requestId)
    {
        var c = await db.Requests.FirstOrDefaultAsync(x => x.Id == requestId);
        if (c is null) return NotFound();

        db.Requests.Remove(c);
        
        await db.SaveChangesAsync();

        return NoContent();
    }
    */
}
