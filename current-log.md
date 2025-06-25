[15:15:30 INF] Starting MonitoringWorker application {"SourceContext": "Program", "MachineName": "IL-NB-URI", "ThreadId": 1, "EnvironmentName": "Development"}
[15:15:30 INF] Starting MonitoringWorker application
[15:15:30 INF] Environment: Development {"SourceContext": "Program", "MachineName": "IL-NB-URI", "ThreadId": 1, "EnvironmentName": "Development"}
[15:15:30 INF] Environment: Development
[15:15:30 INF] Application started at: 06/25/2025 12:15:30 +00:00 {"SourceContext": "Program", "MachineName": "IL-NB-URI", "ThreadId": 1, "EnvironmentName": "Development"}
[15:15:30 INF] Application started at: 06/25/2025 12:15:30 +00:00
[15:15:30 INF] User profile is available. Using 'C:\Users\Work\AppData\Local\ASP.NET\DataProtection-Keys' as key repository and Windows DPAPI to encrypt keys at rest. {"EventId": {"Id": 63, "Name": "UsingProfileAsKeyRepositoryWithDPAPI"}, "SourceContext": "Microsoft.AspNetCore.DataProtection.KeyManagement.XmlKeyManager", "MachineName": "IL-NB-URI", "ThreadId": 1, "EnvironmentName": "Development"}
[15:15:30 INF] User profile is available. Using 'C:\Users\Work\AppData\Local\ASP.NET\DataProtection-Keys' as key repository and Windows DPAPI to encrypt keys at rest.
[15:15:30 INF] Initialized Scheduler Signaller of type: Quartz.Core.SchedulerSignalerImpl {"SourceContext": "Quartz.Core.SchedulerSignalerImpl", "MachineName": "IL-NB-URI", "ThreadId": 1, "EnvironmentName": "Development"}
[15:15:30 INF] Initialized Scheduler Signaller of type: Quartz.Core.SchedulerSignalerImpl
[15:15:30 INF] Quartz Scheduler created {"SourceContext": "Quartz.Core.QuartzScheduler", "MachineName": "IL-NB-URI", "ThreadId": 1, "EnvironmentName": "Development"}
[15:15:30 INF] Quartz Scheduler created
[15:15:30 INF] JobFactory set to: Quartz.Simpl.MicrosoftDependencyInjectionJobFactory {"SourceContext": "Quartz.Core.QuartzScheduler", "MachineName": "IL-NB-URI", "ThreadId": 1, "EnvironmentName": "Development"}
[15:15:30 INF] JobFactory set to: Quartz.Simpl.MicrosoftDependencyInjectionJobFactory
[15:15:30 INF] RAMJobStore initialized. {"SourceContext": "Quartz.Simpl.RAMJobStore", "MachineName": "IL-NB-URI", "ThreadId": 1, "EnvironmentName": "Development"}
[15:15:30 INF] RAMJobStore initialized.
[15:15:30 INF] Quartz Scheduler 3.8.0.0 - 'QuartzScheduler' with instanceId 'NON_CLUSTERED' initialized {"SourceContext": "Quartz.Impl.StdSchedulerFactory", "MachineName": "IL-NB-URI", "ThreadId": 1, "EnvironmentName": "Development"}
[15:15:30 INF] Quartz Scheduler 3.8.0.0 - 'QuartzScheduler' with instanceId 'NON_CLUSTERED' initialized
[15:15:30 INF] Using thread pool 'Quartz.Simpl.DefaultThreadPool', size: 10 {"SourceContext": "Quartz.Impl.StdSchedulerFactory", "MachineName": "IL-NB-URI", "ThreadId": 1, "EnvironmentName": "Development"}
[15:15:30 INF] Using thread pool 'Quartz.Simpl.DefaultThreadPool', size: 10
[15:15:30 INF] Using job store 'Quartz.Simpl.RAMJobStore', supports persistence: False, clustered: False {"SourceContext": "Quartz.Impl.StdSchedulerFactory", "MachineName": "IL-NB-URI", "ThreadId": 1, "EnvironmentName": "Development"}
[15:15:30 INF] Using job store 'Quartz.Simpl.RAMJobStore', supports persistence: False, clustered: False
[15:15:30 INF] Adding 1 jobs, 1 triggers. {"SourceContext": "Quartz.ContainerConfigurationProcessor", "MachineName": "IL-NB-URI", "ThreadId": 1, "EnvironmentName": "Development"}
[15:15:30 INF] Adding 1 jobs, 1 triggers.
[15:15:30 INF] Adding job: DEFAULT.monitoring-job {"SourceContext": "Quartz.ContainerConfigurationProcessor", "MachineName": "IL-NB-URI", "ThreadId": 1, "EnvironmentName": "Development"}
[15:15:30 INF] Adding job: DEFAULT.monitoring-job
[15:15:30 INF] Starting Monitoring Worker service {"SourceContext": "MonitoringWorker.Worker", "MachineName": "IL-NB-URI", "ThreadId": 1, "EnvironmentName": "Development"}
[15:15:30 INF] Starting Monitoring Worker service
[15:15:30 INF] Monitoring Worker started at 06/25/2025 12:15:30 +00:00. Configured endpoints: 2 {"SourceContext": "MonitoringWorker.Worker", "MachineName": "IL-NB-URI", "ThreadId": 1, "EnvironmentName": "Development"}
[15:15:30 INF] Monitoring Worker started at 06/25/2025 12:15:30 +00:00. Configured endpoints: 2
[15:15:30 INF] Monitoring endpoint: LocalTest -> http://localhost:5000/healthz/live (timeout: 5s) {"SourceContext": "MonitoringWorker.Worker", "MachineName": "IL-NB-URI", "ThreadId": 1, "EnvironmentName": "Development"}
[15:15:30 INF] Monitoring endpoint: LocalTest -> http://localhost:5000/healthz/live (timeout: 5s)
[15:15:30 INF] Monitoring endpoint: ExternalAPI -> https://httpbin.org/status/200 (timeout: 10s) {"SourceContext": "MonitoringWorker.Worker", "MachineName": "IL-NB-URI", "ThreadId": 1, "EnvironmentName": "Development"}
[15:15:30 INF] Monitoring endpoint: ExternalAPI -> https://httpbin.org/status/200 (timeout: 10s)
[15:15:31 INF] Now listening on: https://localhost:56568 {"EventId": {"Id": 14, "Name": "ListeningOnAddress"}, "SourceContext": "Microsoft.Hosting.Lifetime", "MachineName": "IL-NB-URI", "ThreadId": 1, "EnvironmentName": "Development"}
[15:15:31 INF] Now listening on: https://localhost:56568
[15:15:31 INF] Now listening on: http://localhost:56569 {"EventId": {"Id": 14, "Name": "ListeningOnAddress"}, "SourceContext": "Microsoft.Hosting.Lifetime", "MachineName": "IL-NB-URI", "ThreadId": 1, "EnvironmentName": "Development"}
[15:15:31 INF] Now listening on: http://localhost:56569
[15:15:31 INF] Application started. Press Ctrl+C to shut down. {"SourceContext": "Microsoft.Hosting.Lifetime", "MachineName": "IL-NB-URI", "ThreadId": 1, "EnvironmentName": "Development"}
[15:15:31 INF] Application started. Press Ctrl+C to shut down.
[15:15:31 INF] Scheduler QuartzScheduler_$_NON_CLUSTERED started. {"SourceContext": "Quartz.Core.QuartzScheduler", "MachineName": "IL-NB-URI", "ThreadId": 10, "EnvironmentName": "Development"}
[15:15:31 INF] Scheduler QuartzScheduler_$_NON_CLUSTERED started.
[15:15:31 INF] Hosting environment: Development {"SourceContext": "Microsoft.Hosting.Lifetime", "MachineName": "IL-NB-URI", "ThreadId": 1, "EnvironmentName": "Development"}
[15:15:31 INF] Hosting environment: Development
[15:15:31 INF] Content root path: C:\Dev\code\Worker {"SourceContext": "Microsoft.Hosting.Lifetime", "MachineName": "IL-NB-URI", "ThreadId": 1, "EnvironmentName": "Development"}
[15:15:31 INF] Content root path: C:\Dev\code\Worker
[15:15:31 INF] Starting monitoring job 79e80ed9-f0b9-456c-86bc-55c960b39575 at 06/25/2025 12:15:31 +00:00 {"SourceContext": "MonitoringWorker.Jobs.MonitoringJob", "MachineName": "IL-NB-URI", "ThreadId": 15, "EnvironmentName": "Development"}
[15:15:31 INF] Starting monitoring job 79e80ed9-f0b9-456c-86bc-55c960b39575 at 06/25/2025 12:15:31 +00:00
[15:15:31 DBG] Recorded job start {"SourceContext": "MonitoringWorker.Services.MetricsService", "MachineName": "IL-NB-URI", "ThreadId": 15, "EnvironmentName": "Development"}
[15:15:31 DBG] Recorded job start
[15:15:31 INF] Starting monitoring checks for 2 endpoints {"SourceContext": "MonitoringWorker.Services.MonitoringService", "MachineName": "IL-NB-URI", "ThreadId": 15, "EnvironmentName": "Development"}
[15:15:31 INF] Starting monitoring checks for 2 endpoints
[15:15:31 DBG] Checking endpoint LocalTest at http://localhost:5000/healthz/live {"SourceContext": "MonitoringWorker.Services.MonitoringService", "MachineName": "IL-NB-URI", "ThreadId": 15, "EnvironmentName": "Development"}
[15:15:31 DBG] Checking endpoint LocalTest at http://localhost:5000/healthz/live
[15:15:31 INF] Start processing HTTP request GET http://localhost:5000/healthz/live {"EventId": {"Id": 100, "Name": "RequestPipelineStart"}, "SourceContext": "System.Net.Http.HttpClient.IMonitoringService.LogicalHandler", "Scope": ["HTTP GET http://localhost:5000/healthz/live"], "MachineName": "IL-NB-URI", "ThreadId": 15, "EnvironmentName": "Development"}
[15:15:31 INF] Start processing HTTP request GET http://localhost:5000/healthz/live
[15:15:31 INF] Sending HTTP request GET http://localhost:5000/healthz/live {"EventId": {"Id": 100, "Name": "RequestStart"}, "SourceContext": "System.Net.Http.HttpClient.IMonitoringService.ClientHandler", "Scope": ["HTTP GET http://localhost:5000/healthz/live"], "MachineName": "IL-NB-URI", "ThreadId": 15, "EnvironmentName": "Development"}
[15:15:31 INF] Sending HTTP request GET http://localhost:5000/healthz/live
[15:15:31 DBG] Checking endpoint ExternalAPI at https://httpbin.org/status/200 {"SourceContext": "MonitoringWorker.Services.MonitoringService", "MachineName": "IL-NB-URI", "ThreadId": 15, "EnvironmentName": "Development"}
[15:15:31 DBG] Checking endpoint ExternalAPI at https://httpbin.org/status/200
[15:15:31 INF] Start processing HTTP request GET https://httpbin.org/status/200 {"EventId": {"Id": 100, "Name": "RequestPipelineStart"}, "SourceContext": "System.Net.Http.HttpClient.IMonitoringService.LogicalHandler", "Scope": ["HTTP GET https://httpbin.org/status/200"], "MachineName": "IL-NB-URI", "ThreadId": 15, "EnvironmentName": "Development"}
[15:15:31 INF] Start processing HTTP request GET https://httpbin.org/status/200
[15:15:31 INF] Sending HTTP request GET https://httpbin.org/status/200 {"EventId": {"Id": 100, "Name": "RequestStart"}, "SourceContext": "System.Net.Http.HttpClient.IMonitoringService.ClientHandler", "Scope": ["HTTP GET https://httpbin.org/status/200"], "MachineName": "IL-NB-URI", "ThreadId": 15, "EnvironmentName": "Development"}
[15:15:31 INF] Sending HTTP request GET https://httpbin.org/status/200
[15:15:33 INF] Received HTTP response headers after 2752.2317ms - 200 {"EventId": {"Id": 101, "Name": "RequestEnd"}, "SourceContext": "System.Net.Http.HttpClient.IMonitoringService.ClientHandler", "HttpMethod": "GET", "Uri": "https://httpbin.org/status/200", "Scope": ["HTTP GET https://httpbin.org/status/200"], "MachineName": "IL-NB-URI", "ThreadId": 17, "EnvironmentName": "Development"}
[15:15:33 INF] Received HTTP response headers after 2752.2317ms - 200
[15:15:33 INF] End processing HTTP request after 2771.2885ms - 200 {"EventId": {"Id": 101, "Name": "RequestPipelineEnd"}, "SourceContext": "System.Net.Http.HttpClient.IMonitoringService.LogicalHandler", "HttpMethod": "GET", "Uri": "https://httpbin.org/status/200", "Scope": ["HTTP GET https://httpbin.org/status/200"], "MachineName": "IL-NB-URI", "ThreadId": 17, "EnvironmentName": "Development"}
[15:15:33 INF] End processing HTTP request after 2771.2885ms - 200
[15:15:33 DBG] Endpoint ExternalAPI check completed: Healthy in 2784ms {"SourceContext": "MonitoringWorker.Services.MonitoringService", "MachineName": "IL-NB-URI", "ThreadId": 17, "EnvironmentName": "Development"}
[15:15:33 DBG] Endpoint ExternalAPI check completed: Healthy in 2784ms
[15:15:34 INF] Request starting HTTP/2 GET https://localhost:56568/ - null null {"EventId": {"Id": 1}, "SourceContext": "Microsoft.AspNetCore.Hosting.Diagnostics", "RequestId": "0HNDJS7TEFDFF:00000001", "RequestPath": "/", "ConnectionId": "0HNDJS7TEFDFF", "MachineName": "IL-NB-URI", "ThreadId": 5, "EnvironmentName": "Development"}
[15:15:34 INF] Request starting HTTP/2 GET https://localhost:56568/ - null null
[15:15:34 INF] Request finished HTTP/2 GET https://localhost:56568/ - 404 0 null 135.8879ms {"EventId": {"Id": 2}, "SourceContext": "Microsoft.AspNetCore.Hosting.Diagnostics", "RequestId": "0HNDJS7TEFDFF:00000001", "RequestPath": "/", "ConnectionId": "0HNDJS7TEFDFF", "MachineName": "IL-NB-URI", "ThreadId": 17, "EnvironmentName": "Development"}
[15:15:34 INF] Request finished HTTP/2 GET https://localhost:56568/ - 404 0 null 135.8879ms
[15:15:34 INF] Request reached the end of the middleware pipeline without being handled by application code. Request path: GET https://localhost:56568/, Response status code: 404 {"EventId": {"Id": 16}, "SourceContext": "Microsoft.AspNetCore.Hosting.Diagnostics", "RequestId": "0HNDJS7TEFDFF:00000001", "RequestPath": "/", "ConnectionId": "0HNDJS7TEFDFF", "MachineName": "IL-NB-URI", "ThreadId": 17, "EnvironmentName": "Development"}
[15:15:34 INF] Request reached the end of the middleware pipeline without being handled by application code. Request path: GET https://localhost:56568/, Response status code: 404
[15:15:35 WRN] HTTP retry 1 after 2000ms {"HttpMethod": "GET", "Uri": "http://localhost:5000/healthz/live", "Scope": ["HTTP GET http://localhost:5000/healthz/live"], "MachineName": "IL-NB-URI", "ThreadId": 15, "EnvironmentName": "Development"}
[15:15:35 WRN] HTTP retry 1 after 2000ms
[15:15:36 WRN] Endpoint LocalTest check timed out after 5072ms {"SourceContext": "MonitoringWorker.Services.MonitoringService", "MachineName": "IL-NB-URI", "ThreadId": 17, "EnvironmentName": "Development"}
[15:15:36 WRN] Endpoint LocalTest check timed out after 5072ms
[15:15:36 INF] Completed monitoring checks. Results: 1 healthy, 0 unhealthy, 1 errors {"SourceContext": "MonitoringWorker.Services.MonitoringService", "MachineName": "IL-NB-URI", "ThreadId": 17, "EnvironmentName": "Development"}
[15:15:36 INF] Completed monitoring checks. Results: 1 healthy, 0 unhealthy, 1 errors
[15:15:36 DBG] Processing result for check LocalTest: Error in 5072ms {"SourceContext": "MonitoringWorker.Jobs.MonitoringJob", "MachineName": "IL-NB-URI", "ThreadId": 17, "EnvironmentName": "Development"}
[15:15:36 DBG] Processing result for check LocalTest: Error in 5072ms
[15:15:36 DBG] Sent monitoring event 27297385-913e-4830-813d-8b5b6c7b9bbf for check LocalTest with status Error to all clients {"SourceContext": "MonitoringWorker.Services.EventNotificationService", "MachineName": "IL-NB-URI", "ThreadId": 17, "EnvironmentName": "Development"}
[15:15:36 DBG] Sent monitoring event 27297385-913e-4830-813d-8b5b6c7b9bbf for check LocalTest with status Error to all clients
[15:15:36 WRN] Check LocalTest is Error: Request timed out {"SourceContext": "MonitoringWorker.Jobs.MonitoringJob", "MachineName": "IL-NB-URI", "ThreadId": 17, "EnvironmentName": "Development"}
[15:15:36 WRN] Check LocalTest is Error: Request timed out
[15:15:36 DBG] Processing result for check ExternalAPI: Healthy in 2784ms {"SourceContext": "MonitoringWorker.Jobs.MonitoringJob", "MachineName": "IL-NB-URI", "ThreadId": 17, "EnvironmentName": "Development"}
[15:15:36 DBG] Processing result for check ExternalAPI: Healthy in 2784ms
[15:15:36 DBG] Sent monitoring event 29a41f5d-31ec-4521-abfb-af0553a3822c for check ExternalAPI with status Healthy to all clients {"SourceContext": "MonitoringWorker.Services.EventNotificationService", "MachineName": "IL-NB-URI", "ThreadId": 17, "EnvironmentName": "Development"}
[15:15:36 DBG] Sent monitoring event 29a41f5d-31ec-4521-abfb-af0553a3822c for check ExternalAPI with status Healthy to all clients
[15:15:36 DBG] Recorded job success {"SourceContext": "MonitoringWorker.Services.MetricsService", "MachineName": "IL-NB-URI", "ThreadId": 17, "EnvironmentName": "Development"}
[15:15:36 DBG] Recorded job success
[15:15:36 INF] Monitoring job 79e80ed9-f0b9-456c-86bc-55c960b39575 completed successfully in 5177ms {"SourceContext": "MonitoringWorker.Jobs.MonitoringJob", "MachineName": "IL-NB-URI", "ThreadId": 17, "EnvironmentName": "Development"}
[15:15:36 INF] Monitoring job 79e80ed9-f0b9-456c-86bc-55c960b39575 completed successfully in 5177ms
[15:16:00 INF] Starting monitoring job ec48e476-fed5-4e5c-9cc1-d02b54ae896d at 06/25/2025 12:16:00 +00:00 {"SourceContext": "MonitoringWorker.Jobs.MonitoringJob", "MachineName": "IL-NB-URI", "ThreadId": 12, "EnvironmentName": "Development"}
[15:16:00 INF] Starting monitoring job ec48e476-fed5-4e5c-9cc1-d02b54ae896d at 06/25/2025 12:16:00 +00:00
[15:16:00 DBG] Recorded job start {"SourceContext": "MonitoringWorker.Services.MetricsService", "MachineName": "IL-NB-URI", "ThreadId": 12, "EnvironmentName": "Development"}
[15:16:00 DBG] Recorded job start
[15:16:00 INF] Starting monitoring checks for 2 endpoints {"SourceContext": "MonitoringWorker.Services.MonitoringService", "MachineName": "IL-NB-URI", "ThreadId": 12, "EnvironmentName": "Development"}
[15:16:00 INF] Starting monitoring checks for 2 endpoints
[15:16:00 DBG] Checking endpoint LocalTest at http://localhost:5000/healthz/live {"SourceContext": "MonitoringWorker.Services.MonitoringService", "MachineName": "IL-NB-URI", "ThreadId": 12, "EnvironmentName": "Development"}
[15:16:00 DBG] Checking endpoint LocalTest at http://localhost:5000/healthz/live
[15:16:00 INF] Start processing HTTP request GET http://localhost:5000/healthz/live {"EventId": {"Id": 100, "Name": "RequestPipelineStart"}, "SourceContext": "System.Net.Http.HttpClient.IMonitoringService.LogicalHandler", "Scope": ["HTTP GET http://localhost:5000/healthz/live"], "MachineName": "IL-NB-URI", "ThreadId": 12, "EnvironmentName": "Development"}
[15:16:00 INF] Start processing HTTP request GET http://localhost:5000/healthz/live
[15:16:00 INF] Sending HTTP request GET http://localhost:5000/healthz/live {"EventId": {"Id": 100, "Name": "RequestStart"}, "SourceContext": "System.Net.Http.HttpClient.IMonitoringService.ClientHandler", "Scope": ["HTTP GET http://localhost:5000/healthz/live"], "MachineName": "IL-NB-URI", "ThreadId": 12, "EnvironmentName": "Development"}
[15:16:00 INF] Sending HTTP request GET http://localhost:5000/healthz/live
[15:16:00 DBG] Checking endpoint ExternalAPI at https://httpbin.org/status/200 {"SourceContext": "MonitoringWorker.Services.MonitoringService", "MachineName": "IL-NB-URI", "ThreadId": 12, "EnvironmentName": "Development"}
[15:16:00 DBG] Checking endpoint ExternalAPI at https://httpbin.org/status/200
[15:16:00 INF] Start processing HTTP request GET https://httpbin.org/status/200 {"EventId": {"Id": 100, "Name": "RequestPipelineStart"}, "SourceContext": "System.Net.Http.HttpClient.IMonitoringService.LogicalHandler", "Scope": ["HTTP GET https://httpbin.org/status/200"], "MachineName": "IL-NB-URI", "ThreadId": 12, "EnvironmentName": "Development"}
[15:16:00 INF] Start processing HTTP request GET https://httpbin.org/status/200
[15:16:00 INF] Sending HTTP request GET https://httpbin.org/status/200 {"EventId": {"Id": 100, "Name": "RequestStart"}, "SourceContext": "System.Net.Http.HttpClient.IMonitoringService.ClientHandler", "Scope": ["HTTP GET https://httpbin.org/status/200"], "MachineName": "IL-NB-URI", "ThreadId": 12, "EnvironmentName": "Development"}
[15:16:00 INF] Sending HTTP request GET https://httpbin.org/status/200