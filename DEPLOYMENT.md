# Deployment Guide

This guide covers various deployment scenarios for the MonitoringWorker service.

## 🐳 Docker Deployment

### Single Container

```bash
# Build the image
docker build -t monitoring-worker:latest .

# Run with basic configuration
docker run -d \
  --name monitoring-worker \
  -p 5000:5000 \
  -e Monitoring__DefaultTimeoutSeconds=30 \
  -e Quartz__CronSchedule="0 0/5 * * * ?" \
  monitoring-worker:latest

# Run with custom configuration file
docker run -d \
  --name monitoring-worker \
  -p 5000:5000 \
  -v $(pwd)/appsettings.Production.json:/app/appsettings.Production.json \
  -e ASPNETCORE_ENVIRONMENT=Production \
  monitoring-worker:latest
```

### Docker Compose

```bash
# Development environment
docker-compose up -d

# Production with nginx
docker-compose --profile production up -d

# With Redis for scaling
docker-compose --profile scaling up -d

# View logs
docker-compose logs -f monitoring-worker

# Scale the service
docker-compose up -d --scale monitoring-worker=3
```

## ☸️ Kubernetes Deployment

### Basic Deployment

```yaml
# monitoring-worker-deployment.yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: monitoring-worker
  labels:
    app: monitoring-worker
spec:
  replicas: 2
  selector:
    matchLabels:
      app: monitoring-worker
  template:
    metadata:
      labels:
        app: monitoring-worker
    spec:
      containers:
      - name: monitoring-worker
        image: monitoring-worker:latest
        ports:
        - containerPort: 5000
        env:
        - name: ASPNETCORE_ENVIRONMENT
          value: "Production"
        - name: ASPNETCORE_URLS
          value: "http://+:5000"
        envFrom:
        - configMapRef:
            name: monitoring-config
        - secretRef:
            name: monitoring-secrets
        resources:
          requests:
            memory: "128Mi"
            cpu: "100m"
          limits:
            memory: "512Mi"
            cpu: "500m"
        livenessProbe:
          httpGet:
            path: /healthz/live
            port: 5000
          initialDelaySeconds: 30
          periodSeconds: 30
          timeoutSeconds: 10
        readinessProbe:
          httpGet:
            path: /healthz/ready
            port: 5000
          initialDelaySeconds: 5
          periodSeconds: 10
          timeoutSeconds: 5
        volumeMounts:
        - name: config-volume
          mountPath: /app/appsettings.Production.json
          subPath: appsettings.Production.json
      volumes:
      - name: config-volume
        configMap:
          name: monitoring-config-file

---
apiVersion: v1
kind: Service
metadata:
  name: monitoring-worker-service
spec:
  selector:
    app: monitoring-worker
  ports:
  - protocol: TCP
    port: 80
    targetPort: 5000
  type: ClusterIP

---
apiVersion: v1
kind: ConfigMap
metadata:
  name: monitoring-config
data:
  Monitoring__DefaultTimeoutSeconds: "30"
  Quartz__CronSchedule: "0 0/5 * * * ?"
  Serilog__MinimumLevel__Default: "Information"

---
apiVersion: v1
kind: ConfigMap
metadata:
  name: monitoring-config-file
data:
  appsettings.Production.json: |
    {
      "Monitoring": {
        "DefaultTimeoutSeconds": 30,
        "Endpoints": [
          {
            "Name": "ProductionAPI",
            "Url": "https://api.production.com/health",
            "TimeoutSeconds": 10
          }
        ]
      },
      "Quartz": {
        "CronSchedule": "0 0/5 * * * ?"
      }
    }
```

### Deploy to Kubernetes

```bash
# Apply the deployment
kubectl apply -f monitoring-worker-deployment.yaml

# Check deployment status
kubectl get deployments
kubectl get pods -l app=monitoring-worker

# View logs
kubectl logs -l app=monitoring-worker -f

# Scale the deployment
kubectl scale deployment monitoring-worker --replicas=5

# Update the deployment
kubectl set image deployment/monitoring-worker monitoring-worker=monitoring-worker:v2.0

# Check health endpoints
kubectl port-forward service/monitoring-worker-service 8080:80
curl http://localhost:8080/healthz
```

## 🌐 Cloud Deployments

### Azure Container Instances

```bash
# Create resource group
az group create --name monitoring-rg --location eastus

# Deploy container
az container create \
  --resource-group monitoring-rg \
  --name monitoring-worker \
  --image monitoring-worker:latest \
  --ports 5000 \
  --environment-variables \
    ASPNETCORE_ENVIRONMENT=Production \
    Monitoring__DefaultTimeoutSeconds=30 \
  --restart-policy Always \
  --cpu 1 \
  --memory 2
```

### AWS ECS

```json
{
  "family": "monitoring-worker",
  "networkMode": "awsvpc",
  "requiresCompatibilities": ["FARGATE"],
  "cpu": "256",
  "memory": "512",
  "executionRoleArn": "arn:aws:iam::account:role/ecsTaskExecutionRole",
  "containerDefinitions": [
    {
      "name": "monitoring-worker",
      "image": "monitoring-worker:latest",
      "portMappings": [
        {
          "containerPort": 5000,
          "protocol": "tcp"
        }
      ],
      "environment": [
        {
          "name": "ASPNETCORE_ENVIRONMENT",
          "value": "Production"
        },
        {
          "name": "Monitoring__DefaultTimeoutSeconds",
          "value": "30"
        }
      ],
      "healthCheck": {
        "command": [
          "CMD-SHELL",
          "curl -f http://localhost:5000/healthz/live || exit 1"
        ],
        "interval": 30,
        "timeout": 5,
        "retries": 3,
        "startPeriod": 60
      },
      "logConfiguration": {
        "logDriver": "awslogs",
        "options": {
          "awslogs-group": "/ecs/monitoring-worker",
          "awslogs-region": "us-east-1",
          "awslogs-stream-prefix": "ecs"
        }
      }
    }
  ]
}
```

