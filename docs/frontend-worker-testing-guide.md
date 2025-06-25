# Frontend Development Guide: Worker Service Testing Page

## Overview

This guide provides frontend developers with everything needed to create a comprehensive testing page for the MonitoringWorker service. The page will allow users to manage the worker lifecycle, monitor endpoints, and view real-time status updates.

## API Endpoints Reference

### Base URL
- **Development**: `http://localhost:5002`
- **Production**: `https://your-domain.com`

### Authentication Required
All endpoints require JWT authentication. Include the token in the Authorization header:
```
Authorization: Bearer <your-jwt-token>
```

### Worker Management Endpoints

#### 1. Get Worker Status
```http
GET /api/worker/status
```
**Response:**
```json
{
  "isRunning": true,
  "isStarted": true,
  "isShutdown": false,
  "startTime": "2025-06-25T12:31:43Z",
  "jobs": [
    {
      "jobName": "monitoring-job",
      "jobGroup": "DEFAULT",
      "triggerName": "monitoring-trigger",
      "triggerGroup": "DEFAULT",
      "state": "Normal",
      "nextFireTime": "2025-06-25T12:33:00Z",
      "previousFireTime": "2025-06-25T12:32:00Z",
      "description": "Monitoring job for endpoint health checks"
    }
  ],
  "metrics": {
    "totalChecks": 10,
    "successfulChecks": 8,
    "failedChecks": 2,
    "errorChecks": 0,
    "successRate": 80.0,
    "totalJobs": 5,
    "successfulJobs": 4,
    "failedJobs": 1,
    "cancelledJobs": 0,
    "lastHeartbeat": "2025-06-25T12:32:30Z",
    "uptime": "00:01:30"
  }
}
```

#### 2. Start Worker
```http
POST /api/worker/start
```
**Response:**
```json
{
  "success": true,
  "message": "Worker started successfully"
}
```

#### 3. Stop Worker
```http
POST /api/worker/stop
```
**Response:**
```json
{
  "success": true,
  "message": "Worker stopped successfully"
}
```

#### 4. Restart Worker
```http
POST /api/worker/restart
```
**Response:**
```json
{
  "success": true,
  "message": "Worker restarted successfully"
}
```

#### 5. Pause Job
```http
POST /api/worker/jobs/{jobName}/pause
```
**Response:**
```json
{
  "success": true,
  "message": "Job 'monitoring-job' paused successfully"
}
```

#### 6. Resume Job
```http
POST /api/worker/jobs/{jobName}/resume
```
**Response:**
```json
{
  "success": true,
  "message": "Job 'monitoring-job' resumed successfully"
}
```

### Authentication Endpoints

