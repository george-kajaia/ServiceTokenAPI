using ServiceTokenApi.Entities;
using ServiceTokenApi.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ServiceTokenApi.DBContext;
using System.Net.Mime;
using ServiceTokenApi.Dto;

namespace ServiceTokenApi.Controllers;

[ApiController]
[Route("servicetoken/api/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
public class RequestController(
    ServiceTokenDbContext db,
    IOptionsSnapshot<GeneralOptions> generalOptions) : ControllerBase
{
    private readonly GeneralOptions generalOptions = generalOptions.Value;

    [HttpGet("GetAll")]
    public async Task<ActionResult<List<Request>>> GetAll(int CompanyId = -1, RequestStatus status = RequestStatus.None)
    {
        var c = await db.Requests.AsNoTracking().Where(
            x => (x.CompanyId == CompanyId || CompanyId == -1) &&
                 (x.Status == status || status == RequestStatus.None)
            ).ToListAsync();

        return Ok(c);
    }

    [HttpGet("GetById/{id}")]
    public async Task<ActionResult<Request>> GetById(long requestId)
    {
        var request = await db.Requests.FirstOrDefaultAsync(x => x.Id == requestId);
        if (request is null) return NotFound();

        return Ok(request);
    }

    [HttpPost("Create")]
    public async Task<ActionResult> Create([FromBody] RequestDto requestDto)
    {
        var request = new Request
        {
            CompanyId = requestDto.CompanyId,
            RowVersion = DateTime.UtcNow,
            ProdId = requestDto.ProdId,
            ServiceTokenCount = requestDto.ServiceTokenCount,
            RegDate = DateTime.UtcNow,
            Status = RequestStatus.Created
        };

        await db.Requests.AddAsync(request);

        await db.SaveChangesAsync();

        return Ok();
    }

    [HttpPut("Update")]
    public async Task<ActionResult> Update(int requestId, [FromBody] RequestDto requestDto)
    {
        var request = await db.Requests.FirstOrDefaultAsync(x => x.Id == requestId);
        if (request is null) return NotFound();

        request.ProdId = requestDto.ProdId;

        await db.SaveChangesAsync();

        return Ok();
    }

    [HttpDelete("Delete")]
    public async Task<IActionResult> Delete(int requestId)
    {
        var request = await db.Requests.FirstOrDefaultAsync(x => x.Id == requestId);
        if (request is null) return NotFound();

        db.Requests.Remove(request);

        await db.SaveChangesAsync();
        
        return NoContent();
    }

    [HttpPost("Authorize")]
    public async Task<ActionResult> Authorize(int requestId)
    {
        var request = await db.Requests.FirstOrDefaultAsync(x => x.Id == requestId && x.Status == RequestStatus.Created);
        if (request is null) return NotFound();

        request.Status = RequestStatus.Authorised;
        request.AuthorizeDate = DateTime.UtcNow;

        await db.SaveChangesAsync();

        return Ok();
    }

    [HttpPost("Deauthorize")]
    public async Task<ActionResult> Deauthorize(int requestId)
    {
        var request = await db.Requests.FirstOrDefaultAsync(x => x.Id == requestId && x.Status == RequestStatus.Authorised);
        if (request is null) return NotFound();

        request.Status = RequestStatus.Created;
        request.AuthorizeDate = null;

        await db.SaveChangesAsync();

        return Ok();
    }

    [HttpPost("Approve")]
    public async Task<ActionResult> Approve(int requestId)
    {
        var request = await db.Requests.FirstOrDefaultAsync(x => x.Id == requestId && x.Status == RequestStatus.Authorised);
        if (request is null) return NotFound();

        request.Status = RequestStatus.Approved;
        request.ApproveDate = DateTime.UtcNow;

        var product = await db.Products.AsNoTracking().Where(x => x.Id == request.ProdId).SingleAsync();

        List<ServiceToken> serviceTokens = new List<ServiceToken>();
        List<Operation> serviceTokenOperations = new List<Operation>();

        var platformPublicKey = generalOptions.PlatformAccount;

        for (int i = 0; i < request.ServiceTokenCount; i++)
        {
            serviceTokens.Add(
                new ServiceToken
                {
                    Id = Guid.NewGuid().ToString(),
                    RowVersion = DateTime.UtcNow,
                    CompanyId = request.CompanyId,
                    RequestId = request.Id,
                    ProdId = request.ProdId,
                    StartDate = DateTime.UtcNow,
                    EndDate = null,
                    Status = ServiceTokenStatus.Available,
                    Count = 0,
                    ServiceCount = product.ServiceCount,
                    ScheduleType = new ScheduleType
                    {
                        PeriodType = product.ScheduleType.PeriodType,
                        PeriodNumber = product.ScheduleType.PeriodNumber
                    },
                    OwnerType = OwnerType.Company,
                    OwnerPublicKey = platformPublicKey,
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

        return Ok();
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