### Google Cloud Run

```bash
# Build and push to Google Container Registry
docker tag monitoring-worker:latest gcr.io/PROJECT_ID/monitoring-worker:latest
docker push gcr.io/PROJECT_ID/monitoring-worker:latest

# Deploy to Cloud Run
gcloud run deploy monitoring-worker \
  --image gcr.io/PROJECT_ID/monitoring-worker:latest \
  --platform managed \
  --region us-central1 \
  --allow-unauthenticated \
  --port 5000 \
  --set-env-vars ASPNETCORE_ENVIRONMENT=Production \
  --set-env-vars Monitoring__DefaultTimeoutSeconds=30 \
  --memory 512Mi \
  --cpu 1 \
  --min-instances 1 \
  --max-instances 10
```

## 🔧 Configuration Management

### Environment-Specific Configurations

Create separate configuration files for each environment:

```bash
# appsettings.Development.json
{
  "Monitoring": {
    "Endpoints": [
      {
        "Name": "LocalTest",
        "Url": "http://localhost:8080/health"
      }
    ]
  },
  "Quartz": {
    "CronSchedule": "0/30 * * * * ?"
  }
}

# appsettings.Staging.json
{
  "Monitoring": {
    "Endpoints": [
      {
        "Name": "StagingAPI",
        "Url": "https://api.staging.com/health"
      }
    ]
  }
}

# appsettings.Production.json
{
  "Monitoring": {
    "Endpoints": [
      {
        "Name": "ProductionAPI",
        "Url": "https://api.production.com/health"
      }
    ]
  }
}
```

### Secrets Management

#### Kubernetes Secrets

```bash
# Create secret
kubectl create secret generic monitoring-secrets \
  --from-literal=api-key=your-secret-api-key \
  --from-literal=connection-string=your-connection-string

# Use in deployment
env:
- name: API_KEY
  valueFrom:
    secretKeyRef:
      name: monitoring-secrets
      key: api-key
```

#### Azure Key Vault

```csharp
// In Program.cs
builder.Configuration.AddAzureKeyVault(
    new Uri("https://your-keyvault.vault.azure.net/"),
    new DefaultAzureCredential());
```

#### AWS Secrets Manager

```csharp
// In Program.cs
builder.Configuration.AddSecretsManager(region: RegionEndpoint.USEast1);
```

## 📊 Monitoring & Observability

### Prometheus Integration

```csharp
// Add to Program.cs
services.AddOpenTelemetryMetrics(builder =>
{
    builder
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddPrometheusExporter();
});

app.UseOpenTelemetryPrometheusScrapingEndpoint();
```

### Application Insights

```csharp
// Add to Program.cs
services.AddApplicationInsightsTelemetry();
```

### Grafana Dashboard

```json
{
  "dashboard": {
    "title": "MonitoringWorker Dashboard",
    "panels": [
      {
        "title": "Job Success Rate",
        "type": "stat",
        "targets": [
          {
            "expr": "rate(job_completed_total[5m]) / rate(job_started_total[5m])"
          }
        ]
      },
      {
        "title": "Endpoint Response Times",
        "type": "graph",
        "targets": [
          {
            "expr": "check_duration_avg"
          }
        ]
      }
    ]
  }
}
```

## 🚨 Troubleshooting

### Common Deployment Issues

1. **Container won't start**
   ```bash
   # Check logs
   docker logs monitoring-worker
   kubectl logs -l app=monitoring-worker
   
   # Check configuration
   docker exec -it monitoring-worker cat /app/appsettings.json
   ```

2. **Health checks failing**
   ```bash
   # Test health endpoints
   curl http://localhost:5000/healthz/live
   curl http://localhost:5000/healthz/ready
   
   # Check network connectivity
   kubectl exec -it pod-name -- curl http://localhost:5000/healthz
   ```

3. **SignalR connection issues**
   ```bash
   # Check CORS configuration
   # Verify WebSocket support in load balancer
   # Check firewall rules for WebSocket traffic
   ```

### Performance Tuning

```bash
# Monitor resource usage
docker stats monitoring-worker
kubectl top pods -l app=monitoring-worker

# Adjust resource limits
kubectl patch deployment monitoring-worker -p '{"spec":{"template":{"spec":{"containers":[{"name":"monitoring-worker","resources":{"limits":{"memory":"1Gi","cpu":"1000m"}}}]}}}}'
```

## 🔄 Rolling Updates

```bash
# Kubernetes rolling update
kubectl set image deployment/monitoring-worker monitoring-worker=monitoring-worker:v2.0

# Monitor rollout
kubectl rollout status deployment/monitoring-worker

# Rollback if needed
kubectl rollout undo deployment/monitoring-worker
```

## 📋 Deployment Checklist

- [ ] Configuration files reviewed and environment-specific
- [ ] Secrets properly managed and not in source code
- [ ] Health checks configured and tested
- [ ] Resource limits set appropriately
- [ ] Monitoring and logging configured
- [ ] Network policies and security groups configured
- [ ] Backup and disaster recovery plan in place
- [ ] Load testing completed
- [ ] Rollback procedure documented and tested
