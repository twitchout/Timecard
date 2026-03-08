using Microsoft.EntityFrameworkCore;
using Timecard.Data;
using Timecard.Models;
using Timecard.Models.DTOs;

namespace Timecard.Services;

public interface IEmployeeService
{
    Task<EmployeeDto?> GetByIdAsync(int id);
    Task<IEnumerable<EmployeeDto>> GetAllAsync(bool includeInactive = false);
    Task<EmployeeDto> CreateAsync(CreateEmployeeDto dto);
    Task<EmployeeDto?> UpdateAsync(int id, UpdateEmployeeDto dto);
    Task<bool> DeleteAsync(int id);
    Task<bool> ExistsAsync(int id);
    Task<LoginResponseDto?> AuthenticateAsync(LoginDto dto);
    Task<bool> SetPasswordAsync(int id, string password);
}

public class EmployeeService : IEmployeeService
{
    private readonly AppDbContext _db;

    public EmployeeService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<EmployeeDto?> GetByIdAsync(int id)
    {
        var employee = await _db.Employees.FindAsync(id);
        return employee == null ? null : MapToDto(employee);
    }

    public async Task<IEnumerable<EmployeeDto>> GetAllAsync(bool includeInactive = false)
    {
        var query = _db.Employees.AsQueryable();
        
        if (!includeInactive)
        {
            query = query.Where(e => e.IsActive);
        }

        var employees = await query.OrderBy(e => e.Name).ToListAsync();
        return employees.Select(MapToDto);
    }

    public async Task<EmployeeDto> CreateAsync(CreateEmployeeDto dto)
    {
        var employee = new Employee
        {
            Name = dto.Name,
            Email = dto.Email,
            Role = dto.Role,
            Department = dto.Department,
            HourlyRate = dto.HourlyRate,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };

        _db.Employees.Add(employee);
        await _db.SaveChangesAsync();

        return MapToDto(employee);
    }

    public async Task<EmployeeDto?> UpdateAsync(int id, UpdateEmployeeDto dto)
    {
        var employee = await _db.Employees.FindAsync(id);
        if (employee == null) return null;

        if (dto.Name != null) employee.Name = dto.Name;
        if (dto.Email != null) employee.Email = dto.Email;
        if (dto.Role != null) employee.Role = dto.Role;
        if (dto.Department != null) employee.Department = dto.Department;
        if (dto.HourlyRate.HasValue) employee.HourlyRate = dto.HourlyRate.Value;
        if (dto.IsActive.HasValue) employee.IsActive = dto.IsActive.Value;

        employee.UpdatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        await _db.SaveChangesAsync();
        return MapToDto(employee);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var employee = await _db.Employees.FindAsync(id);
        if (employee == null) return false;

        // Soft delete
        employee.IsActive = false;
        employee.UpdatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        await _db.SaveChangesAsync();

        return true;
    }

    public async Task<bool> ExistsAsync(int id)
    {
        return await _db.Employees.AnyAsync(e => e.Id == id);
    }

    private static EmployeeDto MapToDto(Employee employee)
    {
        return new EmployeeDto
        {
            Id = employee.Id,
            Name = employee.Name,
            Email = employee.Email,
            Role = employee.Role,
            Department = employee.Department,
            HourlyRate = employee.HourlyRate,
            IsActive = employee.IsActive,
            CreatedAt = employee.CreatedAt,
            UpdatedAt = employee.UpdatedAt
        };
    }

    public async Task<LoginResponseDto?> AuthenticateAsync(LoginDto dto)
    {
        var employee = await _db.Employees
            .FirstOrDefaultAsync(e => e.Email == dto.Email && e.IsActive);

        if (employee == null)
        {
            return null;
        }

        // Verify password hash
        if (!BCrypt.Net.BCrypt.Verify(dto.Password, employee.PasswordHash))
        {
            return null;
        }

        return new LoginResponseDto
        {
            EmployeeId = employee.Id,
            Name = employee.Name,
            Email = employee.Email,
            Role = employee.Role
        };
    }

    public async Task<bool> SetPasswordAsync(int id, string password)
    {
        var employee = await _db.Employees.FindAsync(id);
        if (employee == null) return false;

        employee.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
        employee.UpdatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        
        await _db.SaveChangesAsync();
        return true;
    }
}
