# Frontend API Changes - Second Round Improvements

## 🚨 **IMPORTANT: Major API Response Format Changes**

### **New Standardized Response Format**

**ALL API responses are now wrapped in a consistent format:**

```typescript
interface ApiResponse<T> {
  success: boolean;           // Indicates if request was successful
  data?: T;                  // The actual response data
  message?: string;          // Success/error message
  errors?: string[];         // Detailed error information
  metadata?: ResponseMetadata; // Additional response metadata
  timestamp: string;         // ISO timestamp of response
}

interface ResponseMetadata {
  totalCount?: number;       // For paginated responses
  pageNumber?: number;       // Current page number
  pageSize?: number;         // Items per page
  totalPages?: number;       // Total number of pages
  hasNextPage?: boolean;     // Whether there's a next page
  hasPreviousPage?: boolean; // Whether there's a previous page
  processingTimeMs?: number; // Request processing time
  version?: string;          // API version
}
```

### **Before vs After Examples**

#### **Before (Old Format)**
```javascript
// GET /api/worker/status
{
  "isRunning": true,
  "isStarted": true,
  "startTime": "2024-01-01T10:00:00Z",
  "jobs": [...],
  "metrics": {...}
}
```

#### **After (New Format)**
```javascript
// GET /api/worker/status
{
  "success": true,
  "data": {
    "isRunning": true,
    "isStarted": true,
    "startTime": "2024-01-01T10:00:00Z",
    "jobs": [...],
    "metrics": {...}
  },
  "message": "Request completed successfully",
  "metadata": {
    "processingTimeMs": 45,
    "version": "1.0"
  },
  "timestamp": "2024-01-01T10:00:00.123Z"
}
```

## 🔧 **Required Frontend Code Changes**

### **1. Update API Client Functions**

#### **Old Code**
```javascript
async function getWorkerStatus() {
  const response = await fetch('/api/worker/status');
  const data = await response.json();
  return data; // Direct access to worker status
}
```

#### **New Code**
```javascript
async function getWorkerStatus() {
  const response = await fetch('/api/worker/status');
  const apiResponse = await response.json();
  
  if (!apiResponse.success) {
    throw new Error(apiResponse.message || 'Request failed');
  }
  
  return apiResponse.data; // Access data through .data property
}
```

### **2. Update Error Handling**

#### **Enhanced Error Information**
```javascript
async function handleApiCall(url) {
  try {
    const response = await fetch(url);
    const apiResponse = await response.json();
    
    if (!apiResponse.success) {
      // Enhanced error handling
      console.error('API Error:', apiResponse.message);
      if (apiResponse.errors) {
        console.error('Details:', apiResponse.errors);
      }
      throw new Error(apiResponse.message);
    }
    
    return apiResponse.data;
  } catch (error) {
    console.error('Request failed:', error);
    throw error;
  }
}
```

### **3. Update Pagination Handling**

#### **New Pagination Support**
```javascript
async function getMonitoringResults(page = 1, pageSize = 20) {
  const response = await fetch(
    `/api/database-monitoring/results?page=${page}&pageSize=${pageSize}`
  );
  const apiResponse = await response.json();
  
  if (!apiResponse.success) {
    throw new Error(apiResponse.message);
  }
  
  return {
    items: apiResponse.data,
    pagination: {
      totalCount: apiResponse.metadata.totalCount,
      pageNumber: apiResponse.metadata.pageNumber,
      pageSize: apiResponse.metadata.pageSize,
      totalPages: apiResponse.metadata.totalPages,
      hasNextPage: apiResponse.metadata.hasNextPage,
      hasPreviousPage: apiResponse.metadata.hasPreviousPage
    }
  };
}
```

## 📊 **New API Endpoints Available**

