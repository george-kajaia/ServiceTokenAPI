using ServiceTokenApi.Dto;
using ServiceTokenApi.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServiceTokenApi.DBContext;
using System.Net.Mime;

namespace ServiceTokenApi.Controllers;

[ApiController]
[Route("servicetoken/api/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
public class UserController(ServiceTokenDbContext db) : ControllerBase
{
    [HttpGet("GetAll")]
    public async Task<ActionResult<IEnumerable<User>>> GetAll(
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50,
        [FromQuery] string? search = null)
    {
        take = Math.Clamp(take, 1, 200);

        var q = db.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            q = q.Where(c => EF.Functions.ILike(c.UserName, $"%{s}%")
                           || EF.Functions.ILike(c.UserFullName, $"%{s}%"));
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
    public async Task<ActionResult<User>> GetById(int id)
    {
        var c = await db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (c is null) return NotFound();

        return Ok(c);
    }

    [HttpPost("Login")]
    public async Task<ActionResult<User>> Login([FromBody] LoginCredentialDto loginCredential)
    {
        var c = await db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.UserName == loginCredential.userName && x.Password == loginCredential.password);
        if (c is null)
        {
            return NotFound();
        }

        return Ok(c.Id);
    }

    [HttpPost("create")]
    public async Task<ActionResult<User>> Create([FromBody] User user)
    {
        if (string.IsNullOrWhiteSpace(user.Password))
            return BadRequest("Password is required.");

        var hash = user.Password; // BCrypt.Net.BCrypt.HashPassword(user.Password, workFactor: 12);

        var c = new User
        {
            UserName = user.UserName.Trim(),
            UserFullName = user.UserFullName.Trim(),
            Password = hash
        };

        db.Users.Add(c);
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("duplicate") == true)
        {
            return Conflict("UserName (or another unique field) already exists.");
        }

        return Ok(User);
    }

    [HttpPut("update")]
    public async Task<ActionResult<User>> Update(int id, [FromBody] User user)
    {
        var c = await db.Users.FirstOrDefaultAsync(x => x.Id == id);
        if (c is null) return NotFound();

        if (string.IsNullOrWhiteSpace(user.Password))
            return BadRequest("Password is required.");

        c.UserName = user.UserName.Trim();
        c.UserFullName = user.UserFullName;
        c.Password = user.Password; // BCrypt.Net.BCrypt.HashPassword(user.Password, workFactor: 12);

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

    [HttpDelete("delete")]
    public async Task<IActionResult> Delete(int id)
    {
        var c = await db.Users.FirstOrDefaultAsync(x => x.Id == id);
        if (c is null) return NotFound();

        db.Users.Remove(c);
        await db.SaveChangesAsync();
        return NoContent();
    }
}
