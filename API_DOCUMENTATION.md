# API Documentation

## Base URL

Development: `https://localhost:5001/api`

## Authentication

Currently, the API does not require authentication. This should be implemented before production deployment.

## Response Format

All responses are in JSON format.

### Success Response
```json
{
  "data": { ... }
}
```

### Error Response
```json
{
  "message": "Error description"
}
```

## HTTP Status Codes

- `200 OK`: Request succeeded
- `201 Created`: Resource created successfully
- `204 No Content`: Resource deleted successfully
- `400 Bad Request`: Invalid request data
- `404 Not Found`: Resource not found
- `409 Conflict`: Request conflicts with current state

## Timestamps

All timestamps in the API use Unix time (seconds since epoch) in UTC.

## Endpoints

### Employees

#### List All Employees

**GET** `/employees`

Query Parameters:
- `includeInactive` (boolean, optional): Include inactive employees. Default: false

**Response**: 200 OK
```json
[
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
]
```

#### Get Employee

**GET** `/employees/{id}`

**Response**: 200 OK
```json
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

**Error Responses**:
- 404: Employee not found

#### Create Employee

**POST** `/employees`

**Request Body**:
```json
{
  "name": "John Doe",
  "email": "john@example.com",
  "role": "Developer",
  "department": "IT",
  "hourlyRate": 50.00
}
```

**Validation Rules**:
- `name`: Required, max 100 characters
- `email`: Required, valid email format, max 100 characters
- `role`: Required, max 50 characters
- `department`: Optional, max 50 characters
- `hourlyRate`: Required, range 0-1000

**Response**: 201 Created
```json
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

#### Update Employee

**PUT** `/employees/{id}`

**Request Body** (all fields optional):
```json
{
  "name": "John Smith",
  "email": "john.smith@example.com",
  "role": "Senior Developer",
  "department": "Engineering",
  "hourlyRate": 60.00,
  "isActive": true
}
```

**Response**: 200 OK
```json
{
  "id": 1,
  "name": "John Smith",
  "email": "john.smith@example.com",
  "role": "Senior Developer",
  "department": "Engineering",
  "hourlyRate": 60.00,
  "isActive": true,
  "createdAt": 1708617600,
  "updatedAt": 1708704000
}
```

**Error Responses**:
- 404: Employee not found

#### Delete Employee

**DELETE** `/employees/{id}`

Note: This is a soft delete. The employee is marked as inactive but not removed from the database.

**Response**: 204 No Content

**Error Responses**:
- 404: Employee not found

### Time Entries

#### Get Time Entry

**GET** `/timeentries/{id}`

**Response**: 200 OK
```json
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

**Error Responses**:
- 404: Time entry not found

#### List Employee Time Entries

**GET** `/timeentries/employee/{employeeId}`

Returns all time entries for an employee, ordered by most recent first.

**Response**: 200 OK
```json
[
  {
    "id": 2,
    "employeeId": 1,
    "employeeName": "John Doe",
    "clockIn": 1708704000,
    "clockOut": null,
    "notes": "Currently working",
    "isApproved": false,
    "createdAt": 1708704000,
    "updatedAt": null,
    "hoursWorked": null,
    "regularHours": null,
    "overtimeHours": null
  },
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
]
```

#### Get Time Entries by Date Range

**GET** `/timeentries/employee/{employeeId}/range`

Query Parameters:
- `startDate` (required): Unix timestamp for range start
- `endDate` (required): Unix timestamp for range end

**Response**: 200 OK (same format as list endpoint)

#### Clock In (Create Time Entry)

**POST** `/timeentries`

**Request Body**:
```json
{
  "employeeId": 1,
  "clockIn": 1708617600,
  "notes": "Starting work"
}
```

**Validation Rules**:
- `employeeId`: Required, must exist
- `clockIn`: Required, cannot be in the future
- `notes`: Optional, max 500 characters

**Response**: 201 Created
```json
{
  "id": 1,
  "employeeId": 1,
  "employeeName": "John Doe",
  "clockIn": 1708617600,
  "clockOut": null,
  "notes": "Starting work",
  "isApproved": false,
  "createdAt": 1708617600,
  "updatedAt": null,
  "hoursWorked": null,
  "regularHours": null,
  "overtimeHours": null
}
```

**Error Responses**:
- 400: Invalid request or employee not found

#### Clock Out

**POST** `/timeentries/{id}/clock-out`

**Request Body**:
```json
{
  "clockOut": 1708646400,
  "notes": "Completed work"
}
```

**Validation Rules**:
- `clockOut`: Required, must be after clock-in, cannot be in the future
- Entry must not already be clocked out

**Response**: 200 OK
```json
{
  "id": 1,
  "employeeId": 1,
  "employeeName": "John Doe",
  "clockIn": 1708617600,
  "clockOut": 1708646400,
  "notes": "Completed work",
  "isApproved": false,
  "createdAt": 1708617600,
  "updatedAt": 1708646400,
  "hoursWorked": 8.0,
  "regularHours": 8.0,
  "overtimeHours": 0.0
}
```

**Error Responses**:
- 404: Time entry not found
- 409: Time entry already clocked out

#### Update Time Entry

**PUT** `/timeentries/{id}`

**Request Body** (all fields optional):
```json
{
  "clockIn": 1708617600,
  "clockOut": 1708646400,
  "notes": "Updated notes",
  "isApproved": true
}
```

**Response**: 200 OK (same format as get endpoint)

**Error Responses**:
- 404: Time entry not found
- 400: Validation error

#### Delete Time Entry

**DELETE** `/timeentries/{id}`

Note: This is a hard delete. The entry is permanently removed.

**Response**: 204 No Content

**Error Responses**:
- 404: Time entry not found

#### Get Time Report

**GET** `/timeentries/employee/{employeeId}/report`

Query Parameters:
- `startDate` (required): Unix timestamp for report start
- `endDate` (required): Unix timestamp for report end

Generates a summary report with total hours, overtime calculation, and pay.

**Response**: 200 OK
```json
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

**Error Responses**:
- 404: Employee not found

## Rate Limiting

Currently not implemented. Consider adding rate limiting before production deployment.

## Pagination

List endpoints currently return all results. Consider implementing pagination for large datasets.

## Future Enhancements

1. **Authentication & Authorization**: Implement JWT-based authentication
2. **Role-based Access Control**: Different permissions for employees, managers, and admins
3. **Audit Logging**: Track all changes to time entries
4. **Approval Workflow**: Multi-level approval for time entries
5. **Export Functionality**: Export reports to CSV/Excel
6. **Real-time Updates**: WebSocket support for live updates
7. **Notifications**: Email/SMS notifications for pending approvals
