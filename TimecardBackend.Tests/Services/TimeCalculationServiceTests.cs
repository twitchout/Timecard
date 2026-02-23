using Timecard.Services;

namespace TimecardBackend.Tests.Services;

public class TimeCalculationServiceTests
{
    private readonly TimeCalculationService _service;

    public TimeCalculationServiceTests()
    {
        var settings = new TimeCardSettings
        {
            RegularHoursPerDay = 8.0,
            RegularHoursPerWeek = 40.0,
            OvertimeMultiplier = 1.5,
            MaxHoursPerDay = 24
        };
        _service = new TimeCalculationService(settings);
    }

    [Fact]
    public void CalculateHours_ShouldReturnCorrectHours()
    {
        // Arrange - 8 hours difference
        var clockIn = new DateTimeOffset(2026, 2, 22, 9, 0, 0, TimeSpan.Zero).ToUnixTimeSeconds();
        var clockOut = new DateTimeOffset(2026, 2, 22, 17, 0, 0, TimeSpan.Zero).ToUnixTimeSeconds();

        // Act
        var hours = _service.CalculateHours(clockIn, clockOut);

        // Assert
        Assert.Equal(8.0, hours);
    }

    [Fact]
    public void CalculateRegularAndOvertime_NoOvertime_ShouldReturnAllRegularHours()
    {
        // Arrange
        var totalHours = 8.0;
        var weeklyHours = 30.0;

        // Act
        var (regularHours, overtimeHours) = _service.CalculateRegularAndOvertime(totalHours, weeklyHours);

        // Assert
        Assert.Equal(8.0, regularHours);
        Assert.Equal(0.0, overtimeHours);
    }

    [Fact]
    public void CalculateRegularAndOvertime_WithOvertime_ShouldSplitHours()
    {
        // Arrange - 50 hours total, 10 hours overtime
        var totalHours = 10.0;
        var weeklyHours = 50.0;

        // Act
        var (regularHours, overtimeHours) = _service.CalculateRegularAndOvertime(totalHours, weeklyHours);

        // Assert
        Assert.Equal(0.0, regularHours);
        Assert.Equal(10.0, overtimeHours);
    }

    [Fact]
    public void CalculatePay_ShouldIncludeOvertimeMultiplier()
    {
        // Arrange
        var regularHours = 40.0;
        var overtimeHours = 10.0;
        var hourlyRate = 20m;

        // Act
        var pay = _service.CalculatePay(regularHours, overtimeHours, hourlyRate);

        // Assert
        var expectedPay = (40m * 20m) + (10m * 20m * 1.5m); // 800 + 300 = 1100
        Assert.Equal(expectedPay, pay);
    }

    [Fact]
    public void ValidateTimeEntry_ClockInInFuture_ShouldReturnFalse()
    {
        // Arrange
        var futureTime = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds();

        // Act
        var isValid = _service.ValidateTimeEntry(futureTime, null, out var errorMessage);

        // Assert
        Assert.False(isValid);
        Assert.Equal("Clock-in time cannot be in the future", errorMessage);
    }

    [Fact]
    public void ValidateTimeEntry_ClockOutBeforeClockIn_ShouldReturnFalse()
    {
        // Arrange
        var clockIn = DateTimeOffset.UtcNow.AddHours(-2).ToUnixTimeSeconds();
        var clockOut = DateTimeOffset.UtcNow.AddHours(-3).ToUnixTimeSeconds();

        // Act
        var isValid = _service.ValidateTimeEntry(clockIn, clockOut, out var errorMessage);

        // Assert
        Assert.False(isValid);
        Assert.Equal("Clock-out time must be after clock-in time", errorMessage);
    }

    [Fact]
    public void ValidateTimeEntry_ExceedsMaxHours_ShouldReturnFalse()
    {
        // Arrange
        var clockIn = DateTimeOffset.UtcNow.AddHours(-25).ToUnixTimeSeconds();
        var clockOut = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        // Act
        var isValid = _service.ValidateTimeEntry(clockIn, clockOut, out var errorMessage);

        // Assert
        Assert.False(isValid);
        Assert.Contains("cannot exceed", errorMessage);
    }

    [Fact]
    public void ValidateTimeEntry_ValidEntry_ShouldReturnTrue()
    {
        // Arrange
        var clockIn = DateTimeOffset.UtcNow.AddHours(-8).ToUnixTimeSeconds();
        var clockOut = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        // Act
        var isValid = _service.ValidateTimeEntry(clockIn, clockOut, out var errorMessage);

        // Assert
        Assert.True(isValid);
        Assert.Null(errorMessage);
    }
}