#### Login
```http
POST /api/auth/login
Content-Type: application/json

{
  "username": "admin",
  "password": "admin123"
}
```
**Response:**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "user": {
    "username": "admin",
    "roles": ["Admin"],
    "permissions": ["ViewMonitoring", "ManageMonitoring"]
  },
  "expiresAt": "2025-06-25T18:31:43Z"
}
```

### Monitoring Endpoints

#### Get Monitoring Status
```http
GET /api/monitoring/status
```

#### Get Monitoring Metrics
```http
GET /api/monitoring/metrics
```

#### Manual Endpoint Check
```http
POST /api/monitoring/check
POST /api/monitoring/check/{endpointName}
```

### Database Monitoring Endpoints

#### Get Database Monitoring Dashboard
```http
GET /api/database-monitoring/dashboard
```
**Response:**
```json
{
  "summary": {
    "totalConnections": 5,
    "healthyConnections": 4,
    "warningConnections": 1,
    "criticalConnections": 0,
    "errorConnections": 0,
    "overallHealthPercentage": 80.0,
    "lastUpdate": "2025-06-25T16:30:00Z"
  },
  "connections": [
    {
      "connectionName": "Production-DB",
      "status": "Healthy",
      "message": "Connection successful",
      "durationMs": 245,
      "timestamp": "2025-06-25T16:29:45Z",
      "serverVersion": "Microsoft SQL Server 2022",
      "databaseName": "ProductionDB"
    }
  ],
  "statistics": {
    "totalConnections": 5,
    "totalQueries": 12,
    "lastRunTimestamp": "2025-06-25T16:29:45Z"
  }
}
```

#### Get Database Monitoring Results
```http
GET /api/database-monitoring/results?connectionName=&status=&environment=&fromDate=&toDate=&pageSize=20&pageNumber=1
```
**Response:**
```json
{
  "items": [
    {
      "connectionName": "Production-DB",
      "queryName": "Connection Test",
      "status": "Healthy",
      "message": "Query executed successfully",
      "timestamp": "2025-06-25T16:29:45Z",
      "durationMs": 125,
      "resultValue": 1,
      "provider": "SqlServer",
      "environment": "Production"
    }
  ],
  "totalCount": 150,
  "pageNumber": 1,
  "pageSize": 20,
  "totalPages": 8,
  "hasNextPage": true,
  "hasPreviousPage": false
}
```

#### Get Database Monitoring Trends
```http
GET /api/database-monitoring/trends?period=24h&groupBy=hour
```
**Response:**
```json
{
  "period": "24h",
  "groupBy": "hour",
  "data": [
    {
      "timestamp": "2025-06-25T15:00:00Z",
      "healthyCount": 8,
      "warningCount": 1,
      "errorCount": 0
    }
  ],
  "summary": {
    "averageHealthyPercentage": 88.9,
    "totalChecks": 1250,
    "averageResponseTime": 245.5,
    "uptimePercentage": 99.2
  }
}
```

### Database Configuration Endpoints

#### Get Database Connections
```http
GET /api/database-monitoring/config/connections
```
**Response:**
```json
[
  {
    "connectionId": "123e4567-e89b-12d3-a456-426614174000",
    "connectionName": "Production-DB",
    "provider": "SqlServer",
    "environment": "Production",
    "tags": ["critical", "primary"],
    "connectionTimeoutSeconds": 30,
    "commandTimeoutSeconds": 30,
    "isEnabled": true,
    "createdDate": "2025-05-25T16:30:00Z",
    "modifiedDate": "2025-06-24T16:30:00Z"
  }
]
```

#### Create Database Connection
```http
POST /api/database-monitoring/config/connections
Content-Type: application/json

{
  "connectionName": "New-DB",
  "provider": "SqlServer",
  "connectionString": "Server=localhost;Database=TestDB;Integrated Security=true;",
  "environment": "Development",
  "tags": ["dev", "testing"],
  "connectionTimeoutSeconds": 30,
  "commandTimeoutSeconds": 30,
  "isEnabled": true
}
```

#### Update Database Connection
```http
PUT /api/database-monitoring/config/connections/{connectionId}
Content-Type: application/json

{
  "connectionName": "Updated-DB",
  "provider": "SqlServer",
  "environment": "Development",
  "tags": ["dev", "updated"],
  "connectionTimeoutSeconds": 45,
  "commandTimeoutSeconds": 45,
  "isEnabled": true
}
```

#### Delete Database Connection
```http
DELETE /api/database-monitoring/config/connections/{connectionId}
```

#### Test Database Connection
```http
POST /api/database-monitoring/config/connections/{connectionId}/test
```

### Enhanced Worker Endpoints

#### Get Worker Metrics
```http
GET /api/worker/metrics?metricType=&fromDate=&toDate=
```
**Response:**
```json
{
  "current": {
    "totalChecks": 150,
    "successfulChecks": 145,
    "failedChecks": 5,
    "successRate": 96.7,
    "lastHeartbeat": "2025-06-25T16:30:00Z",
    "uptime": "02:15:30"
  },
  "historical": [
    {
      "timestamp": "2025-06-25T16:00:00Z",
      "totalChecks": 145,
      "successRate": 95.0
    }
  ],
  "summary": {
    "averageSuccessRate": 96.2,
    "totalUptime": "02:15:30",
    "averageResponseTime": 245.5,
    "peakMemoryUsage": "125 MB"
  }
}
```

#### Get Worker Instances
```http
GET /api/worker/instances
```
**Response:**
```json
[
  {
    "workerInstanceId": "123e4567-e89b-12d3-a456-426614174000",
    "workerName": "MonitoringWorker-01",
    "machineName": "SERVER-01",
    "processId": 1234,
    "version": "1.0.0",
    "environment": "Production",
    "status": "Running",
    "startedAt": "2025-06-25T14:15:00Z",
    "lastHeartbeat": "2025-06-25T16:30:00Z",
    "uptime": "02:15:00",
    "runningJobs": 2,
    "queuedJobs": 1
  }
]
```

#### Get Worker Jobs
```http
GET /api/worker/jobs?status=&jobType=&pageSize=20&pageNumber=1
```
**Response:**
```json
{
  "items": [
    {
      "jobId": "123e4567-e89b-12d3-a456-426614174000",
      "jobName": "database-monitoring-job",
      "jobGroup": "DEFAULT",
      "jobType": "DatabaseMonitoring",
      "status": "Running",
      "priority": 5,
      "scheduledAt": "2025-06-25T16:30:00Z",
      "nextFireTime": "2025-06-25T16:35:00Z",
      "previousFireTime": "2025-06-25T16:30:00Z",
      "description": "Database monitoring job"
    }
  ],
  "totalCount": 25,
  "pageNumber": 1,
  "pageSize": 20,
  "totalPages": 2,
  "hasNextPage": true,
  "hasPreviousPage": false
}
```

## Real-time Updates (SignalR)

### Connection
```javascript
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/monitoringHub", {
        accessTokenFactory: () => localStorage.getItem('jwt-token')
    })
    .build();
