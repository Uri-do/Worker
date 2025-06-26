# MonitoringWorker - Complete Monitoring Setup Guide

## 🎯 Overview

This guide provides step-by-step instructions to set up a complete monitoring stack for the MonitoringWorker service using Prometheus and Grafana.

## 📋 Prerequisites

- .NET 8 SDK
- Windows 10/11 or Windows Server
- PowerShell 5.1 or later
- Internet connection for downloading tools

## 🚀 Quick Start

### 1. Start the Monitoring Stack

```powershell
# Start Prometheus (metrics collection)
.\scripts\start-monitoring.ps1

# Verify Prometheus is running
# Visit: http://localhost:9090
```

### 2. Start Your MonitoringWorker

```powershell
# Build and run the worker
dotnet run --project src/MonitoringWorker

# Or run the published version
dotnet src/MonitoringWorker/bin/Release/net8.0/MonitoringWorker.dll
```

### 3. Install and Start Grafana

```powershell
# Download and install Grafana
.\scripts\setup-grafana-windows.ps1 -Download

# Start Grafana
.\scripts\setup-grafana-windows.ps1 -Start

# Visit: http://localhost:3000
# Login: admin / admin123
```

## 📊 Available Dashboards

### MonitoringWorker Overview Dashboard
- **File**: `grafana/dashboards/monitoring-worker-overview.json`
- **Features**:
  - Worker uptime and health status
  - Job execution rates (started, completed, failed)
  - Health check results (healthy, unhealthy, error)
  - Success rates and performance percentiles
  - Real-time metrics visualization

### System Performance Dashboard
- **File**: `grafana/dashboards/system-performance.json`
- **Features**:
  - CPU and memory usage
  - HTTP request metrics
  - Active request counts
  - Resource utilization trends

## 🔧 Configuration

### Prometheus Configuration
- **Config File**: `prometheus/prometheus.yml`
- **Scrape Interval**: 15 seconds
- **Targets**: MonitoringWorker on `localhost:5000/metrics`
- **Retention**: 15 days
- **Web UI**: http://localhost:9090

### Grafana Configuration
- **Config File**: `tools/grafana/conf/custom.ini`
- **Port**: 3000
- **Admin User**: admin
- **Admin Password**: admin123
- **Data Source**: Prometheus (http://localhost:9090)

## 📈 Available Metrics

### Worker Metrics
```
monitoring_worker_uptime_seconds - Worker uptime in seconds
monitoring_worker_jobs_started_total - Total jobs started
monitoring_worker_jobs_completed_total - Total jobs completed
monitoring_worker_jobs_failed_total - Total jobs failed
monitoring_worker_checks_total{status} - Health checks by status
monitoring_worker_check_duration_milliseconds - Check duration histogram
```

### System Metrics
```
process_cpu_seconds_total - CPU usage
process_resident_memory_bytes - Memory usage
http_server_requests_total - HTTP request counts
http_server_active_requests - Active HTTP requests
```

## 🎛️ Management Commands

### Prometheus
```powershell
# Start Prometheus
.\scripts\start-monitoring.ps1

# Stop Prometheus
.\scripts\stop-monitoring.ps1

# Check status
.\scripts\setup-prometheus-windows.ps1 -Status
```

### Grafana
```powershell
# Download Grafana
.\scripts\setup-grafana-windows.ps1 -Download

# Start Grafana
.\scripts\setup-grafana-windows.ps1 -Start

# Stop Grafana
.\scripts\setup-grafana-windows.ps1 -Stop

# Check status
.\scripts\setup-grafana-windows.ps1 -Status
```

## 🔍 Accessing the Monitoring Stack

### Prometheus
- **Web UI**: http://localhost:9090
- **Targets**: http://localhost:9090/targets
- **Metrics Explorer**: http://localhost:9090/graph
- **Configuration**: http://localhost:9090/config

### Grafana
- **Web UI**: http://localhost:3000
- **Login**: admin / admin123
- **Data Sources**: http://localhost:3000/datasources
- **Dashboards**: http://localhost:3000/dashboards

### MonitoringWorker
- **Health Check**: http://localhost:5000/health
- **Metrics Endpoint**: http://localhost:5000/metrics
- **API Documentation**: http://localhost:5000/swagger

## 📋 Setup Checklist

- [ ] Prometheus installed and running on port 9090
- [ ] MonitoringWorker running and exposing metrics on port 5000
- [ ] Grafana installed and running on port 3000
- [ ] Prometheus data source configured in Grafana
- [ ] Dashboards imported and displaying data
- [ ] All services accessible via web browsers

## 🔧 Troubleshooting

### Prometheus Issues
```powershell
# Check if Prometheus is running
Get-Process -Name "prometheus" -ErrorAction SilentlyContinue

# Check Prometheus logs
Get-Content ".\data\prometheus\prometheus.log" -Tail 20

# Verify targets are being scraped
# Visit: http://localhost:9090/targets
```

### Grafana Issues
```powershell
# Check if Grafana is running
Get-Process -Name "grafana-server" -ErrorAction SilentlyContinue

# Check Grafana logs
Get-Content ".\data\grafana\logs\grafana.log" -Tail 20

# Reset Grafana admin password
# Stop Grafana, delete .\data\grafana\grafana.db, restart
```

### MonitoringWorker Issues
```powershell
# Check if metrics endpoint is accessible
Invoke-WebRequest -Uri "http://localhost:5000/metrics" -UseBasicParsing

# Check health endpoint
Invoke-WebRequest -Uri "http://localhost:5000/health" -UseBasicParsing

# Check application logs
# Logs are written to console and structured logging output
```

## 🎯 Next Steps

1. **Customize Dashboards**: Modify the provided dashboard JSON files to suit your needs
2. **Add Alerting**: Configure Prometheus alerting rules for critical conditions
3. **Set Up Notifications**: Configure Grafana notification channels (email, Slack, etc.)
4. **Add More Metrics**: Extend the MetricsService to collect additional application metrics
5. **Performance Tuning**: Adjust scrape intervals and retention policies based on your requirements

## 📚 Additional Resources

- [Prometheus Documentation](https://prometheus.io/docs/)
- [Grafana Documentation](https://grafana.com/docs/)
- [OpenTelemetry .NET Documentation](https://opentelemetry.io/docs/instrumentation/net/)
- [ASP.NET Core Metrics](https://docs.microsoft.com/en-us/aspnet/core/log-mon/metrics/)

---

**Happy Monitoring!** 🎉
