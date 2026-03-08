# Timecard Management System

A comprehensive timecard management system built with ASP.NET Core 9.0 featuring automatic overtime calculation, time tracking, and payroll reporting.

## Quick Start

```bash
# Clone and navigate
git clone https://github.com/twitchout/Timecard.git
cd Timecard/TimecardBackend

# Restore and run
dotnet restore
dotnet ef database update
dotnet run
```

Access the API at `https://localhost:5001/swagger`

## Features

- Employee management with CRUD operations
- Clock in/out time tracking
- Automatic overtime calculation (40+ hours/week)
- Time reports with pay calculations
- Comprehensive validation and testing (21 unit tests)

## Documentation

📖 **[Complete Project Documentation](PROJECT_DOCUMENTATION.md)**

The comprehensive documentation includes:
- System overview and architecture
- Complete API reference with examples
- Business logic and calculations
- Configuration guide
- Testing information
- Future roadmap
- Presentation points and demo flow

## Technology Stack

- ASP.NET Core 9.0
- Entity Framework Core 9.0
- SQLite Database
- xUnit Testing
- Swagger/OpenAPI

## Quick Test

```bash
# Run tests
cd TimecardBackend.Tests
dotnet test
```

Expected: 21 tests passing ✓

## License

MIT License