```

### Event Handlers
```javascript
// Worker status changes
connection.on("WorkerStarted", (event) => {
    console.log("Worker started:", event);
});

connection.on("WorkerStopped", (event) => {
    console.log("Worker stopped:", event);
});

// Monitoring results
connection.on("MonitoringResult", (result) => {
    console.log("Monitoring result:", result);
});
```

## Frontend Implementation Requirements

### 1. Worker Control Panel

Create a control panel with the following components:

#### Status Display
- **Worker Status Badge**: Running/Stopped/Error states with color coding
- **Uptime Counter**: Real-time display of worker uptime
- **Last Heartbeat**: Timestamp of last worker heartbeat
- **Metrics Summary**: Success rate, total checks, etc.

#### Control Buttons
```html
<div class="worker-controls">
  <button id="start-worker" class="btn btn-success">Start Worker</button>
  <button id="stop-worker" class="btn btn-danger">Stop Worker</button>
  <button id="restart-worker" class="btn btn-warning">Restart Worker</button>
  <button id="refresh-status" class="btn btn-secondary">Refresh Status</button>
</div>
```

#### Jobs Management
- **Jobs List**: Display all scheduled jobs with their status
- **Job Controls**: Pause/Resume buttons for each job
- **Next Execution**: Countdown to next job execution

### 2. Monitoring Dashboard

#### Endpoint Status Grid
```html
<div class="endpoints-grid">
  <div class="endpoint-card" data-status="healthy">
    <h4>LocalTest</h4>
    <span class="status-badge healthy">Healthy</span>
    <p>Response Time: 491ms</p>
    <p>Last Check: 2 minutes ago</p>
  </div>
</div>
```

#### Real-time Metrics
- **Success Rate Chart**: Visual representation of success percentage
- **Response Time Graph**: Historical response times
- **Status Distribution**: Pie chart of healthy/unhealthy/error states

### 3. Testing Tools

#### Manual Endpoint Testing
```html
<div class="manual-testing">
  <h3>Manual Endpoint Testing</h3>
  <select id="endpoint-select">
    <option value="">All Endpoints</option>
    <option value="LocalTest">LocalTest</option>
    <option value="ExternalAPI">ExternalAPI</option>
  </select>
  <button id="run-check" class="btn btn-primary">Run Check</button>
