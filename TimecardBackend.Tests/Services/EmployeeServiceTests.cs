using Microsoft.EntityFrameworkCore;
using Timecard.Data;
using Timecard.Models;
using Timecard.Models.DTOs;
using Timecard.Services;

namespace TimecardBackend.Tests.Services;

public class EmployeeServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly EmployeeService _service;

    public EmployeeServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _db = new AppDbContext(options);
        _service = new EmployeeService(_db);
    }

    [Fact]
    public async Task CreateAsync_ShouldCreateEmployee()
    {
        // Arrange
        var dto = new CreateEmployeeDto
        {
            Name = "John Doe",
            Email = "john@example.com",
            Role = "Developer",
            Department = "IT",
            HourlyRate = 50m
        };

        // Act
        var result = await _service.CreateAsync(dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("John Doe", result.Name);
        Assert.Equal("john@example.com", result.Email);
        Assert.True(result.IsActive);
        Assert.True(result.Id > 0);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingEmployee_ShouldReturnEmployee()
    {
        // Arrange
        var employee = new Employee
        {
            Name = "Jane Smith",
            Email = "jane@example.com",
            Role = "Manager",
            HourlyRate = 60m,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };
        _db.Employees.Add(employee);
        await _db.SaveChangesAsync();

        // Act
        var result = await _service.GetByIdAsync(employee.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Jane Smith", result.Name);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingEmployee_ShouldReturnNull()
    {
        // Act
        var result = await _service.GetByIdAsync(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnActiveEmployees()
    {
        // Arrange
        _db.Employees.AddRange(
            new Employee { Name = "Active 1", Email = "a1@test.com", Role = "Dev", IsActive = true, CreatedAt = 1 },
            new Employee { Name = "Active 2", Email = "a2@test.com", Role = "Dev", IsActive = true, CreatedAt = 1 },
            new Employee { Name = "Inactive", Email = "i@test.com", Role = "Dev", IsActive = false, CreatedAt = 1 }
        );
        await _db.SaveChangesAsync();

        // Act
        var result = await _service.GetAllAsync(includeInactive: false);

        // Assert
        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateEmployee()
    {
        // Arrange
        var employee = new Employee
        {
            Name = "Old Name",
            Email = "old@example.com",
            Role = "Developer",
            HourlyRate = 40m,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };
        _db.Employees.Add(employee);
        await _db.SaveChangesAsync();

        var updateDto = new UpdateEmployeeDto
        {
            Name = "New Name",
            HourlyRate = 50m
        };

        // Act
        var result = await _service.UpdateAsync(employee.Id, updateDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("New Name", result.Name);
        Assert.Equal(50m, result.HourlyRate);
        Assert.NotNull(result.UpdatedAt);
    }

    [Fact]
    public async Task DeleteAsync_ShouldSoftDeleteEmployee()
    {
        // Arrange
        var employee = new Employee
        {
            Name = "To Delete",
            Email = "delete@example.com",
            Role = "Developer",
            HourlyRate = 40m,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };
        _db.Employees.Add(employee);
        await _db.SaveChangesAsync();

        // Act
        var result = await _service.DeleteAsync(employee.Id);

        // Assert
        Assert.True(result);
        var deletedEmployee = await _db.Employees.FindAsync(employee.Id);
        Assert.False(deletedEmployee!.IsActive);
    }

    public void Dispose()
    {
        _db.Database.EnsureDeleted();
        _db.Dispose();
    }
}
