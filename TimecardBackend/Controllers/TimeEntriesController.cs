using Microsoft.AspNetCore.Mvc;
using Timecard.Models.DTOs;
using Timecard.Services;

namespace Timecard.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TimeEntriesController : ControllerBase
{
    private readonly ITimeEntryService _timeEntryService;

    public TimeEntriesController(ITimeEntryService timeEntryService)
    {
        _timeEntryService = timeEntryService;
    }

    /// <summary>
    /// Get time entry by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<TimeEntryDto>> GetById(int id)
    {
        var entry = await _timeEntryService.GetByIdAsync(id);
        if (entry == null)
        {
            return NotFound(new { message = "Time entry not found" });
        }
        return Ok(entry);
    }

    /// <summary>
    /// Get all time entries for an employee
    /// </summary>
    [HttpGet("employee/{employeeId}")]
    public async Task<ActionResult<IEnumerable<TimeEntryDto>>> GetByEmployeeId(int employeeId)
    {
        var entries = await _timeEntryService.GetByEmployeeIdAsync(employeeId);
        return Ok(entries);
    }

    /// <summary>
    /// Get time entries for an employee within a date range
    /// </summary>
    [HttpGet("employee/{employeeId}/range")]
    public async Task<ActionResult<IEnumerable<TimeEntryDto>>> GetByDateRange(
        int employeeId,
        [FromQuery] long startDate,
        [FromQuery] long endDate)
    {
        var entries = await _timeEntryService.GetByDateRangeAsync(employeeId, startDate, endDate);
        return Ok(entries);
    }

    /// <summary>
    /// Create a new time entry (clock in)
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<TimeEntryDto>> Create([FromBody] CreateTimeEntryDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var entry = await _timeEntryService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = entry.Id }, entry);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Clock out of a time entry
    /// </summary>
    [HttpPost("{id}/clock-out")]
    public async Task<ActionResult<TimeEntryDto>> ClockOut(int id, [FromBody] ClockOutDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var entry = await _timeEntryService.ClockOutAsync(id, dto);
            if (entry == null)
            {
                return NotFound(new { message = "Time entry not found" });
            }
            return Ok(entry);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Update a time entry
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<TimeEntryDto>> Update(int id, [FromBody] UpdateTimeEntryDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var entry = await _timeEntryService.UpdateAsync(id, dto);
            if (entry == null)
            {
                return NotFound(new { message = "Time entry not found" });
            }
            return Ok(entry);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Delete a time entry
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var success = await _timeEntryService.DeleteAsync(id);
        if (!success)
        {
            return NotFound(new { message = "Time entry not found" });
        }
        return NoContent();
    }

    /// <summary>
    /// Get time report for an employee within a date range
    /// </summary>
    [HttpGet("employee/{employeeId}/report")]
    public async Task<ActionResult<TimeReportDto>> GetTimeReport(
        int employeeId,
        [FromQuery] long startDate,
        [FromQuery] long endDate)
    {
        var report = await _timeEntryService.GetTimeReportAsync(employeeId, startDate, endDate);
        if (report == null)
        {
            return NotFound(new { message = "Employee not found" });
        }
        return Ok(report);
    }
}