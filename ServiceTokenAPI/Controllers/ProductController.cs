using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServiceTokenApi.DBContext;
using ServiceTokenApi.Dto;
using ServiceTokenApi.Entities;
using ServiceTokenApi.Enums;
using System.ComponentModel.Design;
using System.Net.Mime;
using System.Xml.Linq;

namespace ServiceTokenApi.Controllers;

[ApiController]
[Route("servicetoken/api/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
public class ProductController(ServiceTokenDbContext db) : ControllerBase
{
    [HttpGet("GetAll")]
    public async Task<ActionResult<IEnumerable<Product>>> GetAll(
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50,
        [FromQuery] string? search = null)
    {
        take = Math.Clamp(take, 1, 200);

        var q = db.Products.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            q = q.Where(c => EF.Functions.ILike(c.Name, $"%{s}%"));
        }

        var items = await q
            .OrderBy(c => c.Id)
            .Skip(skip)
            .Take(take)
            .Select(item => item)
            .ToListAsync();

        return Ok(items);
    }

    [HttpGet("GetById/{productId}")]
    public async Task<IActionResult> GetById(long productId)
    {
        var product = await db.Products.FirstOrDefaultAsync(x => x.Id == productId);
        if (product is null) return NotFound();

        return Ok(product);
    }

    [HttpPost("Create")]
    public async Task<IActionResult> Create([FromBody] Product product)
    {
        db.Products.Add(product);

        await db.SaveChangesAsync();

        return Ok();
    }

    [HttpPut("Update")]
    public async Task<IActionResult> Update(int ProductId, [FromBody] Product newProduct)
    {
        var product = await db.Products.FirstOrDefaultAsync(x => x.Id == ProductId);
        if (product is null) return NotFound();

        product.Name = newProduct.Name;
        product.ServiceCount = newProduct.ServiceCount;        
        product.Price = newProduct.Price;
        product.Term = newProduct.Term;
        product.ScheduleType = newProduct.ScheduleType;

        await db.SaveChangesAsync();
        return Ok();
    }

    [HttpDelete("Delete")]
    public async Task<IActionResult> Delete(int productId)
    {
        var product = await db.Products.FirstOrDefaultAsync(x => x.Id == productId);
        if (product is null) return NotFound("Record not found or already deleted.");

        db.Products.Remove(product);

        await db.SaveChangesAsync();

        return NoContent();
    }
}
