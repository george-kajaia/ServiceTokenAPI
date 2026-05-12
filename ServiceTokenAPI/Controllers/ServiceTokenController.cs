using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic;
using ServiceTokenApi.DBContext;
using ServiceTokenApi.Dto;
using ServiceTokenApi.Entities;
using ServiceTokenApi.Enums;
using ServiceTokenApi.Hubs;

namespace ServiceTokenApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
public class ServiceTokenController(
    ServiceTokenDbContext db,
    IHubContext<RedemptionHub> hub) : ControllerBase
{
    //--Get Tokens
    [HttpGet("GetInvestorServiceTokens")]
    public async Task<ActionResult<List<ServiceTokenDto>>> GetInvestorServiceTokens(string investorPublicKey)
    {
        var tokenList = await db.ServiceTokens.AsNoTracking()
            .Where(x => x.OwnerType == OwnerType.Investor && x.OwnerPublicKey == investorPublicKey)
            .Join(
                db.Companies,
                token => token.CompanyId,
                c => c.Id,
                (token, c) => new { Token = token, CompanyName = c.Name }
            )
            .Join(
                db.Products,
                tc => tc.Token.ProductId,
                p => p.Id,
                (tc, p) => new { tc.Token, tc.CompanyName, Product = p }
            )
            .Join(
                db.ProductPictograms,
                tc => tc.Product.Id,
                p => p.ProductId,
                (tc, p) => new ServiceTokenDto
                {
                    Id = tc.Token.Id,
                    RowVersion = tc.Token.RowVersion,
                    CompanyId = tc.Token.CompanyId,
                    RequestId = tc.Token.RequestId,
                    ProductId = tc.Token.ProductId,
                    ProductName = tc.Product.Name,
                    StartDate = tc.Token.StartDate,
                    EndDate = tc.Token.EndDate,
                    Status = tc.Token.Status,
                    RemainingCount = tc.Token.RemainingCount,
                    ServiceCount = tc.Token.ServiceCount,
                    ScheduleType = tc.Token.ScheduleType,
                    OwnerType = tc.Token.OwnerType,
                    OwnerPublicKey = tc.Token.OwnerPublicKey,
                    CompanyName = tc.CompanyName,
                    Pictogram = p.Pictogram
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
                token => token.CompanyId,
                c => c.Id,
                (token, c) => new { Token = token, CompanyName = c.Name }
            )
            .Join(
                db.Products,
                tc => tc.Token.ProductId,
                p => p.Id,
                (tc, p) => new { tc.Token, tc.CompanyName, Product = p }
            )
            .Join(
                db.ProductPictograms,
                tc => tc.Product.Id,
                p => p.ProductId,
                (tc, p) => new ServiceTokenDto
                {
                    Id = tc.Token.Id,
                    RowVersion = tc.Token.RowVersion,
                    CompanyId = tc.Token.CompanyId,
                    RequestId = tc.Token.RequestId,
                    ProductId = tc.Token.ProductId,
                    ProductName = tc.Product.Name,
                    StartDate = tc.Token.StartDate,
                    EndDate = tc.Token.EndDate,
                    Status = tc.Token.Status,
                    RemainingCount = tc.Token.RemainingCount,
                    ServiceCount = tc.Token.ServiceCount,
                    ScheduleType = tc.Token.ScheduleType,
                    OwnerType = tc.Token.OwnerType,
                    OwnerPublicKey = tc.Token.OwnerPublicKey,
                    CompanyName = tc.CompanyName,
                    Pictogram = p.Pictogram
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
                token => token.CompanyId,
                c => c.Id,
                (token, c) => new { Token = token, CompanyName = c.Name }
            )
            .Join(
                db.Products,
                tc => tc.Token.ProductId,
                p => p.Id,
                (tc, p) => new { tc.Token, tc.CompanyName, Product = p }
            )
            .Join(
                db.ProductPictograms,
                tc => tc.Product.Id,
                p => p.ProductId,
                (tc, p) => new ServiceTokenDto
                {
                    Id = tc.Token.Id,
                    RowVersion = tc.Token.RowVersion,
                    CompanyId = tc.Token.CompanyId,
                    RequestId = tc.Token.RequestId,
                    ProductId = tc.Token.ProductId,
                    ProductName = tc.Product.Name,
                    StartDate = tc.Token.StartDate,
                    EndDate = tc.Token.EndDate,
                    Status = tc.Token.Status,
                    RemainingCount = tc.Token.RemainingCount,
                    ServiceCount = tc.Token.ServiceCount,
                    ScheduleType = tc.Token.ScheduleType,
                    OwnerType = tc.Token.OwnerType,
                    OwnerPublicKey = tc.Token.OwnerPublicKey,
                    CompanyName = tc.CompanyName,
                    Pictogram = p.Pictogram
                }
            )
            .OrderBy(x => x.CompanyName).ThenBy(x => x.Id).ToListAsync();

        return Ok(tokenList);
    }

    //--Put Into Cart
    [HttpPost("MarkServiceTokenInCart")]
    public async Task<ActionResult<ServiceTokenDto>> MarkServiceTokenInCart(string serviceTokenId, uint rowVersion, string investorPublicKey)
    {
        var serviceToken = await db.ServiceTokens.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == serviceTokenId && x.RowVersion == rowVersion && x.Status == ServiceTokenStatus.Available);
        if (serviceToken is null) return NotFound("The record was changed by another user. Refresh the Data.");

        db.Entry(serviceToken).Property(x => x.RowVersion).OriginalValue = rowVersion;

        serviceToken.Status = ServiceTokenStatus.InCart;

        db.ServiceTokens.Update(serviceToken);

        var operation = new Operation
        {
            ServiceTokenId = serviceToken.Id,
            OpType = OpType.MarkInCart,
            OpDate = DateTime.UtcNow,
            OwnerPublicKey = serviceToken.OwnerPublicKey
        };
        
        db.Operations.Add(operation);

        var serviceTokenInCart = new ServiceTokenInCart
        {
            ServiceTokenId = serviceToken.Id,
            OwnerPublicKey = investorPublicKey
        };

        db.ServiceTokenInCart.Add(serviceTokenInCart);

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict("The record was changed by another user. Refresh the data.");
        }

        var tokenDto = await db.ServiceTokens.AsNoTracking()
            .Where(x => x.Id == serviceToken.Id)
            .Join(
                db.Companies,
                token => token.CompanyId,
                c => c.Id,
                (token, c) => new { Token = token, CompanyName = c.Name }
            )
            .Join(
                db.Products,
                tc => tc.Token.ProductId,
                p => p.Id,
                (tc, p) => new { tc.Token, tc.CompanyName, Product = p }
            )
            .Join(
                db.ProductPictograms,
                tc => tc.Product.Id,
                p => p.ProductId,
                (tc, p) => new ServiceTokenDto
                {
                    Id = tc.Token.Id,
                    RowVersion = tc.Token.RowVersion,
                    CompanyId = tc.Token.CompanyId,
                    RequestId = tc.Token.RequestId,
                    ProductId = tc.Token.ProductId,
                    ProductName = tc.Product.Name,
                    StartDate = tc.Token.StartDate,
                    EndDate = tc.Token.EndDate,
                    Status = tc.Token.Status,
                    RemainingCount = tc.Token.RemainingCount,
                    ServiceCount = tc.Token.ServiceCount,
                    ScheduleType = tc.Token.ScheduleType,
                    OwnerType = tc.Token.OwnerType,
                    OwnerPublicKey = tc.Token.OwnerPublicKey,
                    CompanyName = tc.CompanyName,
                    Pictogram = p.Pictogram
                }
            )
            .OrderBy(x => x.CompanyName).ThenBy(x => x.Id).FirstOrDefaultAsync();

        return Ok(tokenDto);
    }

    [HttpPost("CancelInCart")]
    public async Task<ActionResult<ServiceTokenDto>> CancelInCart(string serviceTokenId, uint rowVersion)
    {
        var serviceToken = await db.ServiceTokens.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == serviceTokenId && x.RowVersion == rowVersion && x.Status == ServiceTokenStatus.Available);
        if (serviceToken is null) return NotFound("The record was changed by another user. Refresh the Data.");

        db.Entry(serviceToken).Property(x => x.RowVersion).OriginalValue = rowVersion;

        serviceToken.Status = ServiceTokenStatus.Available;

        db.ServiceTokens.Update(serviceToken);

        var operation = new Operation
        {
            ServiceTokenId = serviceToken.Id,
            OpType = OpType.CancelInCart,
            OpDate = DateTime.UtcNow,
            OwnerPublicKey = serviceToken.OwnerPublicKey
        };

        db.Operations.Add(operation);

        var serviceTokenInCart = await db.ServiceTokenInCart.AsNoTracking().FirstOrDefaultAsync(x => x.ServiceTokenId == serviceToken.Id);
        if (serviceTokenInCart is null) return NotFound("The record was changed by another user. Refresh the Data.");

        db.ServiceTokenInCart.Remove(serviceTokenInCart);

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict("The record was changed by another user. Refresh the data.");
        }

        var tokenDto = await db.ServiceTokens.AsNoTracking()
            .Where(x => x.Id == serviceToken.Id)
            .Join(
                db.Companies,
                token => token.CompanyId,
                c => c.Id,
                (token, c) => new { Token = token, CompanyName = c.Name }
            )
            .Join(
                db.Products,
                tc => tc.Token.ProductId,
                p => p.Id,
                (tc, p) => new { tc.Token, tc.CompanyName, Product = p }
            )
            .Join(
                db.ProductPictograms,
                tc => tc.Product.Id,
                p => p.ProductId,
                (tc, p) => new ServiceTokenDto
                {
                    Id = tc.Token.Id,
                    RowVersion = tc.Token.RowVersion,
                    CompanyId = tc.Token.CompanyId,
                    RequestId = tc.Token.RequestId,
                    ProductId = tc.Token.ProductId,
                    ProductName = tc.Product.Name,
                    StartDate = tc.Token.StartDate,
                    EndDate = tc.Token.EndDate,
                    Status = tc.Token.Status,
                    RemainingCount = tc.Token.RemainingCount,
                    ServiceCount = tc.Token.ServiceCount,
                    ScheduleType = tc.Token.ScheduleType,
                    OwnerType = tc.Token.OwnerType,
                    OwnerPublicKey = tc.Token.OwnerPublicKey,
                    CompanyName = tc.CompanyName,
                    Pictogram = p.Pictogram
                }
            )
            .OrderBy(x => x.CompanyName).ThenBy(x => x.Id).FirstOrDefaultAsync();

        return Ok(tokenDto);
    }

    [HttpGet("GetInvestorServiceTokensInCart")]
    public async Task<ActionResult<ServiceTokenDto>> GetInvestorServiceTokensInCart(string investorPublicKey)
    {
        var tokenList = await db.ServiceTokenInCart.AsNoTracking()
            .Where(x => x.OwnerPublicKey == investorPublicKey)
            .Join(
                db.ServiceTokens,
                tokenInCart => tokenInCart.ServiceTokenId,
                token => token.Id,                
                (tokenInCart, token) => token
            )
            .Join(
                db.Companies,
                token => token.CompanyId,
                c => c.Id,
                (token, c) => new { Token = token, CompanyName = c.Name }
            )
            .Join(
                db.Products,
                tc => tc.Token.ProductId,
                p => p.Id,
                (tc, p) => new { tc.Token, tc.CompanyName, Product = p }
            )
            .Join(
                db.ProductPictograms,
                tc => tc.Product.Id,
                p => p.ProductId,
                (tc, p) => new ServiceTokenDto
                {
                    Id = tc.Token.Id,
                    RowVersion = tc.Token.RowVersion,
                    CompanyId = tc.Token.CompanyId,
                    RequestId = tc.Token.RequestId,
                    ProductId = tc.Token.ProductId,
                    ProductName = tc.Product.Name,
                    StartDate = tc.Token.StartDate,
                    EndDate = tc.Token.EndDate,
                    Status = tc.Token.Status,
                    RemainingCount = tc.Token.RemainingCount,
                    ServiceCount = tc.Token.ServiceCount,
                    ScheduleType = tc.Token.ScheduleType,
                    OwnerType = tc.Token.OwnerType,
                    OwnerPublicKey = tc.Token.OwnerPublicKey,
                    CompanyName = tc.CompanyName,
                    Pictogram = p.Pictogram
                }
            )
            .OrderBy(x => x.CompanyName).ThenBy(x => x.Id).ToListAsync();

        return Ok(tokenList);
    }

    //--Buy Tokens
    [HttpPost("BuyPrimaryServiceToken")]
    public async Task<IActionResult> BuyPrimaryServiceToken(string serviceTokenId, uint rowVersion, string investorPublicKey)
    {
        var serviceToken = await db.ServiceTokens.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == serviceTokenId && x.RowVersion == rowVersion && x.Status == ServiceTokenStatus.InCart);
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

        var serviceTokenInCart = await db.ServiceTokenInCart.AsNoTracking().FirstOrDefaultAsync(x => x.ServiceTokenId == serviceToken.Id);
        if (serviceTokenInCart is null) return NotFound("The record was changed by another user. Refresh the Data.");

        db.ServiceTokenInCart.Remove(serviceTokenInCart);

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
        x.Status == ServiceTokenStatus.InCart &&
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

        var serviceTokenInCart = await db.ServiceTokenInCart.AsNoTracking().FirstOrDefaultAsync(x => x.ServiceTokenId == serviceToken.Id);
        if (serviceTokenInCart is null) return NotFound("The record was changed by another user. Refresh the Data.");

        db.ServiceTokenInCart.Remove(serviceTokenInCart);

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

        Operation lastOperation = await db.Operations.AsNoTracking()
            .Where(x => x.ServiceTokenId == serviceTokenId && x.OpType == OpType.MarkForResell).OrderBy(x => x.OpDate)
            .OrderBy(x => x.OpDate)
            .LastAsync();

        serviceToken.Status = ServiceTokenStatus.Sold;
        serviceToken.OwnerPublicKey = lastOperation.OwnerPublicKey;

        var operation = new Operation
        {
            ServiceTokenId = serviceToken.Id,
            OpType = OpType.CancelReselling,
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

    /// <summary>
    /// Called by the company-side scanner after reading the investor's QR code.
    /// Validates the token, decrements Count (marking as Finished when it reaches 0),
    /// logs a GetService operation, then pushes the result back to the investor's
    /// open SignalR connection identified by connectionId.
    /// </summary>
    [HttpPost("GetService")]
    public async Task<IActionResult> GetService(string serviceTokenId, uint rowVersion, string connectionId)
    {
        var serviceToken = await db.ServiceTokens
            .FirstOrDefaultAsync(x =>
                x.Id == serviceTokenId &&
                x.RowVersion == rowVersion &&
                x.Status == ServiceTokenStatus.Sold &&
                x.OwnerType == OwnerType.Investor);

        if (serviceToken is null)
        {
            await hub.Clients.Client(connectionId).SendAsync("ServiceResult", new
            {
                success = false,
                message = "Token not found or already used."
            });
            return NotFound("Token not found or already used.");
        }

        if (serviceToken.RemainingCount <= 0)
        {
            await hub.Clients.Client(connectionId).SendAsync("ServiceResult", new
            {
                success = false,
                message = "No service uses remaining on this token."
            });
            return BadRequest("No service uses remaining on this token.");
        }

        db.Entry(serviceToken).Property(x => x.RowVersion).OriginalValue = rowVersion;

        serviceToken.RemainingCount -= 1;

        if (serviceToken.RemainingCount == 0)
            serviceToken.Status = ServiceTokenStatus.Finished;

        var operation = new Operation
        {
            ServiceTokenId = serviceToken.Id,
            OpType = OpType.GetService,
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
            await hub.Clients.Client(connectionId).SendAsync("ServiceResult", new
            {
                success = false,
                message = "The token was modified concurrently. Please try again."
            });
            return Conflict("The record was changed by another user. Refresh the data.");
        }

        await hub.Clients.Client(connectionId).SendAsync("ServiceResult", new
        {
            success = true,
            message = "Service granted successfully.",
            count = serviceToken.RemainingCount,
            rowVersion = serviceToken.RowVersion
        });

        return Ok();
    }
}
