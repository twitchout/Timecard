using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Timecard.Data;
using Timecard.Models;

namespace Timecard.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TimeEntriesController : ControllerBase
{
    private readonly AppDbContext _db;

    public TimeEntriesController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet("{employeeId}")]
    public async Task<IActionResult> GetEntries(int employeeId)
    {
        var entries = await _db.TimeEntries
            .Where(t => t.EmployeeId == employeeId)
            .ToListAsync();

        return Ok(entries);
    }

    [HttpPost]
    public async Task<IActionResult> Create(TimeEntry entry)
    {
        _db.TimeEntries.Add(entry);
        await _db.SaveChangesAsync();
        return Ok(entry);
    }
}