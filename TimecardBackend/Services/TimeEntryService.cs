using Microsoft.EntityFrameworkCore;
using Timecard.Data;
using Timecard.Models;
using Timecard.Models.DTOs;

namespace Timecard.Services;

public interface ITimeEntryService
{
    Task<TimeEntryDto?> GetByIdAsync(int id);
    Task<IEnumerable<TimeEntryDto>> GetByEmployeeIdAsync(int employeeId);
    Task<IEnumerable<TimeEntryDto>> GetByDateRangeAsync(int employeeId, long startDate, long endDate);
    Task<TimeEntryDto> CreateAsync(CreateTimeEntryDto dto);
    Task<TimeEntryDto?> ClockOutAsync(int id, ClockOutDto dto);
    Task<TimeEntryDto?> UpdateAsync(int id, UpdateTimeEntryDto dto);
    Task<bool> DeleteAsync(int id);
    Task<TimeReportDto?> GetTimeReportAsync(int employeeId, long startDate, long endDate);
}

public class TimeEntryService : ITimeEntryService
{
    private readonly AppDbContext _db;
    private readonly ITimeCalculationService _timeCalc;
    private readonly IEmployeeService _employeeService;

    public TimeEntryService(AppDbContext db, ITimeCalculationService timeCalc, IEmployeeService employeeService)
    {
        _db = db;
        _timeCalc = timeCalc;
        _employeeService = employeeService;
    }

    public async Task<TimeEntryDto?> GetByIdAsync(int id)
    {
        var entry = await _db.TimeEntries
            .Include(t => t.Employee)
            .FirstOrDefaultAsync(t => t.Id == id);

        return entry == null ? null : await MapToDtoAsync(entry);
    }

    public async Task<IEnumerable<TimeEntryDto>> GetByEmployeeIdAsync(int employeeId)
    {
        var entries = await _db.TimeEntries
            .Include(t => t.Employee)
            .Where(t => t.EmployeeId == employeeId)
            .OrderByDescending(t => t.ClockIn)
            .ToListAsync();

        var dtos = new List<TimeEntryDto>();
        foreach (var entry in entries)
        {
            dtos.Add(await MapToDtoAsync(entry));
        }
        return dtos;
    }

    public async Task<IEnumerable<TimeEntryDto>> GetByDateRangeAsync(int employeeId, long startDate, long endDate)
    {
        var entries = await _db.TimeEntries
            .Include(t => t.Employee)
            .Where(t => t.EmployeeId == employeeId && t.ClockIn >= startDate && t.ClockIn <= endDate)
            .OrderBy(t => t.ClockIn)
            .ToListAsync();

        var dtos = new List<TimeEntryDto>();
        foreach (var entry in entries)
        {
            dtos.Add(await MapToDtoAsync(entry));
        }
        return dtos;
    }

    public async Task<TimeEntryDto> CreateAsync(CreateTimeEntryDto dto)
    {
        // Validate employee exists
        if (!await _employeeService.ExistsAsync(dto.EmployeeId))
        {
            throw new ArgumentException("Employee not found");
        }

        // Validate time entry
        if (!_timeCalc.ValidateTimeEntry(dto.ClockIn, null, out var errorMessage))
        {
            throw new ArgumentException(errorMessage);
        }

        var entry = new TimeEntry
        {
            EmployeeId = dto.EmployeeId,
            ClockIn = dto.ClockIn,
            Notes = dto.Notes,
            CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };

        _db.TimeEntries.Add(entry);
        await _db.SaveChangesAsync();

        // Reload with employee data
        await _db.Entry(entry).Reference(t => t.Employee).LoadAsync();
        return await MapToDtoAsync(entry);
    }

    public async Task<TimeEntryDto?> ClockOutAsync(int id, ClockOutDto dto)
    {
        var entry = await _db.TimeEntries
            .Include(t => t.Employee)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (entry == null) return null;

        if (entry.ClockOut.HasValue)
        {
            throw new InvalidOperationException("Time entry is already clocked out");
        }

        // Validate time entry
        if (!_timeCalc.ValidateTimeEntry(entry.ClockIn, dto.ClockOut, out var errorMessage))
        {
            throw new ArgumentException(errorMessage);
        }

        entry.ClockOut = dto.ClockOut;
        if (dto.Notes != null) entry.Notes = dto.Notes;
        entry.UpdatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        await _db.SaveChangesAsync();
        return await MapToDtoAsync(entry);
    }

