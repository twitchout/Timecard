using Microsoft.EntityFrameworkCore;
using Timecard.Data;
using Timecard.Models;
using Timecard.Models.DTOs;
using Timecard.Services;

namespace TimecardBackend.Tests.Services;

public class TimeEntryServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly TimeEntryService _service;
    private readonly TimeCalculationService _timeCalc;
    private readonly EmployeeService _employeeService;

    public TimeEntryServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _db = new AppDbContext(options);
        var settings = new TimeCardSettings();
        _timeCalc = new TimeCalculationService(settings);
        _employeeService = new EmployeeService(_db);
        _service = new TimeEntryService(_db, _timeCalc, _employeeService);

        // Create a test employee
        _db.Employees.Add(new Employee
        {
            Id = 1,
            Name = "Test Employee",
            Email = "test@example.com",
            Role = "Developer",
            HourlyRate = 50m,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        });
        _db.SaveChanges();
    }

    [Fact]
    public async Task CreateAsync_ValidEntry_ShouldCreateTimeEntry()
    {
        // Arrange
        var dto = new CreateTimeEntryDto
        {
            EmployeeId = 1,
            ClockIn = DateTimeOffset.UtcNow.AddHours(-1).ToUnixTimeSeconds(),
            Notes = "Test entry"
        };

        // Act
        var result = await _service.CreateAsync(dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.EmployeeId);
        Assert.Equal("Test entry", result.Notes);
        Assert.Null(result.ClockOut);
    }

    [Fact]
    public async Task CreateAsync_InvalidEmployee_ShouldThrowException()
    {
        // Arrange
        var dto = new CreateTimeEntryDto
        {
            EmployeeId = 999,
            ClockIn = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateAsync(dto));
    }

    [Fact]
    public async Task ClockOutAsync_ValidEntry_ShouldClockOut()
    {
        // Arrange
        var clockInTime = DateTimeOffset.UtcNow.AddHours(-8).ToUnixTimeSeconds();
        var entry = new TimeEntry
        {
            EmployeeId = 1,
            ClockIn = clockInTime,
            CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };
        _db.TimeEntries.Add(entry);
        await _db.SaveChangesAsync();

        var clockOutDto = new ClockOutDto
        {
            ClockOut = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            Notes = "Completed work"
        };

        // Act
        var result = await _service.ClockOutAsync(entry.Id, clockOutDto);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.ClockOut);
        Assert.NotNull(result.HoursWorked);
        Assert.True(result.HoursWorked > 7 && result.HoursWorked < 9);
    }

    [Fact]
    public async Task ClockOutAsync_AlreadyClockedOut_ShouldThrowException()
    {
        // Arrange
        var entry = new TimeEntry
        {
            EmployeeId = 1,
            ClockIn = DateTimeOffset.UtcNow.AddHours(-8).ToUnixTimeSeconds(),
            ClockOut = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };
        _db.TimeEntries.Add(entry);
        await _db.SaveChangesAsync();

        var clockOutDto = new ClockOutDto
        {
            ClockOut = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.ClockOutAsync(entry.Id, clockOutDto));
    }

    [Fact]
    public async Task GetByEmployeeIdAsync_ShouldReturnEntriesForEmployee()
    {
        // Arrange
        _db.TimeEntries.AddRange(
            new TimeEntry { EmployeeId = 1, ClockIn = 100, CreatedAt = 100 },
            new TimeEntry { EmployeeId = 1, ClockIn = 200, CreatedAt = 200 }
        );
        await _db.SaveChangesAsync();

        // Act
        var result = await _service.GetByEmployeeIdAsync(1);

        // Assert
        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task GetTimeReportAsync_ShouldCalculateCorrectTotals()
    {
        // Arrange
        var startOfWeek = DateTimeOffset.UtcNow.Date.ToUniversalTime();
        var clockIn1 = new DateTimeOffset(startOfWeek).ToUnixTimeSeconds();
        var clockOut1 = new DateTimeOffset(startOfWeek.AddHours(8)).ToUnixTimeSeconds();

        _db.TimeEntries.Add(new TimeEntry
        {
            EmployeeId = 1,
            ClockIn = clockIn1,
            ClockOut = clockOut1,
            CreatedAt = clockIn1
        });
        await _db.SaveChangesAsync();

        var startDate = new DateTimeOffset(startOfWeek.AddDays(-1)).ToUnixTimeSeconds();
        var endDate = new DateTimeOffset(startOfWeek.AddDays(1)).ToUnixTimeSeconds();

        // Act
        var report = await _service.GetTimeReportAsync(1, startDate, endDate);

        // Assert
        Assert.NotNull(report);
        Assert.Equal(1, report.EmployeeId);
        Assert.Equal(8.0, report.TotalHours);
        Assert.Equal(1, report.TotalEntries);
        Assert.True(report.TotalPay > 0);
    }

    public void Dispose()
    {
        _db.Database.EnsureDeleted();
        _db.Dispose();
    }
}
