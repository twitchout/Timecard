using Microsoft.EntityFrameworkCore;
using Timecard.Models;

namespace Timecard.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Employee> Employees { get; set; }
    public DbSet<TimeEntry> TimeEntries { get; set; }
}