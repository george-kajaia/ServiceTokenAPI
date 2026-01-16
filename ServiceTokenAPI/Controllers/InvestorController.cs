using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServiceTokenApi.DBContext;
using ServiceTokenApi.Dto;
using ServiceTokenApi.Entities;
using System.Net.Mime;

namespace ServiceTokenApi.Controllers;

[ApiController]
[Route("servicetoken/api/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
public class InvestorController(ServiceTokenDbContext db) : ControllerBase
{
    [HttpGet("GetAll")]
    public async Task<ActionResult<IEnumerable<Investor>>> GetAll(
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50,
        [FromQuery] string? search = null)
    {
        take = Math.Clamp(take, 1, 200);

        var q = db.Investors.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            q = q.Where(c => EF.Functions.ILike(c.PublicKey, $"%{s}%")
                           || EF.Functions.ILike(c.UserName, $"%{s}%"));
        }

        var items = await q
            .OrderBy(c => c.Id)
            .Skip(skip)
            .Take(take)
            .Select(item => item)
            .ToListAsync();

        return Ok(items);
    }

    [HttpGet("GetById/{id}")]
    public async Task<ActionResult<Investor>> GetById(int id)
    {
        var c = await db.Investors.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (c is null) return NotFound();

        return Ok(c);
    }

    [HttpPost("Login")]
    public async Task<ActionResult<Investor>> Login([FromBody] LoginCredentialDto loginCredential)
    {
        var c = await db.Investors.AsNoTracking().FirstOrDefaultAsync(x => x.UserName == loginCredential.userName && x.Password == loginCredential.password);
        if (c is null)
        {
            return NotFound();
        }

        return Ok(c);
    }

    [HttpPost("create")]
    public async Task<ActionResult<Investor>> Create([FromBody] Investor investor)
    {
        if (string.IsNullOrWhiteSpace(investor.Password))
            return BadRequest("Password is required.");

        // hash password
        var hash = investor.Password; // BCrypt.Net.BCrypt.HashPassword(investor.Password, workFactor: 12);

        var c = new Investor
        {
            PublicKey = investor.PublicKey.Trim(),
            UserName = investor.UserName.Trim(),
            Password = hash
        };

        db.Investors.Add(c);
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("duplicate") == true)
        {
            return Conflict("UserName (or another unique field) already exists.");
        }

        return Ok(investor);
    }

    [HttpPut("update")]
    public async Task<ActionResult<Investor>> Update(int id, DateTime rowVersion, [FromBody] Investor investor)
    {
        var c = await db.Investors.FirstOrDefaultAsync(x => x.Id == id && x.RowVersion == rowVersion);
        if (c is null) return NotFound("The record is changed. Refresh the Data.");

        c.RowVersion = DateTime.UtcNow;
        c.Status = 0;
        c.PublicKey = investor.PublicKey.Trim();
        c.UserName = investor.UserName.Trim();

        if (!string.IsNullOrWhiteSpace(investor.Password))
        {
            c.Password = investor.Password; // BCrypt.Net.BCrypt.HashPassword(investor.Password, workFactor: 12);
        }

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("duplicate") == true)
        {
            return Conflict("UserName (or another unique field) already exists.");
        }

        return Ok(c);
    }

    [HttpPatch("Approve")]
    public async Task<ActionResult<Investor>> Approve(int investorId, DateTime rowVersion)
    {
        var c = await db.Investors.FirstOrDefaultAsync(x => x.Id == investorId && x.RowVersion == rowVersion);
        if (c is null) return NotFound("The record is changed. Refresh the Data.");

        c.RowVersion = rowVersion;
        c.Status = 1;

        await db.SaveChangesAsync();

        return Ok(c);
    }

    [HttpDelete("delete")]
    public async Task<IActionResult> Delete(int id)
    {
        var c = await db.Investors.FirstOrDefaultAsync(x => x.Id == id);
        if (c is null) return NotFound();

        db.Investors.Remove(c);
        await db.SaveChangesAsync();
        return NoContent();
    }
}
