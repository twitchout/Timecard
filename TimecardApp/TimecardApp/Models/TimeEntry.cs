namespace TimecardApp.Models;

public class TimeEntry
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public long ClockIn { get; set; }
    public long? ClockOut { get; set; }
    public string? Notes { get; set; }
    public bool IsApproved { get; set; }
}

public class CreateTimeEntryDto
{
    public int EmployeeId { get; set; }
    public long ClockIn { get; set; }
    public string? Notes { get; set; }
}

public class ClockOutDto
{
    public long ClockOut { get; set; }
    public string? Notes { get; set; }
}
