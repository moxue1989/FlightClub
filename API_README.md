# FlightClub - Scheduled Tasks API with Execution Engine

## Overview

FlightClub now includes a comprehensive task scheduling and execution system with:
- **REST API** for managing scheduled tasks
- **Background Service** for automatic task execution
- **Multiple Task Executors** for different task types
- **Manual Trigger System** for on-demand execution
- **Real-time Monitoring** and statistics

## API Endpoints

### Base URL
- Development: `https://localhost:7xxx/api/scheduledtasks`
- Production: `https://flightclub.azurewebsites.net/api/scheduledtasks`

### Endpoints

#### 1. Create Scheduled Task
- **POST** `/api/scheduledtasks`
- **Description**: Creates a new scheduled task
- **Request Body**:
```json
{
  "name": "Buntzen Lake Reservation",
  "description": "Reserve Buntzen Lake recreation area",
  "scheduledTime": "2025-07-02T10:00:00Z",
  "taskType": "ReserveBuntzen",
  "parameters": "{\"Date\":\"2025-07-15\",\"AuthToken\":\"eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...\"}",
  "priority": 1,
  "createdBy": "System"
}
```
- **Response**: 201 Created with task details

#### 2. Get All Scheduled Tasks
- **GET** `/api/scheduledtasks`
- **Query Parameters**:
  - `status` (optional): Filter by task status
  - `taskType` (optional): Filter by task type
- **Response**: 200 OK with array of tasks

#### 3. Get Scheduled Task by ID
- **GET** `/api/scheduledtasks/{id}`
- **Response**: 200 OK with task details or 404 Not Found

#### 4. Update Scheduled Task
- **PUT** `/api/scheduledtasks/{id}`
- **Request Body**: Partial update object
- **Response**: 200 OK with updated task or 404 Not Found

#### 5. Update Task Status
- **PATCH** `/api/scheduledtasks/{id}/status`
- **Request Body**: Status string ("Pending", "Running", "Completed", "Failed", "Cancelled")
- **Response**: 200 OK with updated task or 404 Not Found

#### 6. Delete Scheduled Task
- **DELETE** `/api/scheduledtasks/{id}`
- **Response**: 204 No Content or 404 Not Found

### Task Execution Endpoints

#### 1. Trigger Task Execution
- **POST** `/api/taskexecution/trigger/{id}`
- **Description**: Manually trigger execution of a specific task
- **Response**: 200 OK with execution result

#### 2. Get Execution Statistics
- **GET** `/api/taskexecution/stats`
- **Description**: Get comprehensive execution statistics
- **Response**: 200 OK with statistics object

#### 3. Get Available Task Types
- **GET** `/api/taskexecution/task-types`
- **Description**: List all registered task executor types
- **Response**: 200 OK with array of task type strings

#### 4. Get System Health
- **GET** `/api/taskexecution/health`
- **Description**: Get system health and execution status
- **Response**: 200 OK with health information

## Data Models

### ScheduledTask
```json
{
  "id": 1,
  "name": "Task Name",
  "description": "Task Description",
  "scheduledTime": "2025-07-02T10:00:00Z",
  "taskType": "TaskType",
  "parameters": "JSON string",
  "status": "Pending",
  "priority": 1,
  "createdAt": "2025-07-01T12:00:00Z",
  "updatedAt": "2025-07-01T12:30:00Z",
  "createdBy": "User"
}
```

### Valid Status Values
- `Pending` - Task is scheduled but not started
- `Running` - Task is currently executing
- `Completed` - Task finished successfully
- `Failed` - Task execution failed
- `Cancelled` - Task was cancelled

## Task Execution System

### Background Service
The `TaskSchedulerService` runs as a background service and:
- Monitors for due tasks every 5 seconds
- Automatically executes tasks when their scheduled time arrives
- Supports concurrent execution (up to 5 tasks simultaneously)
- Handles task timeouts (10 minutes maximum)
- Updates task status throughout the execution lifecycle

### Task Executors
The system includes built-in task executors:

#### NotificationTaskExecutor
- **Task Type**: `Notification`
- **Purpose**: Simulates sending notifications
- **Parameters**: `{"recipient": "user@example.com", "message": "Text", "type": "Email|SMS|Push"}`
- **Duration**: 0.2-1 second based on notification type

