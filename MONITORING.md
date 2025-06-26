# 📊 MonitoringWorker Observability Stack

This document describes how to set up and use the complete observability stack for MonitoringWorker, including Prometheus for metrics collection and Grafana for visualization.

## 🏗️ Architecture

```
MonitoringWorker (:5002)
    ↓ /metrics endpoint
Prometheus (:9090)
    ↓ PromQL queries
Grafana (:3000)
    ↓ Dashboards & Alerts
```

## 🚀 Quick Start

### Prerequisites
- Docker Desktop installed and running
- .NET 8 SDK
- PowerShell (for scripts)

### 1. Start the Monitoring Stack

```powershell
# Start Prometheus and Grafana
.\scripts\start-monitoring.ps1

# Or with logs
.\scripts\start-monitoring.ps1 -Logs

# Check status
.\scripts\start-monitoring.ps1 -Status
```

### 2. Start MonitoringWorker

```powershell
# In a separate terminal
dotnet run
```

### 3. Access the Services

| Service | URL | Credentials |
|---------|-----|-------------|
| MonitoringWorker | http://localhost:5002 | - |
| Metrics Endpoint | http://localhost:5002/metrics | - |
| Prometheus | http://localhost:9090 | - |
| Grafana | http://localhost:3000 | admin/admin123 |

## 📈 Available Metrics

### Custom MonitoringWorker Metrics
- `monitoring_worker_heartbeat_total` - Worker heartbeats
- `monitoring_worker_jobs_started_total` - Jobs started
- `monitoring_worker_jobs_completed_total` - Jobs completed
- `monitoring_worker_jobs_failed_total` - Jobs failed
- `monitoring_worker_jobs_cancelled_total` - Jobs cancelled
- `monitoring_worker_checks_total{check_name, status}` - Check results
- `monitoring_worker_check_duration_milliseconds` - Check durations
- `monitoring_worker_uptime_seconds` - Worker uptime
- `monitoring_worker_total_checks` - Total checks performed

### Standard OpenTelemetry Metrics
- `http_server_active_requests` - Active HTTP requests
- `aspnetcore_routing_match_attempts_total` - Routing attempts
- `process_*` - Process metrics (CPU, memory, etc.)

## 🔍 Using Prometheus

### Useful Queries

```promql
# Job success rate (last 5 minutes)
rate(monitoring_worker_jobs_completed_total[5m]) / rate(monitoring_worker_jobs_started_total[5m])

# Check failure rate by check name
rate(monitoring_worker_checks_total{status="unhealthy"}[5m]) by (check_name)

# Average check duration
rate(monitoring_worker_check_duration_milliseconds_sum[5m]) / rate(monitoring_worker_check_duration_milliseconds_count[5m])

# Worker uptime
monitoring_worker_uptime_seconds

# HTTP request rate
rate(http_server_requests_total[1m])
```

### Targets and Service Discovery
- Visit http://localhost:9090/targets to see scrape targets
- Visit http://localhost:9090/service-discovery to see discovered services

## 📊 Grafana Dashboards

Grafana is automatically configured with:
- Prometheus as the default data source
- Dashboard provisioning enabled
- Admin user: `admin` / `admin123`

### Creating Custom Dashboards
1. Go to http://localhost:3000
2. Login with admin/admin123
3. Click "+" → "Dashboard"
4. Add panels with PromQL queries

## 🚨 Alerting Rules

The following alerts are pre-configured:

### Critical Alerts
- **MonitoringWorkerDown**: Worker is unreachable
- **CriticalCheckFailing**: Checks returning errors
- **NoHeartbeat**: No heartbeat for 2+ minutes

### Warning Alerts
- **HighJobFailureRate**: >10% job failure rate
- **HighCheckFailureRate**: >20% check failure rate
- **HighResponseTime**: 95th percentile >5 seconds
- **NoJobsStarted**: No jobs in 10+ minutes

## 🛠️ Configuration

### Prometheus Configuration
- **File**: `prometheus/prometheus.yml`
- **Scrape Interval**: 10 seconds for MonitoringWorker
- **Retention**: 15 days
- **Storage**: 1GB limit

### Grafana Configuration
- **Data Source**: Auto-configured Prometheus
- **Dashboards**: Auto-provisioned from `grafana/dashboards/`
- **Plugins**: Clock panel, Simple JSON datasource

## 🔧 Troubleshooting

### Common Issues

1. **"Target Down" in Prometheus**
   - Ensure MonitoringWorker is running on port 5002
   - Check `docker-compose logs prometheus`

2. **No Data in Grafana**
   - Verify Prometheus data source connection
   - Check time range in Grafana queries

3. **Docker Issues**
   - Restart Docker Desktop
   - Run `docker system prune -f`

### Useful Commands

```powershell
# View logs
docker-compose -f docker-compose.monitoring.yml logs -f

# Restart services
docker-compose -f docker-compose.monitoring.yml restart

# Stop everything
.\scripts\stop-monitoring.ps1

# Stop and remove data
.\scripts\stop-monitoring.ps1 -RemoveData
```

## 📁 File Structure

```
├── prometheus/
│   ├── prometheus.yml          # Main Prometheus config
│   └── rules/
│       └── monitoring-worker.yml  # Alerting rules
├── grafana/
│   ├── provisioning/
│   │   ├── datasources/        # Auto-configured data sources
│   │   └── dashboards/         # Dashboard provisioning
│   └── dashboards/             # Dashboard JSON files
├── scripts/
│   ├── start-monitoring.ps1    # Start script
│   └── stop-monitoring.ps1     # Stop script
└── docker-compose.monitoring.yml  # Docker services
```

## 🎯 Next Steps

1. **Create Custom Dashboards**: Build specific dashboards for your use cases
2. **Set Up Alerting**: Configure AlertManager for notifications
3. **Add More Exporters**: Monitor Windows system metrics
4. **Implement Distributed Tracing**: Add Jaeger for request tracing

## 📚 Resources

- [Prometheus Documentation](https://prometheus.io/docs/)
- [Grafana Documentation](https://grafana.com/docs/)
- [OpenTelemetry .NET](https://opentelemetry.io/docs/instrumentation/net/)
- [PromQL Tutorial](https://prometheus.io/docs/prometheus/latest/querying/basics/)
