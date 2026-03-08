using System.ComponentModel.DataAnnotations;

namespace Timecard.Models.DTOs;

public class CreateEmployeeDto
{
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

    [Required]
    [MinLength(6)]
    public string Password { get; set; } = string.Empty;
}

public class UpdateEmployeeDto
{
    [MaxLength(100)]
    public string? Name { get; set; }

    [EmailAddress]
    [MaxLength(100)]
    public string? Email { get; set; }

    [MaxLength(50)]
    public string? Role { get; set; }

    [MaxLength(50)]
    public string? Department { get; set; }

    [Range(0, 1000)]
    public decimal? HourlyRate { get; set; }

    public bool? IsActive { get; set; }
}

public class EmployeeDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? Department { get; set; }
    public decimal HourlyRate { get; set; }
    public bool IsActive { get; set; }
    public long CreatedAt { get; set; }
    public long? UpdatedAt { get; set; }
}

public class LoginDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

public class LoginResponseDto
{
    public int EmployeeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

public class SetPasswordDto
{
    [Required]
    [MinLength(6)]
    public string Password { get; set; } = string.Empty;
}
