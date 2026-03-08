using Microsoft.EntityFrameworkCore;
using Timecard.Data;
using Timecard.Services;

var builder = WebApplication.CreateBuilder(args);

// Configure TimeCard settings
var timeCardSettings = new TimeCardSettings();
builder.Configuration.GetSection("TimeCardSettings").Bind(timeCardSettings);
builder.Services.AddSingleton(timeCardSettings);

// Add SQLite
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register services
builder.Services.AddScoped<IEmployeeService, EmployeeService>();
builder.Services.AddScoped<ITimeEntryService, TimeEntryService>();
builder.Services.AddScoped<ITimeCalculationService, TimeCalculationService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "Timecard API",
        Version = "v1",
        Description = "API for managing employee timecards with overtime calculation"
    });
});

// Add CORS for development
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });
});

var app = builder.Build();

// Enable Swagger UI
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Timecard API v1");
    });
    app.UseCors("AllowAll");
}

// Disable HTTPS redirection in development to avoid port issues
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.MapControllers();
app.Run();