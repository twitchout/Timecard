using System.ComponentModel.DataAnnotations;

namespace Timecard.Models;

public class Employee
{
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(100)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Role { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? Department { get; set; }

    [Range(0, 1000)]
    public decimal HourlyRate { get; set; }

    public bool IsActive { get; set; } = true;

    [Required]
    [MaxLength(255)]
    public string PasswordHash { get; set; } = string.Empty;

    public long CreatedAt { get; set; }

    public long? UpdatedAt { get; set; }

    // Navigation property
    public ICollection<TimeEntry> TimeEntries { get; set; } = new List<TimeEntry>();
}