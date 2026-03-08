namespace TimecardApp.Models;

public class Employee
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? Department { get; set; }
    public decimal HourlyRate { get; set; }
    public bool IsActive { get; set; }
}