### **Database Configuration Management**
```javascript
// Get all database connections
GET /api/database-monitoring/config/connections

// Create new database connection
POST /api/database-monitoring/config/connections
Body: {
  connectionName: string,
  provider: string,
  connectionString: string,
  environment: string,
  tags?: string[],
  connectionTimeoutSeconds?: number,
  commandTimeoutSeconds?: number,
  isEnabled?: boolean
}

// Update database connection
PUT /api/database-monitoring/config/connections/{id}

// Delete database connection
DELETE /api/database-monitoring/config/connections/{id}

// Test database connection
POST /api/database-monitoring/config/connections/{id}/test
```

### **Enhanced Dashboard Endpoints**
```javascript
// Real-time dashboard summary
GET /api/database-monitoring/dashboard

// Historical trends and analytics
GET /api/database-monitoring/trends?period=24h&connectionName=optional

// Paginated monitoring results with filtering
GET /api/database-monitoring/results?page=1&pageSize=20&status=healthy&environment=production
```

## ⚡ **Performance Improvements**

### **Processing Time Tracking**
All responses now include processing time in metadata:
```javascript
const response = await fetch('/api/worker/status');
const apiResponse = await response.json();
console.log(`Request took ${apiResponse.metadata.processingTimeMs}ms`);
```

### **Caching Headers**
Responses now include appropriate caching headers for better performance.

## 🔒 **Security Enhancements**

### **Enhanced Error Responses**
- Sensitive information is never exposed in error messages
- Detailed error information only in development environment
- Structured error responses for better handling

### **Input Validation**
- All endpoints now have comprehensive input validation
- Validation errors return detailed field-specific messages

## 🚀 **Migration Guide**

### **Step 1: Update API Client Library**
Create a new API client wrapper:

```javascript
class MonitoringApiClient {
  async request(url, options = {}) {
    const response = await fetch(url, {
      headers: {
        'Content-Type': 'application/json',
        ...options.headers
      },
      ...options
    });
    
    const apiResponse = await response.json();
    
    if (!apiResponse.success) {
      throw new ApiError(apiResponse.message, apiResponse.errors);
    }
    
    return {
      data: apiResponse.data,
      metadata: apiResponse.metadata
    };
  }
  
  async get(url) {
    return this.request(url);
  }
  
  async post(url, data) {
    return this.request(url, {
      method: 'POST',
      body: JSON.stringify(data)
    });
  }
  
  // ... other HTTP methods
}

class ApiError extends Error {
  constructor(message, errors = []) {
    super(message);
    this.errors = errors;
  }
}
```

### **Step 2: Update Existing Components**
Replace direct API calls with the new client:

```javascript
// Old
const workerStatus = await fetch('/api/worker/status').then(r => r.json());

// New
const { data: workerStatus } = await apiClient.get('/api/worker/status');
```

### **Step 3: Add Pagination Support**
Update list components to handle pagination:

```javascript
function MonitoringResultsList() {
  const [results, setResults] = useState([]);
  const [pagination, setPagination] = useState({});
  
  const loadResults = async (page = 1) => {
    const { data, metadata } = await apiClient.get(
      `/api/database-monitoring/results?page=${page}&pageSize=20`
    );
    setResults(data);
    setPagination(metadata);
  };
  
  // Component implementation...
}
```

## 🎯 **Benefits for Frontend**

### **Consistency**
- All API responses follow the same format
- Predictable error handling
- Standardized metadata

### **Enhanced Features**
- Built-in pagination support
- Performance tracking
- Better error information

### **Developer Experience**
- TypeScript-friendly response format
- Comprehensive error details
- Processing time visibility

## ⚠️ **Breaking Changes Summary**

1. **Response Format**: All responses are now wrapped in `ApiResponse<T>`
2. **Data Access**: Access response data through `.data` property
3. **Error Handling**: Check `.success` property and handle `.errors` array
4. **Pagination**: Use metadata for pagination information

## 📝 **Action Items for Frontend Team**

1. **Update API client library** to handle new response format
2. **Modify error handling** to use new error structure
3. **Implement pagination** for list components
4. **Test all existing API calls** with new format
5. **Update TypeScript interfaces** to match new response format

The new API format provides much better consistency, error handling, and developer experience, but requires updates to existing frontend code.
