# Timecard API

A comprehensive timecard management system built with ASP.NET Core 9.0 and Entity Framework Core. This API allows you to manage employees and their time entries with automatic overtime calculation.

## Features

- **Employee Management**: Full CRUD operations for employees
- **Time Entry Management**: Clock in/out functionality with validation
- **Automatic Overtime Calculation**: Calculates regular and overtime hours based on configurable settings
- **Time Reports**: Generate detailed time reports with pay calculations
- **Data Validation**: Comprehensive validation for time entries
- **RESTful API**: Well-structured REST API with Swagger documentation

## Technology Stack

- ASP.NET Core 9.0
- Entity Framework Core 9.0
- SQLite Database
- xUnit for testing
- Swagger/OpenAPI for documentation

## Getting Started

### Prerequisites

- .NET 9.0 SDK or later
- Visual Studio 2022, VS Code, or any C# IDE

### Installation

1. Clone the repository:
```bash
git clone https://github.com/twitchout/Timecard.git
cd Timecard
```

2. Restore dependencies:
```bash
cd TimecardBackend
dotnet restore
```

3. Apply database migrations:
```bash
dotnet ef database update
```

4. Run the application:
```bash
dotnet run
```

The API will be available at `https://localhost:5001` (or the port specified in launchSettings.json).

### Running Tests

```bash
cd TimecardBackend.Tests
dotnet test
```

## Configuration

Configuration settings are located in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=timecard.db"
  },
  "TimeCardSettings": {
    "RegularHoursPerDay": 8.0,
    "RegularHoursPerWeek": 40.0,
    "OvertimeMultiplier": 1.5,
    "MaxHoursPerDay": 24,
    "MinBreakMinutes": 30
  }
}
```

### TimeCard Settings

- **RegularHoursPerDay**: Standard hours per day (default: 8.0)
- **RegularHoursPerWeek**: Hours per week before overtime applies (default: 40.0)
- **OvertimeMultiplier**: Pay multiplier for overtime hours (default: 1.5)
- **MaxHoursPerDay**: Maximum allowed hours per day (default: 24)
- **MinBreakMinutes**: Minimum break time in minutes (default: 30)

## API Documentation

When running in development mode, Swagger UI is available at `/swagger`.

### Employees API

#### Get All Employees
```
GET /api/employees?includeInactive=false
```

#### Get Employee by ID
```
GET /api/employees/{id}
```

#### Create Employee
```
POST /api/employees
Content-Type: application/json

{
  "name": "John Doe",
  "email": "john@example.com",
  "role": "Developer",
  "department": "IT",
  "hourlyRate": 50.00
}
```

#### Update Employee
```
PUT /api/employees/{id}
Content-Type: application/json

{
  "name": "John Smith",
  "hourlyRate": 55.00
}
```

#### Delete Employee (Soft Delete)
```
DELETE /api/employees/{id}
```

### Time Entries API

#### Get Time Entry by ID
```
GET /api/timeentries/{id}
```

#### Get Time Entries for Employee
```
GET /api/timeentries/employee/{employeeId}
```

#### Get Time Entries by Date Range
```
GET /api/timeentries/employee/{employeeId}/range?startDate={unixTimestamp}&endDate={unixTimestamp}
```

#### Clock In (Create Time Entry)
```
POST /api/timeentries
Content-Type: application/json

{
  "employeeId": 1,
  "clockIn": 1708617600,
  "notes": "Starting work"
}
```

#### Clock Out
```
POST /api/timeentries/{id}/clock-out
Content-Type: application/json

{
  "clockOut": 1708646400,
  "notes": "Completed work"
}
```

#### Update Time Entry
```
PUT /api/timeentries/{id}
Content-Type: application/json

{
  "notes": "Updated notes",
  "isApproved": true
}
```

#### Delete Time Entry
```
DELETE /api/timeentries/{id}
```

#### Get Time Report
```
GET /api/timeentries/employee/{employeeId}/report?startDate={unixTimestamp}&endDate={unixTimestamp}
```

## Data Models

### Employee
```csharp
{
  "id": 1,
  "name": "John Doe",
  "email": "john@example.com",
  "role": "Developer",
  "department": "IT",
  "hourlyRate": 50.00,
  "isActive": true,
  "createdAt": 1708617600,
  "updatedAt": null
}
```

### Time Entry
```csharp
{
  "id": 1,
  "employeeId": 1,
  "employeeName": "John Doe",
  "clockIn": 1708617600,
  "clockOut": 1708646400,
  "notes": "Regular work day",
  "isApproved": false,
  "createdAt": 1708617600,
  "updatedAt": null,
  "hoursWorked": 8.0,
  "regularHours": 8.0,
  "overtimeHours": 0.0
}
```

### Time Report
```csharp
{
  "employeeId": 1,
  "employeeName": "John Doe",
  "startDate": 1708617600,
  "endDate": 1709222400,
  "totalHours": 45.0,
  "regularHours": 40.0,
  "overtimeHours": 5.0,
  "totalPay": 2375.00,
  "totalEntries": 5
}
```

## Business Logic

### Overtime Calculation

Overtime is calculated on a weekly basis:
- Hours up to `RegularHoursPerWeek` (default: 40) are considered regular hours
- Hours beyond that threshold are considered overtime
- Overtime hours are paid at `OvertimeMultiplier` (default: 1.5x) the regular rate

**Example:**
- Employee works 50 hours in a week
- Regular hours: 40 hours @ $20/hour = $800
- Overtime hours: 10 hours @ $30/hour (1.5x) = $300
- Total pay: $1,100

### Time Entry Validation

The system validates:
- Clock-in time cannot be in the future
- Clock-out time must be after clock-in time
- Maximum hours per day cannot exceed configured limit
- Clock-out time cannot be in the future

## Project Structure

```
Timecard/
├── TimecardBackend/
│   ├── Controllers/          # API Controllers
│   ├── Data/                 # Database context
│   ├── Migrations/           # EF Core migrations
│   ├── Models/               # Data models and DTOs
│   │   └── DTOs/            # Data Transfer Objects
│   ├── Services/            # Business logic layer
│   ├── appsettings.json     # Configuration
│   └── Program.cs           # Application entry point
├── TimecardBackend.Tests/
│   └── Services/            # Unit tests
└── Timecard.sln             # Solution file
```

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Write/update tests
5. Submit a pull request

## License

This project is licensed under the MIT License.

## Support

For issues, questions, or contributions, please open an issue on GitHub.
