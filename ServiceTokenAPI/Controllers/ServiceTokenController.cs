using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic;
using ServiceTokenApi.DBContext;
using ServiceTokenApi.Dto;
using ServiceTokenApi.Entities;
using ServiceTokenApi.Enums;

namespace BondTradingPlatformApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
public class ServiceTokenController(ServiceTokenDbContext db) : ControllerBase
{
    [HttpGet("GetInvestorServiceTokens")]
    public async Task<ActionResult<List<ServiceTokenDto>>> GetInvestorServiceTokens(string investorPublicKey)
    {
        var tokenList = await db.ServiceTokens.AsNoTracking()
            .Where(x => x.OwnerType == OwnerType.Investor && x.OwnerPublicKey == investorPublicKey)
            .Join(
                db.Companies,
                b => b.CompanyId,
                c => c.Id,
                (b, c) => new { Token = b, CompanyName = c.Name }
            )
            .Join(
                db.Products,
                bc => bc.Token.ProductId,
                p => p.Id,
                (bc, p) => new ServiceTokenDto
                {
                    Id = bc.Token.Id,
                    RowVersion = bc.Token.RowVersion,
                    CompanyId = bc.Token.CompanyId,
                    RequestId = bc.Token.RequestId,
                    ProductId = bc.Token.ProductId,
                    ProductName = p.Name,
                    StartDate = bc.Token.StartDate,
                    EndDate = bc.Token.EndDate,
                    Status = bc.Token.Status,
                    Count = bc.Token.Count,
                    ServiceCount = bc.Token.ServiceCount,
                    ScheduleType = bc.Token.ScheduleType,
                    OwnerType = bc.Token.OwnerType,
                    OwnerPublicKey = bc.Token.OwnerPublicKey,
                    CompanyName = bc.CompanyName
                }
            )
            .OrderBy(x => x.CompanyName).ThenBy(x => x.Id).ToListAsync();

        return Ok(tokenList);
    }

    [HttpGet("GetPrimaryMarketServiceTokens")]
    public async Task<ActionResult<List<ServiceTokenDto>>> GetPrimaryMarketServiceTokens(int companyId = -1, int requestId = -1)
    {
        var tokenList = await db.ServiceTokens.AsNoTracking()
            .Where(x => x.OwnerType == OwnerType.Company && x.Status == ServiceTokenStatus.Available &&
            (x.CompanyId == companyId || companyId == -1) &&
            (x.RequestId == requestId || requestId == -1))
            .Join(
                db.Companies,
                b => b.CompanyId,
                c => c.Id,
                (b, c) => new { Token = b, CompanyName = c.Name }
            )
            .Join(
                db.Products,
                bc => bc.Token.ProductId,
                p => p.Id,
                (bc, p) => new ServiceTokenDto
                {
                    Id = bc.Token.Id,
                    RowVersion = bc.Token.RowVersion,
                    CompanyId = bc.Token.CompanyId,
                    RequestId = bc.Token.RequestId,
                    ProductId = bc.Token.ProductId,
                    ProductName = p.Name,
                    StartDate = bc.Token.StartDate,
                    EndDate = bc.Token.EndDate,
                    Status = bc.Token.Status,
                    Count = bc.Token.Count,
                    ServiceCount = bc.Token.ServiceCount,
                    ScheduleType = bc.Token.ScheduleType,
                    OwnerType = bc.Token.OwnerType,
                    OwnerPublicKey = bc.Token.OwnerPublicKey,
                    CompanyName = bc.CompanyName
                }
            )
            .OrderBy(x => x.CompanyName).ThenBy(x => x.Id).ToListAsync();

        return Ok(tokenList);
    }

    [HttpGet("GetSecondaryMarketServiceTokens")]
    public async Task<ActionResult<List<ServiceTokenDto>>> GetSecondaryMarketServiceTokens(string investorPublicKey = "", int companyId = -1, int requestId = -1)
    {
        var tokenList = await db.ServiceTokens.AsNoTracking()
            .Where(x => 
            x.OwnerType == OwnerType.Investor && 
            x.Status == ServiceTokenStatus.Available && 
            (x.OwnerPublicKey != investorPublicKey || investorPublicKey == string.Empty) &&
            (x.CompanyId == companyId || companyId == -1) &&
            (x.RequestId == requestId || requestId == -1))
            .Join(
                db.Companies,
                b => b.CompanyId,
                c => c.Id,
                (b, c) => new { Token = b, CompanyName = c.Name }
            )
            .Join(
                db.Products,
                bc => bc.Token.ProductId,
                p => p.Id,
                (bc, p) => new ServiceTokenDto
                {
                    Id = bc.Token.Id,
                    RowVersion = bc.Token.RowVersion,
                    CompanyId = bc.Token.CompanyId,
                    RequestId = bc.Token.RequestId,
                    ProductId = bc.Token.ProductId,
                    ProductName = p.Name,
                    StartDate = bc.Token.StartDate,
                    EndDate = bc.Token.EndDate,
                    Status = bc.Token.Status,
                    Count = bc.Token.Count,
                    ServiceCount = bc.Token.ServiceCount,
                    ScheduleType = bc.Token.ScheduleType,
                    OwnerType = bc.Token.OwnerType,
                    OwnerPublicKey = bc.Token.OwnerPublicKey,
                    CompanyName = bc.CompanyName
                }
            )
            .OrderBy(x => x.CompanyName).ThenBy(x => x.Id).ToListAsync();

        return Ok(tokenList);
    }

