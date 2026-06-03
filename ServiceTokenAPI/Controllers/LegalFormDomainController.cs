using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServiceTokenApi.DBContext;
using ServiceTokenApi.Entities;
using System.Net.Mime;

namespace ServiceTokenApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
public class LegalFormDomainController(ServiceTokenDbContext db) : ControllerBase
{
    [HttpGet("GetAll")]
    public async Task<ActionResult<IEnumerable<LegalFormDomain>>> GetAll()
    {
        var items = await db.LegalFormDomain.AsNoTracking().OrderBy(c => c.Id).ToListAsync();

        return Ok(items);
    }

    [HttpPost("Create")]
    public async Task<IActionResult> Create([FromBody] LegalFormDomain item)
    {
        db.LegalFormDomain.Add(item);
        await db.SaveChangesAsync();
        return Ok();
    }

    [HttpPut("Update")]
    public async Task<IActionResult> Update(int Id, [FromBody] LegalFormDomain newItem)
    {
        var item = await db.LegalFormDomain.FirstOrDefaultAsync(x => x.Id == Id);
        if (item is null) return NotFound();

        item.Name = newItem.Name;

        await db.SaveChangesAsync();
        return Ok();
    }

    [HttpDelete("Delete")]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await db.LegalFormDomain.FirstOrDefaultAsync(x => x.Id == id);
        if (item is null) return NotFound("Record not found or already deleted.");

        db.LegalFormDomain.Remove(item);
        await db.SaveChangesAsync();
        return NoContent();
    }

}