</div>
```

#### Configuration Testing
- **Endpoint Configuration**: Add/edit/remove monitoring endpoints
- **Settings Validation**: Test configuration changes
- **Bulk Operations**: Start/stop multiple endpoints

### 4. Error Handling

#### Error Display Component
```javascript
function showError(message, details = null) {
    const errorDiv = document.createElement('div');
    errorDiv.className = 'alert alert-danger';
    errorDiv.innerHTML = `
        <strong>Error:</strong> ${message}
        ${details ? `<br><small>${details}</small>` : ''}
        <button type="button" class="close" onclick="this.parentElement.remove()">
            <span>&times;</span>
        </button>
    `;
    document.getElementById('error-container').appendChild(errorDiv);
}
```

#### Common Error Scenarios
- **401 Unauthorized**: Token expired or invalid
- **403 Forbidden**: Insufficient permissions
- **500 Server Error**: Worker service issues
- **Connection Lost**: SignalR disconnection

### 5. Sample JavaScript Implementation

#### Worker Service Client
```javascript
class WorkerServiceClient {
    constructor(baseUrl, token) {
        this.baseUrl = baseUrl;
        this.token = token;
    }

    async getStatus() {
        const response = await fetch(`${this.baseUrl}/api/worker/status`, {
            headers: {
                'Authorization': `Bearer ${this.token}`,
                'Content-Type': 'application/json'
            }
        });
        
        if (!response.ok) {
            throw new Error(`HTTP ${response.status}: ${response.statusText}`);
        }
        
        return await response.json();
    }

    async startWorker() {
        const response = await fetch(`${this.baseUrl}/api/worker/start`, {
            method: 'POST',
            headers: {
                'Authorization': `Bearer ${this.token}`,
                'Content-Type': 'application/json'
            }
        });
        
        return await response.json();
    }

    async stopWorker() {
        const response = await fetch(`${this.baseUrl}/api/worker/stop`, {
            method: 'POST',
            headers: {
                'Authorization': `Bearer ${this.token}`,
                'Content-Type': 'application/json'
            }
        });
        
        return await response.json();
    }
}
```

#### Usage Example
```javascript
const client = new WorkerServiceClient('http://localhost:5000', localStorage.getItem('jwt-token'));

// Get worker status
document.getElementById('refresh-status').addEventListener('click', async () => {
    try {
        const status = await client.getStatus();
        updateStatusDisplay(status);
    } catch (error) {
        showError('Failed to get worker status', error.message);
    }
});

// Start worker
document.getElementById('start-worker').addEventListener('click', async () => {
    try {
        const result = await client.startWorker();
        if (result.success) {
            showSuccess(result.message);
            refreshStatus();
        } else {
            showError(result.message, result.details);
        }
    } catch (error) {
        showError('Failed to start worker', error.message);
    }
});
```

## UI/UX Recommendations

### 1. Visual Design
- **Status Colors**: Green (healthy), Yellow (warning), Red (error), Gray (stopped)
- **Real-time Updates**: Smooth animations for status changes
- **Loading States**: Spinners during API calls
- **Responsive Design**: Mobile-friendly layout

### 2. User Experience
- **Auto-refresh**: Periodic status updates (every 30 seconds)
- **Confirmation Dialogs**: For destructive actions (stop worker)
- **Keyboard Shortcuts**: Quick access to common actions
- **Accessibility**: ARIA labels and keyboard navigation

### 3. Performance
- **Debounced Requests**: Prevent rapid API calls
- **Caching**: Cache status data for short periods
- **Lazy Loading**: Load components as needed
- **Error Retry**: Automatic retry for failed requests

## Testing Checklist

### Functional Testing
- [ ] Worker start/stop/restart functionality
- [ ] Job pause/resume operations
- [ ] Real-time status updates
- [ ] Manual endpoint testing
- [ ] Error handling and display
- [ ] Authentication flow

### Integration Testing
- [ ] SignalR connection and events
- [ ] API error responses
- [ ] Token expiration handling
- [ ] Network connectivity issues
- [ ] Concurrent user operations

### Performance Testing
- [ ] Page load time under 3 seconds
- [ ] Smooth animations and transitions
- [ ] Memory usage optimization
- [ ] Mobile device compatibility

## Security Considerations

### 1. Token Management
- Store JWT tokens securely (httpOnly cookies preferred)
- Implement token refresh mechanism
- Clear tokens on logout
- Validate token expiration

### 2. API Security
- Always use HTTPS in production
- Validate all user inputs
- Implement rate limiting on client side
- Handle CORS properly

### 3. Error Information
- Don't expose sensitive error details to users
- Log detailed errors for debugging
- Implement proper error boundaries
- Sanitize error messages

## Browser Support

### Minimum Requirements
- **Chrome**: 80+
- **Firefox**: 75+
- **Safari**: 13+
- **Edge**: 80+

### Required Features
- Fetch API
- WebSocket support (for SignalR)
- ES6+ JavaScript features
- CSS Grid and Flexbox

## Development Tools

### Recommended Libraries
- **SignalR Client**: `@microsoft/signalr`
- **HTTP Client**: Native Fetch API or Axios
- **UI Framework**: Bootstrap, Material-UI, or Tailwind CSS
- **Charts**: Chart.js or D3.js for metrics visualization

### Development Setup
1. Install SignalR client: `npm install @microsoft/signalr`
2. Configure development proxy to backend
3. Set up hot reload for rapid development
4. Configure CORS for local development

## React Component Example

For React applications, here's a complete component example:

```jsx
import React, { useState, useEffect, useCallback } from 'react';
import { MonitoringWorkerClient } from './monitoring-worker-client';