    [HttpPost("BuyPrimaryServiceToken")]
    public async Task<IActionResult> BuyPrimaryServiceToken(string serviceTokenId, uint rowVersion, string investorPublicKey)
    {
        var serviceToken = await db.ServiceTokens.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == serviceTokenId && x.RowVersion == rowVersion && x.Status == ServiceTokenStatus.Available);
        if (serviceToken is null) return NotFound("The record was changed by another user. Refresh the Data.");

        db.Entry(serviceToken).Property(x => x.RowVersion).OriginalValue = rowVersion;

        var term = (await db.Products.Where(x => x.Id == serviceToken.ProductId).Select(x => x.Term).SingleAsync()).GetValueOrDefault();

        var startDate = DateTime.UtcNow.Date;
        var endDate = DateTime.UtcNow.Date.AddMonths(term);

        serviceToken.Status = ServiceTokenStatus.Sold;
        serviceToken.StartDate = startDate;
        serviceToken.EndDate = endDate;
        serviceToken.OwnerType = OwnerType.Investor;
        serviceToken.OwnerPublicKey = investorPublicKey;

        var operation = new Operation
        {
            ServiceTokenId = serviceToken.Id,
            OpType = OpType.BuyPrimary,
            OpDate = DateTime.UtcNow,
            OwnerPublicKey = serviceToken.OwnerPublicKey
        };

        db.ServiceTokens.Update(serviceToken);
        db.Operations.Add(operation);

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

    [HttpPost("MarkServiceTokenForResell")]
    public async Task<IActionResult> MarkServiceTokenForResell(string serviceTokenId, uint rowVersion)
    {
        var serviceToken = await db.ServiceTokens.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == serviceTokenId && x.RowVersion == rowVersion && x.Status == ServiceTokenStatus.Sold);
        if (serviceToken is null) return NotFound("The record was changed by another user. Refresh the Data.");

        db.Entry(serviceToken).Property(x => x.RowVersion).OriginalValue = rowVersion;

        serviceToken.Status = ServiceTokenStatus.Available;

        var operation = new Operation {
            ServiceTokenId = serviceToken.Id,
            OpType = OpType.MarkForResell,
            OpDate = DateTime.UtcNow,
            OwnerPublicKey = serviceToken.OwnerPublicKey
        };

        db.ServiceTokens.Update(serviceToken);
        db.Operations.Add(operation);

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

    [HttpPost("CancelReselling")]
    public async Task<IActionResult> CancelReselling(string serviceTokenId, uint rowVersion)
    {
        var serviceToken = await db.ServiceTokens.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == serviceTokenId && x.RowVersion == rowVersion && x.Status == ServiceTokenStatus.Available);
        if (serviceToken is null) return NotFound("The record was changed by another user. Refresh the Data.");

        db.Entry(serviceToken).Property(x => x.RowVersion).OriginalValue = rowVersion;

        serviceToken.Status = ServiceTokenStatus.Sold;

        var operation = new Operation
        {
            ServiceTokenId = serviceToken.Id,
            OpType = OpType.MarkForResell,
            OpDate = DateTime.UtcNow,
            OwnerPublicKey = serviceToken.OwnerPublicKey
        };

        db.ServiceTokens.Update(serviceToken);
        db.Operations.Add(operation);

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

    [HttpPost("BuySecondaryServiceToken")]
    public async Task<IActionResult> BuySecondaryServiceToken(string serviceTokenId, uint rowVersion, string newInvestorPublicKey)
    {
        var serviceToken = await db.ServiceTokens.AsNoTracking().FirstOrDefaultAsync(x => 
        x.Id == serviceTokenId && 
        x.RowVersion == rowVersion && 
        x.Status == ServiceTokenStatus.Available &&
        x.OwnerType == OwnerType.Investor &&
        x.OwnerPublicKey != newInvestorPublicKey);
        if (serviceToken is null) return NotFound("The record was changed by another user. Refresh the Data.");

        db.Entry(serviceToken).Property(x => x.RowVersion).OriginalValue = rowVersion;

        serviceToken.Status = ServiceTokenStatus.Sold;
        serviceToken.OwnerPublicKey = newInvestorPublicKey;

        var operation = new Operation
        {
            ServiceTokenId = serviceToken.Id,
            OpType = OpType.BuySecondary,
            OpDate = DateTime.UtcNow,
            OwnerPublicKey = serviceToken.OwnerPublicKey
        };

        db.ServiceTokens.Update(serviceToken);
        db.Operations.Add(operation);

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