#### ReserveBuntzenExecutor
- **Task Type**: `ReserveBuntzen`
- **Purpose**: Makes actual Buntzen Lake reservations using YodelPass API
- **Parameters**: `{"Date": "2025-07-15", "AuthToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."}`
- **Duration**: Variable (depends on API response times and retry logic)
- **Features**: Real YodelPass API integration, cart management, automatic retry logic (up to 100 attempts), checkout processing

### Task Execution Flow
1. **Scheduled Time Arrives** - Background service detects due task
2. **Status Update** - Task status changes from `Pending` to `Running`
3. **Executor Selection** - System finds appropriate executor for task type
4. **Execution** - Task executor performs the work
5. **Result Processing** - Execution result is captured
6. **Status Update** - Final status set to `Completed` or `Failed`
7. **Logging** - All steps are logged for monitoring

### Manual Execution
Tasks can be manually triggered via:
- API endpoint: `POST /api/taskexecution/trigger/{id}`
- Demo page "Trigger Last Task" button
- Any task in `Pending` status can be manually executed

## Features

### Validation
- Required field validation
- Scheduled time must be in the future
- Status values are restricted to valid options
- String length limits enforced

### Error Handling
- Comprehensive error responses
- Proper HTTP status codes
- Detailed error messages

### Logging
- All API operations are logged
- Error tracking and debugging support

### Documentation
- Swagger/OpenAPI documentation available at `/api-docs`
- Interactive API explorer for testing

## Usage Examples

### Using cURL

#### Create a Task
```bash
curl -X POST "https://localhost:7xxx/api/scheduledtasks" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Buntzen Lake Reservation",
    "description": "Reserve Buntzen Lake for hiking",
    "scheduledTime": "2025-07-02T02:00:00Z",
    "taskType": "ReserveBuntzen",
    "parameters": "{\"Date\":\"2025-07-15\",\"AuthToken\":\"eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...\"}",
    "priority": 1
  }'
```

#### Get All Tasks
```bash
curl -X GET "https://localhost:7xxx/api/scheduledtasks"
```

#### Update Task Status
```bash
curl -X PATCH "https://localhost:7xxx/api/scheduledtasks/1/status" \
  -H "Content-Type: application/json" \
  -d '"Running"'
```

### Using JavaScript/Fetch
```javascript
// Create a new notification task
const response = await fetch('/api/scheduledtasks', {
  method: 'POST',
  headers: {
    'Content-Type': 'application/json',
  },
  body: JSON.stringify({
    name: 'Buntzen Reservation Reminder',
    scheduledTime: new Date(Date.now() + 3600000).toISOString(),
    taskType: 'Notification',
    parameters: JSON.stringify({
      recipient: 'visitor@example.com',
      message: 'Your Buntzen Lake reservation is tomorrow!',
      type: 'Email'
    }),
    priority: 1
  })
});

const task = await response.json();
console.log('Created task:', task);
```

## Testing

### Interactive Testing
1. Run the application in development mode
2. Navigate to `/api-docs` for Swagger UI
3. Use the interactive interface to test all endpoints
4. Visit `/ScheduledTasks` for a simple demo page

### Demo Page
The application includes a demo page at `/ScheduledTasks` that provides:
- Quick test buttons for creating and retrieving tasks
- Real-time results display
- Links to full API documentation

## Development Notes

### In-Memory Storage
The current implementation uses in-memory storage for simplicity. For production use, consider:
- Database integration (Entity Framework Core)
- Data persistence across application restarts
- Scalability considerations

### Future Enhancements
- Database persistence
- Authentication and authorization
- Task scheduling and execution
- Webhook notifications
- Batch operations
- Advanced filtering and sorting

## Troubleshooting

### Common Issues
1. **404 Not Found**: Ensure the API controllers are properly registered
2. **Validation Errors**: Check required fields and data formats
3. **CORS Issues**: Configure CORS if calling from different domains
4. **Time Zone Issues**: Always use UTC timestamps

### Logs
Check application logs for detailed error information and debugging details.
