using System.ComponentModel.DataAnnotations;

namespace Timecard.Models;

public class TimeEntry
{
    public int Id { get; set; }

    [Required]
    public int EmployeeId { get; set; }

    [Required]
    public long ClockIn { get; set; }

    public long? ClockOut { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    public bool IsApproved { get; set; } = false;

    public long CreatedAt { get; set; }

    public long? UpdatedAt { get; set; }

    // Navigation property
    public Employee? Employee { get; set; }
}