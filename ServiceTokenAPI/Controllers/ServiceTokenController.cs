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
        var bondList = await db.ServiceTokens.AsNoTracking()
            .Where(x => x.OwnerType == OwnerType.Investor && x.OwnerPublicKey == investorPublicKey)
            .Join(
            db.Companies,
            b => b.CompanyId,
            c => c.Id,
            (b, c) => new ServiceTokenDto
            {
                Id = b.Id,
                RowVersion = b.RowVersion,
                CompanyId = b.CompanyId,
                RequestId = b.RequestId,
                ProdId  = b.ProdId,
                StartDate  = b.StartDate,
                EndDate  = b.EndDate,
                Status  = b.Status,
                Count  = b.Count,
                ServiceCount  = b.ServiceCount,
                ScheduleType  = b.ScheduleType,
                OwnerType  = b.OwnerType,
                OwnerPublicKey  = b.OwnerPublicKey,
                CompanyName = c.Name
            }
            )
            .OrderBy(x => x.CompanyName).ThenBy(x => x.Id).ToListAsync();

        return Ok(bondList);
    }

    [HttpGet("GetPrimaryMarketServiceTokens")]
    public async Task<ActionResult<List<ServiceTokenDto>>> GetPrimaryMarketServiceTokens(int companyId = -1, int requestId = -1)
    {
        var bondList = await db.ServiceTokens.AsNoTracking()
            .Where(x => x.OwnerType == OwnerType.Company && x.Status == ServiceTokenStatus.Available &&
            (x.CompanyId == companyId || companyId == -1) &&
            (x.RequestId == requestId || requestId == -1))
            .Join(
            db.Companies,
            b => b.CompanyId,
            c => c.Id,
            (b, c) => new ServiceTokenDto
            {
                Id = b.Id,
                RowVersion = b.RowVersion,
                CompanyId = b.CompanyId,
                RequestId = b.RequestId,
                ProdId = b.ProdId,
                StartDate = b.StartDate,
                EndDate = b.EndDate,
                Status = b.Status,
                Count = b.Count,
                ServiceCount = b.ServiceCount,
                ScheduleType = b.ScheduleType,
                OwnerType = b.OwnerType,
                OwnerPublicKey = b.OwnerPublicKey,
                CompanyName = c.Name
            }
            )
            .OrderBy(x => x.CompanyName).ThenBy(x => x.Id).ToListAsync();

        return Ok(bondList);
    }

    [HttpGet("GetSecondaryMarketServiceTokens")]
    public async Task<ActionResult<List<ServiceTokenDto>>> GetSecondaryMarketServiceTokens(string investorPublicKey, int companyId = -1, int requestId = -1)
    {
        var bondList = await db.ServiceTokens.AsNoTracking()
            .Where(x => 
            x.OwnerType == OwnerType.Investor && 
            x.Status == ServiceTokenStatus.Available && 
            x.OwnerPublicKey != investorPublicKey &&
            (x.CompanyId == companyId || companyId == -1) &&
            (x.RequestId == requestId || requestId == -1))
            .Join(
            db.Companies,
            b => b.CompanyId,
            c => c.Id,
            (b, c) => new ServiceTokenDto
            {
                Id = b.Id,
                RowVersion = b.RowVersion,
                CompanyId = b.CompanyId,
                RequestId = b.RequestId,
                ProdId = b.ProdId,
                StartDate = b.StartDate,
                EndDate = b.EndDate,
                Status = b.Status,
                Count = b.Count,
                ServiceCount = b.ServiceCount,
                ScheduleType = b.ScheduleType,
                OwnerType = b.OwnerType,
                OwnerPublicKey = b.OwnerPublicKey,
                CompanyName = c.Name
            }
            )
            .OrderBy(x => x.CompanyName).ThenBy(x => x.Id).ToListAsync();

        return Ok(bondList);
    }

    [HttpPost("BuyPrimaryServiceToken")]
    public async Task<ActionResult> BuyPrimaryServiceToken(string serviceTokenId, DateTime rowVersion, string investorPublicKey)
    {
        var serviceToken = await db.ServiceTokens.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == serviceTokenId && x.RowVersion == rowVersion && x.Status == ServiceTokenStatus.Available);
        if (serviceToken is null) return NotFound("The record is changed. Refresh the Data.");

        var term = (await db.Products.Where(x => x.Id == serviceToken.ProdId).Select(x => x.Term).SingleAsync()).GetValueOrDefault();

        var startDate = DateTime.UtcNow.Date;
        var endDate = DateTime.UtcNow.Date.AddMonths(term);

        serviceToken.RowVersion = DateTime.UtcNow;
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

        await db.SaveChangesAsync();

        return Ok();
    }

    [HttpPost("MarkServiceTokenForResell")]
    public async Task<ActionResult> MarkServiceTokenForResell(string serviceTokenId, DateTime rowVersion)
    {
        var serviceToken = await db.ServiceTokens.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == serviceTokenId && x.RowVersion == rowVersion && x.Status == ServiceTokenStatus.Sold);
        if (serviceToken is null) return NotFound("The record is changed. Refresh the Data.");

        serviceToken.Status = ServiceTokenStatus.Available;

        var operation = new Operation {
            ServiceTokenId = serviceToken.Id,
            OpType = OpType.MarkForResell,
            OpDate = DateTime.UtcNow,
            OwnerPublicKey = serviceToken.OwnerPublicKey
        };

        db.ServiceTokens.Update(serviceToken);
        db.Operations.Add(operation);
        await db.SaveChangesAsync();

        return Ok();
    }

    [HttpPost("CancelReselling")]
    public async Task<ActionResult> CancelReselling(string serviceTokenId, DateTime rowVersion)
    {
        var serviceToken = await db.ServiceTokens.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == serviceTokenId && x.RowVersion == rowVersion && x.Status == ServiceTokenStatus.Available);
        if (serviceToken is null) return NotFound("The record is changed. Refresh the Data.");

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
        await db.SaveChangesAsync();

        return Ok();
    }

    [HttpPost("BuySecondaryServiceToken")]
    public async Task<ActionResult> BuySecondaryServiceToken(string serviceTokenId, DateTime rowVersion, string newInvestorPublicKey)
    {
        var serviceToken = await db.ServiceTokens.AsNoTracking().FirstOrDefaultAsync(x => 
        x.Id == serviceTokenId && 
        x.RowVersion == rowVersion && 
        x.Status == ServiceTokenStatus.Available &&
        x.OwnerType == OwnerType.Investor &&
        x.OwnerPublicKey != newInvestorPublicKey);
        if (serviceToken is null) return NotFound("The record is changed. Refresh the Data.");


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
        await db.SaveChangesAsync();

        return Ok();
    }
}
