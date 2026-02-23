using System.ComponentModel.DataAnnotations;

namespace Timecard.Models.DTOs;

public class CreateTimeEntryDto
{
    [Required]
    public int EmployeeId { get; set; }

    [Required]
    public long ClockIn { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }
}

public class ClockOutDto
{
    [Required]
    public long ClockOut { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }
}

public class UpdateTimeEntryDto
{
    public long? ClockIn { get; set; }
    public long? ClockOut { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    public bool? IsApproved { get; set; }
}

public class TimeEntryDto
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public long ClockIn { get; set; }
    public long? ClockOut { get; set; }
    public string? Notes { get; set; }
    public bool IsApproved { get; set; }
    public long CreatedAt { get; set; }
    public long? UpdatedAt { get; set; }
    public double? HoursWorked { get; set; }
    public double? RegularHours { get; set; }
    public double? OvertimeHours { get; set; }
}

public class TimeReportDto
{
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public long StartDate { get; set; }
    public long EndDate { get; set; }
    public double TotalHours { get; set; }
    public double RegularHours { get; set; }
    public double OvertimeHours { get; set; }
    public decimal TotalPay { get; set; }
    public int TotalEntries { get; set; }
}
