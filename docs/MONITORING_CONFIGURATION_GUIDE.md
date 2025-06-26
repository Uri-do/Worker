# MonitoringWorker - Configuration Guide

## 🎯 Overview

The MonitoringWorker now supports comprehensive configuration management with feature toggles, allowing you to enable or disable specific monitoring capabilities based on your needs.

## 📋 Configuration Structure

### **Main Configuration Section**

```json
{
  "Monitoring": {
    "Enhanced": {
      "Enabled": true,
      "Metrics": { ... },
      "HealthChecks": { ... },
      "Dashboards": { ... },
      "Alerting": { ... },
      "Performance": { ... }
    }
  }
}
```

### **Metrics Configuration**

```json
{
  "Metrics": {
    "EnableBasicMetrics": true,        // Job counts, health checks
    "EnableDetailedMetrics": true,     // Histograms, percentiles
    "EnableSystemMetrics": true,       // CPU, memory usage
    "EnableHttpMetrics": true,         // HTTP request metrics
    "EnableCustomMetrics": false,      // Custom business metrics
    "EndpointPath": "/metrics",        // Prometheus endpoint
    "CollectionIntervalSeconds": 15    // Collection frequency
  }
}
```

### **Health Checks Configuration**

```json
{
  "HealthChecks": {
    "Enabled": true,                   // Enable health checks
    "EndpointPath": "/health",         // Health check endpoint
    "IntervalSeconds": 30,             // Check frequency
    "EnableDetailedResponses": true,   // Detailed health responses
    "TimeoutSeconds": 30               // Health check timeout
  }
}
```

### **Dashboards Configuration**

```json
{
  "Dashboards": {
    "Enabled": true,                   // Enable dashboard generation
    "EnableBasicDashboard": true,      // Basic monitoring dashboard
    "EnablePerformanceDashboard": true, // Performance metrics dashboard
    "EnableSystemDashboard": true,     // System resource dashboard
    "RefreshIntervalSeconds": 30       // Dashboard refresh rate
  }
}
```

### **Alerting Configuration**

```json
{
  "Alerting": {
    "Enabled": false,                  // Enable alerting rules
    "EnableJobFailureAlerts": true,    // Job failure notifications
    "EnableHealthCheckAlerts": true,   // Health check failure alerts
    "EnablePerformanceAlerts": false,  // Performance degradation alerts
    "EnableSystemResourceAlerts": false, // System resource alerts
    "JobFailureThreshold": 5,          // Failures per minute
    "HealthCheckFailureThreshold": 3,  // Consecutive failures
    "PerformanceThresholdMs": 5000     // 95th percentile threshold
  }
}
```

### **Performance Configuration**

```json
{
  "Performance": {
    "Enabled": true,                   // Enable performance monitoring
    "EnableDurationTracking": true,    // Track request durations
    "EnableMemoryTracking": true,      // Track memory usage
    "EnableCpuTracking": true,         // Track CPU usage
    "EnableGcTracking": false,         // Track garbage collection
    "SamplingRate": 1.0,               // Sampling rate (0.0-1.0)
    "RetentionDays": 30                // Data retention period
  }
}
```

## 🎛️ Configuration Profiles

### **Production Profile** (`appsettings.Production.json`)
- **Full monitoring enabled**
- **All metrics and dashboards active**
- **Alerting enabled with production thresholds**
- **Comprehensive performance tracking**
- **90-day data retention**

### **Development Profile** (`appsettings.Development.json`)
- **Enhanced monitoring for debugging**
- **Detailed responses enabled**
- **Custom metrics available**
- **Shorter intervals for faster feedback**

### **Minimal Profile** (`appsettings.Minimal.json`)
- **Basic metrics only**
- **No dashboards or alerting**
- **Minimal performance impact**
- **Suitable for resource-constrained environments**

### **Disabled Profile** (`appsettings.Disabled.json`)
- **All enhanced monitoring disabled**
- **Minimal overhead**
- **Basic health checks only**
- **Emergency fallback configuration**

## 🚀 Quick Configuration

### **Enable Full Monitoring**
```json
{
  "Monitoring": {
    "Enhanced": {
      "Enabled": true
    }
  }
}
```

### **Basic Metrics Only**
```json
{
  "Monitoring": {
    "Enhanced": {
      "Enabled": true,
      "Metrics": {
        "EnableBasicMetrics": true,
        "EnableDetailedMetrics": false,
        "EnableSystemMetrics": false,
        "EnableHttpMetrics": false
      },
      "Dashboards": { "Enabled": false },
      "Alerting": { "Enabled": false },
      "Performance": { "Enabled": false }
    }
  }
}
```

