using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServiceTokenApi.DBContext;
using ServiceTokenApi.Dto;
using ServiceTokenApi.Entities;
using System.Net;
using System.Net.Mime;
using System.Numerics;
using System.Xml.Linq;

namespace ServiceTokenApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
public class CompanyController(ServiceTokenDbContext db) : ControllerBase
{
    [HttpGet("GetAll")]
    public async Task<ActionResult<IEnumerable<Company>>> GetAll(
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50,
        [FromQuery] string? search = null)
    {
        take = Math.Clamp(take, 1, 200);

        var q = db.Companies.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            q = q.Where(c => EF.Functions.ILike(c.Name, $"%{s}%")
                           || EF.Functions.ILike(c.TaxCode, $"%{s}%"));
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
    public async Task<ActionResult<Company>> GetById(int id)
    {
        var c = await db.Companies.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (c is null) return NotFound();

        return Ok(c);
    }

    [HttpPost("Login")]
    public async Task<ActionResult<CompanyUser>> Login([FromBody] LoginCredentialDto loginCredential)
    {
        var c = await db.CompanyUsers.AsNoTracking().FirstOrDefaultAsync(x => x.UserName == loginCredential.UserName && x.Password == loginCredential.Password);
        if (c is null)
        {
            return NotFound();
        }

        return Ok(c);
    }

    [HttpPost("create")]
    public async Task<ActionResult<Company>> Create([FromBody] CompanyRequestDto Company)
    {
        if (string.IsNullOrWhiteSpace(Company.Password))
            return BadRequest("Password is required.");

        // hash password
        var hash = Company.Password; // BCrypt.Net.BCrypt.HashPassword(Company.Password, workFactor: 12);

        var company = new Company
        {
            Status = 0,
            RegDate = DateTime.UtcNow,

            Name = Company.Name.Trim(),
            TaxCode = Company.TaxCode.Trim(),
            Address = Company.Address.Trim(),
            LegalForm = Company.LegalForm,
            EconomicActivity = Company.EconomicActivity,
            Mail = Company.Mail.Trim(),
            Phone = Company.Phone.Trim(),

            User = new CompanyUser { UserName = Company.UserName, Password = hash }
        };

        db.Companies.Add(company);
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("duplicate") == true)
        {
            return Conflict("UserName (or another unique field) already exists.");
        }

        return Ok(Company);
    }

    [HttpPut("update")]
    public async Task<ActionResult<Company>> Update(int companyId, uint rowVersion, [FromBody] CompanyRequestDto Company)
    {
        var c = await db.Companies.FirstOrDefaultAsync(x => x.Id == companyId && x.RowVersion == rowVersion);
        if (c is null) return NotFound("The record was changed by another user. Refresh the data.");

        db.Entry(c).Property(x => x.RowVersion).OriginalValue = rowVersion;

        c.Name = Company.Name.Trim();
        c.TaxCode = Company.TaxCode.Trim();
        c.Address = Company.Address.Trim();
        c.LegalForm = Company.LegalForm;
        c.EconomicActivity = Company.EconomicActivity;
        c.Mail = Company.Mail.Trim();
        c.Phone = Company.Phone.Trim();

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict("The record was changed by another user. Refresh the data.");
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("duplicate") == true)
        {
            return Conflict("UserName (or another unique field) already exists.");
        }

        return Ok(c);
    }

    [HttpPatch("Approve")]
    public async Task<IActionResult> Approve(int companyId, uint rowVersion)
    {
        var c = await db.Companies.FirstOrDefaultAsync(x => x.Id == companyId && x.RowVersion == rowVersion);
        if (c is null) return NotFound("The record was changed by another user. Refresh the data.");

        db.Entry(c).Property(x => x.RowVersion).OriginalValue = rowVersion;

        c.Status = 1;

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

    [HttpDelete("delete")]
    public async Task<IActionResult> Delete(int companyId, uint rowVersion)
    {
        var c = await db.Companies.FirstOrDefaultAsync(x => x.Id == companyId && x.RowVersion == rowVersion);
        if (c is null) return NotFound("Record not found or already deleted.");

        db.Entry(c).Property(x => x.RowVersion).OriginalValue = rowVersion;

        db.Companies.Remove(c);

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
}