const WorkerDashboard = () => {
    const [client] = useState(() => new MonitoringWorkerClient('http://localhost:5000', {
        autoRefresh: true,
        refreshInterval: 30000
    }));

    const [isAuthenticated, setIsAuthenticated] = useState(false);
    const [workerStatus, setWorkerStatus] = useState(null);
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState(null);
    const [logs, setLogs] = useState([]);

    const addLog = useCallback((message, type = 'info') => {
        const timestamp = new Date().toLocaleTimeString();
        setLogs(prev => [...prev, { timestamp, message, type, id: Date.now() }]);
    }, []);

    useEffect(() => {
        // Set up event listeners
        client.on('statusUpdated', setWorkerStatus);
        client.on('workerStarted', () => addLog('Worker started', 'success'));
        client.on('workerStopped', () => addLog('Worker stopped', 'warning'));
        client.on('monitoringResult', (result) =>
            addLog(`Monitoring: ${result.checkName} - ${result.status}`, 'info')
        );
        client.on('signalRConnected', () => addLog('Real-time connection established', 'success'));
        client.on('signalRDisconnected', () => addLog('Real-time connection lost', 'error'));

        return () => {
            client.destroy();
        };
    }, [client, addLog]);

    const handleLogin = async (username, password) => {
        setLoading(true);
        setError(null);
        try {
            await client.login(username, password);
            setIsAuthenticated(true);
            await client.initialize();
            addLog('Login successful', 'success');
        } catch (err) {
            setError(err.message);
            addLog(`Login failed: ${err.message}`, 'error');
        } finally {
            setLoading(false);
        }
    };

    const handleWorkerAction = async (action) => {
        setLoading(true);
        try {
            let result;
            switch (action) {
                case 'start':
                    result = await client.startWorker();
                    break;
                case 'stop':
                    result = await client.stopWorker();
                    break;
                case 'restart':
                    result = await client.restartWorker();
                    break;
                default:
                    throw new Error('Unknown action');
            }
            addLog(result.message, result.success ? 'success' : 'warning');
        } catch (err) {
            setError(err.message);
            addLog(`Action failed: ${err.message}`, 'error');
        } finally {
            setLoading(false);
        }
    };

    if (!isAuthenticated) {
        return <LoginForm onLogin={handleLogin} loading={loading} error={error} />;
    }

    return (
        <div className="worker-dashboard">
            <WorkerStatusCard
                status={workerStatus}
                onAction={handleWorkerAction}
                loading={loading}
            />
            <JobsManagement
                jobs={workerStatus?.jobs || []}
                onPauseJob={(jobName) => client.pauseJob(jobName)}
                onResumeJob={(jobName) => client.resumeJob(jobName)}
            />
            <LogsPanel logs={logs} onClear={() => setLogs([])} />
        </div>
    );
};

export default WorkerDashboard;
```

## Additional Files Provided

1. **`worker-testing-template.html`** - Complete HTML template with Bootstrap UI
2. **`monitoring-worker-client.js`** - Reusable JavaScript client library
3. **React component examples** - Modern framework integration

This guide provides everything needed to create a comprehensive worker service testing page. Focus on user experience, real-time updates, and robust error handling for the best results.