### **Disable All Enhanced Features**
```json
{
  "Monitoring": {
    "Enhanced": {
      "Enabled": false
    }
  }
}
```

## 📊 API Endpoints

### **Configuration Management**

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/api/monitoringconfiguration` | GET | Get current configuration |
| `/api/monitoringconfiguration/status` | GET | Get configuration status |
| `/api/monitoringconfiguration/features` | GET | Get all feature toggles |
| `/api/monitoringconfiguration/features/{name}` | GET | Check specific feature |
| `/api/monitoringconfiguration/summary` | GET | Get configuration summary |
| `/api/monitoringconfiguration/validate` | POST | Validate configuration |
| `/api/monitoringconfiguration/recommendations` | GET | Get recommendations |

### **Example API Responses**

#### **Configuration Status**
```json
{
  "isValid": true,
  "validationMessage": null,
  "isMonitoringEnabled": true,
  "enabledFeatures": [
    "monitoring",
    "basic-metrics",
    "detailed-metrics",
    "health-checks"
  ],
  "configuration": {
    "metricsEndpoint": "/metrics",
    "healthCheckEndpoint": "/health",
    "metricsInterval": "00:00:15",
    "healthCheckInterval": "00:00:30"
  }
}
```

#### **Feature Status**
```json
{
  "monitoring": true,
  "basic-metrics": true,
  "detailed-metrics": true,
  "system-metrics": false,
  "alerting": false
}
```

## 🔧 Environment-Specific Configuration

### **Using Environment Variables**
```bash
# Enable monitoring
export Monitoring__Enhanced__Enabled=true

# Configure metrics
export Monitoring__Enhanced__Metrics__EnableBasicMetrics=true
export Monitoring__Enhanced__Metrics__CollectionIntervalSeconds=30

# Configure alerting
export Monitoring__Enhanced__Alerting__Enabled=true
export Monitoring__Enhanced__Alerting__JobFailureThreshold=5
```

### **Using Docker Environment Variables**
```yaml
services:
  monitoring-worker:
    environment:
      - Monitoring__Enhanced__Enabled=true
      - Monitoring__Enhanced__Metrics__EnableBasicMetrics=true
      - Monitoring__Enhanced__Alerting__Enabled=false
```

## 🎯 Feature Toggle Usage

### **In Code**
```csharp
// Check if feature is enabled
if (_featureToggleService.IsEnabled("basic-metrics"))
{
    // Record metrics
}

// Execute conditionally
_featureToggleService.ExecuteIfEnabled("detailed-metrics", () =>
{
    // Record detailed metrics
});

// Get configuration value
var interval = _featureToggleService.GetFeatureValue("metrics-collection-interval", 60);
```

## 📈 Performance Impact

### **Configuration Impact Levels**

| Configuration | CPU Impact | Memory Impact | Network Impact |
|---------------|------------|---------------|----------------|
| **Disabled** | None | Minimal | None |
| **Basic Metrics** | Low | Low | Low |
| **Full Monitoring** | Medium | Medium | Medium |
| **All Features** | High | High | High |

### **Recommended Settings by Environment**

| Environment | Profile | Reasoning |
|-------------|---------|-----------|
| **Development** | Full | Maximum observability for debugging |
| **Testing** | Basic | Sufficient for test validation |
| **Staging** | Production | Mirror production configuration |
| **Production** | Production | Full monitoring with alerting |
| **Resource-Limited** | Minimal | Minimal overhead |

## 🔍 Troubleshooting

### **Common Issues**

1. **Metrics not appearing**
   - Check `Monitoring.Enhanced.Enabled = true`
   - Verify `Metrics.EnableBasicMetrics = true`
   - Confirm Prometheus is scraping the correct endpoint

2. **High resource usage**
   - Reduce `CollectionIntervalSeconds`
   - Disable `EnableDetailedMetrics`
   - Lower `SamplingRate`

3. **Configuration validation errors**
   - Use `/api/monitoringconfiguration/validate` endpoint
   - Check logs for validation details
   - Verify all required fields are present

### **Validation Rules**

- `CollectionIntervalSeconds`: 1-3600 seconds
- `IntervalSeconds`: 1-3600 seconds
- `TimeoutSeconds`: 1-300 seconds
- `SamplingRate`: 0.0-1.0
- `RetentionDays`: 1-365 days

## 🎉 Next Steps

1. **Choose your configuration profile** based on environment needs
2. **Test the configuration** using the validation endpoint
3. **Monitor resource usage** and adjust settings as needed
4. **Set up alerting** for production environments
5. **Review recommendations** periodically for optimization

For advanced configuration and custom metrics, see the [Advanced Monitoring Guide](ADVANCED_MONITORING_GUIDE.md).
