using ServiceTokenAPI.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServiceTokenAPI.DBContext;
using System.Net.Mime;
using ServiceTokenAPI.Dto;

namespace ServiceTokenAPI.Controllers;

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
        var c = await db.CompanyUsers.AsNoTracking().FirstOrDefaultAsync(x => x.UserName == loginCredential.userName && x.Password == loginCredential.password);
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
            Name = Company.Name.Trim(),
            Status = 0,
            RegDate = DateTime.UtcNow,
            TaxCode = Company.TaxCode.Trim()
        };

        var companyUser = new CompanyUser { UserName = Company.UserName, Password = hash };

        db.Companies.Add(company);
        db.CompanyUsers.Add(companyUser);
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
    public async Task<ActionResult<Company>> Update(int id, [FromBody] CompanyRequestDto Company)
    {
        var c = await db.Companies.FirstOrDefaultAsync(x => x.Id == id);
        if (c is null) return NotFound();

        c.Name = Company.Name.Trim();
        c.TaxCode = Company.TaxCode.Trim();

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
    public async Task<ActionResult<Company>> Approve(int CompanyId)
    {
        var c = await db.Companies.FirstOrDefaultAsync(x => x.Id == CompanyId);
        if (c is null) return NotFound();

        c.Status = 1;

        await db.SaveChangesAsync();

        return Ok(c);
    }

    [HttpDelete("delete")]
    public async Task<IActionResult> Delete(int id)
    {
        var c = await db.Companies.FirstOrDefaultAsync(x => x.Id == id);
        if (c is null) return NotFound();

        db.Companies.Remove(c);
        await db.SaveChangesAsync();
        return NoContent();
    }
}
