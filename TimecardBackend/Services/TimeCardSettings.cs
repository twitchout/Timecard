namespace Timecard.Services;

public class TimeCardSettings
{
    public double RegularHoursPerDay { get; set; } = 8.0;
    public double RegularHoursPerWeek { get; set; } = 40.0;
    public double OvertimeMultiplier { get; set; } = 1.5;
    public int MaxHoursPerDay { get; set; } = 24;
    public int MinBreakMinutes { get; set; } = 30;
}
