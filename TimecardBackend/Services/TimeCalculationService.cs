using Timecard.Models;

namespace Timecard.Services;

public interface ITimeCalculationService
{
    double CalculateHours(long clockIn, long clockOut);
    (double regularHours, double overtimeHours) CalculateRegularAndOvertime(double totalHours, double weeklyHours);
    decimal CalculatePay(double regularHours, double overtimeHours, decimal hourlyRate);
    bool ValidateTimeEntry(long clockIn, long? clockOut, out string? errorMessage);
}

public class TimeCalculationService : ITimeCalculationService
{
    private readonly TimeCardSettings _settings;

    public TimeCalculationService(TimeCardSettings settings)
    {
        _settings = settings;
    }

    public double CalculateHours(long clockIn, long clockOut)
    {
        var startTime = DateTimeOffset.FromUnixTimeSeconds(clockIn);
        var endTime = DateTimeOffset.FromUnixTimeSeconds(clockOut);
        return (endTime - startTime).TotalHours;
    }

    public (double regularHours, double overtimeHours) CalculateRegularAndOvertime(double totalHours, double weeklyHours)
    {
        // Calculate overtime based on weekly hours
        var regularWeeklyLimit = _settings.RegularHoursPerWeek;
        
        if (weeklyHours <= regularWeeklyLimit)
        {
            return (totalHours, 0);
        }

        var overtimeHours = weeklyHours - regularWeeklyLimit;
        var regularHours = totalHours - overtimeHours;

        // Ensure no negative values
        regularHours = Math.Max(0, regularHours);
        overtimeHours = Math.Max(0, overtimeHours);

        return (regularHours, overtimeHours);
    }

    public decimal CalculatePay(double regularHours, double overtimeHours, decimal hourlyRate)
    {
        var regularPay = (decimal)regularHours * hourlyRate;
        var overtimePay = (decimal)overtimeHours * hourlyRate * (decimal)_settings.OvertimeMultiplier;
        return regularPay + overtimePay;
    }

    public bool ValidateTimeEntry(long clockIn, long? clockOut, out string? errorMessage)
    {
        errorMessage = null;

        var startTime = DateTimeOffset.FromUnixTimeSeconds(clockIn);
        var now = DateTimeOffset.UtcNow;

        // Clock-in cannot be in the future
        if (startTime > now)
        {
            errorMessage = "Clock-in time cannot be in the future";
            return false;
        }

        // If clock-out exists, validate it
        if (clockOut.HasValue)
        {
            var endTime = DateTimeOffset.FromUnixTimeSeconds(clockOut.Value);

            // Clock-out cannot be before clock-in
            if (endTime <= startTime)
            {
                errorMessage = "Clock-out time must be after clock-in time";
                return false;
            }

            // Check max hours per day
            var hours = (endTime - startTime).TotalHours;
            if (hours > _settings.MaxHoursPerDay)
            {
                errorMessage = $"Time entry cannot exceed {_settings.MaxHoursPerDay} hours per day";
                return false;
            }

            // Clock-out cannot be in the future
            if (endTime > now)
            {
                errorMessage = "Clock-out time cannot be in the future";
                return false;
            }
        }

        return true;
    }
}