    public async Task<TimeEntryDto?> UpdateAsync(int id, UpdateTimeEntryDto dto)
    {
        var entry = await _db.TimeEntries
            .Include(t => t.Employee)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (entry == null) return null;

        var clockIn = dto.ClockIn ?? entry.ClockIn;
        var clockOut = dto.ClockOut ?? entry.ClockOut;

        // Validate time entry
        if (!_timeCalc.ValidateTimeEntry(clockIn, clockOut, out var errorMessage))
        {
            throw new ArgumentException(errorMessage);
        }

        if (dto.ClockIn.HasValue) entry.ClockIn = dto.ClockIn.Value;
        if (dto.ClockOut.HasValue) entry.ClockOut = dto.ClockOut.Value;
        if (dto.Notes != null) entry.Notes = dto.Notes;
        if (dto.IsApproved.HasValue) entry.IsApproved = dto.IsApproved.Value;

        entry.UpdatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        await _db.SaveChangesAsync();
        return await MapToDtoAsync(entry);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var entry = await _db.TimeEntries.FindAsync(id);
        if (entry == null) return false;

        _db.TimeEntries.Remove(entry);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<TimeReportDto?> GetTimeReportAsync(int employeeId, long startDate, long endDate)
    {
        var employee = await _employeeService.GetByIdAsync(employeeId);
        if (employee == null) return null;

        var entries = await _db.TimeEntries
            .Where(t => t.EmployeeId == employeeId 
                && t.ClockIn >= startDate 
                && t.ClockIn <= endDate
                && t.ClockOut.HasValue)
            .ToListAsync();

        double totalHours = 0;
        foreach (var entry in entries)
        {
            totalHours += _timeCalc.CalculateHours(entry.ClockIn, entry.ClockOut!.Value);
        }

        var (regularHours, overtimeHours) = _timeCalc.CalculateRegularAndOvertime(totalHours, totalHours);
        var totalPay = _timeCalc.CalculatePay(regularHours, overtimeHours, employee.HourlyRate);

        return new TimeReportDto
        {
            EmployeeId = employeeId,
            EmployeeName = employee.Name,
            StartDate = startDate,
            EndDate = endDate,
            TotalHours = totalHours,
            RegularHours = regularHours,
            OvertimeHours = overtimeHours,
            TotalPay = totalPay,
            TotalEntries = entries.Count
        };
    }

    private async Task<TimeEntryDto> MapToDtoAsync(TimeEntry entry)
    {
        double? hoursWorked = null;
        double? regularHours = null;
        double? overtimeHours = null;

        if (entry.ClockOut.HasValue)
        {
            hoursWorked = _timeCalc.CalculateHours(entry.ClockIn, entry.ClockOut.Value);
            
            // Get weekly hours for overtime calculation
            var startOfWeek = GetStartOfWeek(entry.ClockIn);
            var endOfWeek = GetEndOfWeek(entry.ClockIn);
            
            var weeklyEntries = await _db.TimeEntries
                .Where(t => t.EmployeeId == entry.EmployeeId 
                    && t.ClockIn >= startOfWeek 
                    && t.ClockIn <= endOfWeek
                    && t.ClockOut.HasValue)
                .ToListAsync();

            double weeklyHours = 0;
            foreach (var weekEntry in weeklyEntries)
            {
                weeklyHours += _timeCalc.CalculateHours(weekEntry.ClockIn, weekEntry.ClockOut!.Value);
            }

            var overtime = _timeCalc.CalculateRegularAndOvertime(hoursWorked.Value, weeklyHours);
            regularHours = overtime.regularHours;
            overtimeHours = overtime.overtimeHours;
        }

        return new TimeEntryDto
        {
            Id = entry.Id,
            EmployeeId = entry.EmployeeId,
            EmployeeName = entry.Employee?.Name ?? "Unknown",
            ClockIn = entry.ClockIn,
            ClockOut = entry.ClockOut,
            Notes = entry.Notes,
            IsApproved = entry.IsApproved,
            CreatedAt = entry.CreatedAt,
            UpdatedAt = entry.UpdatedAt,
            HoursWorked = hoursWorked,
            RegularHours = regularHours,
            OvertimeHours = overtimeHours
        };
    }

    private static long GetStartOfWeek(long timestamp)
    {
        var date = DateTimeOffset.FromUnixTimeSeconds(timestamp);
        var diff = (7 + (date.DayOfWeek - DayOfWeek.Sunday)) % 7;
        var startOfWeek = date.AddDays(-diff).Date;
        return new DateTimeOffset(startOfWeek, TimeSpan.Zero).ToUnixTimeSeconds();
    }

    private static long GetEndOfWeek(long timestamp)
    {
        var startOfWeek = GetStartOfWeek(timestamp);
        var startDate = DateTimeOffset.FromUnixTimeSeconds(startOfWeek);
        var endOfWeek = startDate.AddDays(7).AddSeconds(-1);
        return endOfWeek.ToUnixTimeSeconds();
    }
}
