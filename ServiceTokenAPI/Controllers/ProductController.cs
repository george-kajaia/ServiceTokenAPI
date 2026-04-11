using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServiceTokenApi.DBContext;
using ServiceTokenApi.Entities;
using System.Net.Mime;

namespace ServiceTokenApi.Controllers;

[ApiController]
[Route("api/[controller]")]
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

    // ─── Pictogram endpoints ────────────────────────────────────────────────

    [HttpPost("{productId}/Pictogram")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> AddPictogram(long productId, IFormFile pictogram)
    {
        var product = await db.Products.FirstOrDefaultAsync(x => x.Id == productId);
        if (product is null) return NotFound("Product not found.");

        var alreadyExists = await db.ProductPictograms
            .AnyAsync(p => p.ProductId == productId);
        if (alreadyExists)
            return Conflict("A pictogram already exists for this product. Use PUT to update it.");

        using var ms = new MemoryStream();
        await pictogram.CopyToAsync(ms);

        var entity = new ProductPictogram
        {
            ProductId = productId,
            Pictogram = ms.ToArray()
        };

        db.ProductPictograms.Add(entity);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetPictogram), new { productId }, null);
    }

    [HttpPut("{productId}/Pictogram")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UpdatePictogram(long productId, IFormFile pictogram)
    {
        var entity = await db.ProductPictograms
            .FirstOrDefaultAsync(p => p.ProductId == productId);
        if (entity is null) return NotFound("No pictogram found for this product. Use POST to add one.");

        using var ms = new MemoryStream();
        await pictogram.CopyToAsync(ms);

        entity.Pictogram = ms.ToArray();
        await db.SaveChangesAsync();

        return NoContent();
    }

    [HttpGet("{productId}/Pictogram")]
    public async Task<IActionResult> GetPictogram(long productId)
    {
        var entity = await db.ProductPictograms
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.ProductId == productId);
        if (entity is null) return NotFound("No pictogram found for this product.");

        return File(entity.Pictogram, "image/png");
    }
}
