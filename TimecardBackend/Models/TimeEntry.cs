namespace Timecard.Models;

public class TimeEntry
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public long ClockIn { get; set; }
    public long? ClockOut { get; set; }
    public string Notes { get; set; }

    public Employee Employee { get; set; }
}