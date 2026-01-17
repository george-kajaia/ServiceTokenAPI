using System.Data;
using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.VisualBasic;
using ServiceTokenApi.DBContext;
using ServiceTokenApi.Dto;
using ServiceTokenApi.Entities;
using ServiceTokenApi.Enums;

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
    public async Task<ActionResult> Update(int requestId, uint rowVersion, [FromBody] RequestDto requestDto)
    {
        var request = await db.Requests.FirstOrDefaultAsync(x => x.Id == requestId && x.RowVersion == rowVersion);
        if (request is null) return NotFound("The record was changed by another user. Refresh the data.");

        db.Entry(request).Property(x => x.RowVersion).OriginalValue = rowVersion;

        request.ProdId = requestDto.ProdId;
        request.ServiceTokenCount = requestDto.ServiceTokenCount;

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict("The record was changed by another user. Refresh the data.");
        }

        return Ok(request);
    }

    [HttpDelete("Delete")]
    public async Task<IActionResult> Delete(int requestId, uint rowVersion)
    {
        var request = await db.Requests.FirstOrDefaultAsync(x => x.Id == requestId && x.RowVersion == rowVersion);
        if (request is null) return NotFound("Record not found or already deleted.");

        db.Entry(request).Property(x => x.RowVersion).OriginalValue = rowVersion;

        db.Requests.Remove(request);

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict("Record not found or already deleted.");
        }

        return NoContent();
    }

    [HttpPost("Authorize")]
    public async Task<IActionResult> Authorize(int requestId, uint rowVersion)
    {
        var request = await db.Requests.FirstOrDefaultAsync(x => x.Id == requestId && x.RowVersion == rowVersion);
        if (request is null) return NotFound("The record was changed by another user. Refresh the data.");

        db.Entry(request).Property(x => x.RowVersion).OriginalValue = rowVersion;

        request.Status = RequestStatus.Authorised;
        request.AuthorizeDate = DateTime.UtcNow;

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict("The record was changed by another user. Refresh the data.");
        }

        return Ok();
    }

    [HttpPost("Deauthorize")]
    public async Task<IActionResult> Deauthorize(int requestId, uint rowVersion)
    {
        var request = await db.Requests.FirstOrDefaultAsync(x => x.Id == requestId && x.RowVersion == rowVersion);
        if (request is null) return NotFound("The record was changed by another user. Refresh the data.");

        db.Entry(request).Property(x => x.RowVersion).OriginalValue = rowVersion;

        request.Status = RequestStatus.Created;
        request.AuthorizeDate = null;

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict("The record was changed by another user. Refresh the data.");
        }

        return Ok();
    }

    [HttpPost("Approve")]
    public async Task<IActionResult> Approve(int requestId, uint rowVersion)
    {
        var request = await db.Requests.FirstOrDefaultAsync(x => x.Id == requestId && x.RowVersion == rowVersion);
        if (request is null) return NotFound("The record was changed by another user. Refresh the data.");

        db.Entry(request).Property(x => x.RowVersion).OriginalValue = rowVersion;

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

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict("The record was changed by another user. Refresh the data.");
        }

        return Ok();
    }
}
