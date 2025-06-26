# MonitoringWorker Testing Guide

## 🎯 Overview

This guide covers comprehensive testing strategies for the MonitoringWorker enhanced monitoring implementation, including unit tests, integration tests, end-to-end scenarios, performance testing, and validation procedures.

## 📋 Test Categories

### **1. Unit Tests** 🧪
- **Purpose**: Test individual components in isolation
- **Coverage**: Services, configuration, feature toggles, models
- **Framework**: xUnit with Moq for mocking
- **Location**: `tests/MonitoringWorker.Tests/Services/`

### **2. Integration Tests** 🔗
- **Purpose**: Test component interactions and API endpoints
- **Coverage**: HTTP endpoints, service dependencies, configuration
- **Framework**: ASP.NET Core Test Host
- **Location**: `tests/MonitoringWorker.Tests/Integration/`

### **3. End-to-End Tests** 🎯
- **Purpose**: Test complete monitoring scenarios
- **Coverage**: Real-world monitoring workflows, failure scenarios
- **Framework**: Custom test scenarios with real services
- **Location**: `tests/MonitoringWorker.Tests/EndToEnd/`

### **4. Performance Tests** 🚀
- **Purpose**: Validate system performance under load
- **Coverage**: High-volume metrics, concurrent requests, memory usage
- **Framework**: Custom load testing with metrics
- **Location**: `tests/MonitoringWorker.Tests/Performance/`

## 🚀 Quick Start

### **Run All Tests**
```powershell
# Run comprehensive test suite
.\scripts\run-tests.ps1 -All -Coverage

# Run specific test categories
.\scripts\run-tests.ps1 -Unit
.\scripts\run-tests.ps1 -Integration
.\scripts\run-tests.ps1 -EndToEnd
.\scripts\run-tests.ps1 -Performance
```

### **Validate Monitoring Stack**
```powershell
# Quick validation
.\scripts\validate-monitoring-stack.ps1 -Quick

# Full validation
.\scripts\validate-monitoring-stack.ps1 -Full

# Validation without external services
.\scripts\validate-monitoring-stack.ps1 -Full -SkipExternal
```

## 🧪 Unit Testing

### **Test Structure**
```csharp
[Fact]
public void MethodName_WithCondition_ExpectedBehavior()
{
    // Arrange
    var mockService = new Mock<IService>();
    var testObject = new TestClass(mockService.Object);

    // Act
    var result = testObject.Method();

    // Assert
    Assert.True(result);
    mockService.Verify(x => x.Method(), Times.Once);
}
```

### **Key Test Areas**

#### **FeatureToggleService Tests**
- ✅ Feature enablement/disablement
- ✅ Configuration value retrieval
- ✅ Conditional execution
- ✅ Case-insensitive feature names
- ✅ Default value handling

#### **NotificationService Tests**
- ✅ Multi-channel notifications
- ✅ Severity filtering
- ✅ Rate limiting
- ✅ Channel testing
- ✅ Error handling

#### **SlaMonitoringService Tests**
- ✅ SLA calculation accuracy
- ✅ Violation detection
- ✅ Report generation
- ✅ Event recording
- ✅ Trend analysis

### **Running Unit Tests**
```powershell
# Run unit tests with coverage
.\scripts\run-tests.ps1 -Unit -Coverage -Verbose

# Run specific test class
dotnet test --filter "ClassName=FeatureToggleServiceTests"

# Run tests matching pattern
dotnet test --filter "TestCategory=Unit&Name~Configuration"
```

## 🔗 Integration Testing

### **Test Scenarios**

#### **API Endpoint Tests**
- ✅ Health endpoint returns correct status
- ✅ Metrics endpoint provides Prometheus format
- ✅ Configuration API returns valid data
- ✅ Feature toggle API works correctly

#### **Service Integration Tests**
- ✅ Dependency injection container setup
- ✅ Service lifecycle management
- ✅ Configuration binding
- ✅ Cross-service communication

### **Test Configuration**
```json
{
  "Monitoring": {
    "Enhanced": {
      "Enabled": true,
      "Metrics": {
        "CollectionIntervalSeconds": 5
      },
      "HealthChecks": {
        "IntervalSeconds": 10
      }
    }
  }
}
```

### **Running Integration Tests**
```powershell
# Run integration tests
.\scripts\run-tests.ps1 -Integration -Verbose

# Run with test server
dotnet test --filter "Category=Integration" --logger "console;verbosity=detailed"
```

## 🎯 End-to-End Testing

### **Test Scenarios**

#### **Normal Operations**
- ✅ Successful job execution
- ✅ Health check monitoring
- ✅ Metrics collection
- ✅ Heartbeat recording

#### **Failure Scenarios**
- ✅ Service failures and recovery
- ✅ SLA violations
- ✅ Performance degradation
- ✅ Alert storm handling

#### **Configuration Scenarios**
- ✅ Feature toggle changes
- ✅ Configuration validation
- ✅ Runtime configuration updates

### **Example Test**
```csharp
[Fact]
public async Task Scenario_ServiceFailures_TriggersCorrectAlerts()
{
    // Simulate failures
    for (int i = 0; i < 5; i++)
    {
        _metricsService.RecordJobStart();
        _metricsService.RecordJobFailure();
    }

    // Verify failure rate
    var metrics = _metricsService.GetMonitoringMetrics();
    Assert.True(metrics.FailedJobs >= 5);
}
```

### **Running End-to-End Tests**
```powershell
# Run E2E tests
.\scripts\run-tests.ps1 -EndToEnd

# Run specific scenario
dotnet test --filter "Name~Scenario_ServiceFailures"
```

## 🚀 Performance Testing

### **Test Categories**

#### **Load Testing**
- ✅ High-volume metrics collection (1000+ ops/sec)
- ✅ Concurrent HTTP requests (50+ users)
- ✅ Alert storm handling (1000+ alerts)
- ✅ Configuration access under load

#### **Resource Testing**
- ✅ Memory usage validation
- ✅ CPU utilization monitoring
- ✅ GC pressure analysis
- ✅ Connection pooling efficiency

#### **Performance Benchmarks**
| Metric | Target | Threshold |
|--------|--------|-----------|
| Throughput | 1000 ops/sec | 800 ops/sec |
| Response Time | <1000ms avg | <2000ms avg |
| Memory Usage | <500MB | <1GB |
| Success Rate | >95% | >90% |

### **Running Performance Tests**
```powershell
# Run performance tests
.\scripts\run-tests.ps1 -Performance

# Run specific load test
dotnet test --filter "Name~LoadTest_HighVolumeMetrics"
```

## 🔍 Validation Testing

### **Stack Validation**

#### **Component Health Checks**
- ✅ MonitoringWorker health endpoint
- ✅ Prometheus connectivity
- ✅ Alertmanager status
- ✅ Grafana accessibility

#### **Metrics Validation**
- ✅ Required metrics presence
- ✅ Metric format correctness
- ✅ Data accuracy verification
- ✅ Collection frequency validation

#### **Configuration Validation**
- ✅ Feature toggle functionality
- ✅ Configuration API responses
- ✅ Setting persistence
- ✅ Validation rules enforcement

### **Running Validation**
```powershell
# Quick validation (MonitoringWorker + Prometheus)
.\scripts\validate-monitoring-stack.ps1 -Quick

# Full validation (all components)
.\scripts\validate-monitoring-stack.ps1 -Full

# Validation without external dependencies
.\scripts\validate-monitoring-stack.ps1 -Full -SkipExternal
```

## 📊 Test Coverage

### **Coverage Targets**
- **Unit Tests**: >90% code coverage
- **Integration Tests**: >80% API coverage
- **End-to-End Tests**: >95% scenario coverage
- **Performance Tests**: All critical paths

### **Generating Coverage Reports**
```powershell
# Generate coverage report
.\scripts\run-tests.ps1 -All -Coverage

# View coverage report
# Open: TestResults/coverage-report/index.html
```

### **Coverage Analysis**
```bash
# Install coverage tools
dotnet tool install -g dotnet-reportgenerator-globaltool

# Generate detailed report
reportgenerator -reports:TestResults/*.cobertura.xml -targetdir:coverage -reporttypes:Html
```

## 🛠️ Test Development

### **Writing Unit Tests**

#### **Best Practices**
1. **Arrange-Act-Assert** pattern
2. **Single responsibility** per test
3. **Descriptive test names**
4. **Mock external dependencies**
5. **Test edge cases**

#### **Example Test**
```csharp
[Theory]
[InlineData(NotificationSeverity.Info, NotificationSeverity.Warning, false)]
[InlineData(NotificationSeverity.Critical, NotificationSeverity.Warning, true)]
public async Task SendNotification_WithSeverityFiltering_RespectsMinSeverity(
    NotificationSeverity messageSeverity,
    NotificationSeverity channelMinSeverity,
    bool shouldSend)
{
    // Test implementation
}
```

### **Writing Integration Tests**

#### **Test Setup**
```csharp
public class IntegrationTestBase : IClassFixture<WebApplicationFactory<Program>>
{
    protected readonly WebApplicationFactory<Program> _factory;
    protected readonly HttpClient _client;

    public IntegrationTestBase(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddJsonFile("appsettings.Test.json");
            });
        });
        _client = _factory.CreateClient();
    }
}
```

### **Writing Performance Tests**

#### **Load Test Pattern**
```csharp
[Fact]
public async Task LoadTest_HighVolume_PerformsWithinLimits()
{
    const int operations = 1000;
    var stopwatch = Stopwatch.StartNew();
    
    var tasks = Enumerable.Range(0, operations)
        .Select(_ => Task.Run(() => PerformOperation()))
        .ToArray();
    
    await Task.WhenAll(tasks);
    stopwatch.Stop();
    
    var throughput = operations / stopwatch.Elapsed.TotalSeconds;
    Assert.True(throughput > 100); // 100 ops/sec minimum
}
```

## 🔧 Troubleshooting

### **Common Test Issues**

#### **Test Failures**
1. **Check dependencies**: Ensure all required services are running
2. **Verify configuration**: Check test configuration files
3. **Review logs**: Examine test output for error details
4. **Isolate tests**: Run individual tests to identify issues

#### **Performance Issues**
1. **Resource constraints**: Check available memory/CPU
2. **Network latency**: Verify network connectivity
3. **Concurrent limits**: Adjust parallelism settings
4. **Timeout values**: Increase timeout for slow operations

#### **Integration Issues**
1. **Service availability**: Ensure external services are accessible
2. **Port conflicts**: Check for port binding conflicts
3. **Configuration mismatch**: Verify test vs. runtime configuration
4. **Dependency versions**: Ensure compatible package versions

### **Debugging Tests**

#### **Visual Studio**
1. Set breakpoints in test methods
2. Use Test Explorer for individual test execution
3. Enable detailed test output
4. Use Live Unit Testing for real-time feedback

#### **Command Line**
```powershell
# Run with detailed output
dotnet test --logger "console;verbosity=detailed"

# Debug specific test
dotnet test --filter "MethodName" --logger "console;verbosity=diagnostic"

# Collect diagnostic data
dotnet test --collect:"XPlat Code Coverage" --collect:"blame"
```

## 📈 Continuous Integration

### **CI Pipeline Integration**
```yaml
# Example GitHub Actions workflow
- name: Run Tests
  run: |
    .\scripts\run-tests.ps1 -All -Coverage
    
- name: Validate Stack
  run: |
    .\scripts\validate-monitoring-stack.ps1 -Full -SkipExternal
    
- name: Upload Coverage
  uses: codecov/codecov-action@v3
  with:
    files: TestResults/coverage.cobertura.xml
```

### **Quality Gates**
- **Unit Test Coverage**: >90%
- **Integration Test Success**: 100%
- **Performance Benchmarks**: Within thresholds
- **Security Scans**: No critical vulnerabilities

## 🎯 Next Steps

1. **Run initial test suite** to establish baseline
2. **Set up CI/CD integration** for automated testing
3. **Configure coverage reporting** for quality tracking
4. **Implement performance monitoring** for regression detection
5. **Create custom test scenarios** for specific use cases

---

## 📚 Additional Resources

- **xUnit Documentation**: https://xunit.net/
- **ASP.NET Core Testing**: https://docs.microsoft.com/en-us/aspnet/core/test/
- **Moq Framework**: https://github.com/moq/moq4
- **Performance Testing**: https://docs.microsoft.com/en-us/dotnet/core/testing/

**Your MonitoringWorker now has comprehensive testing coverage ensuring reliability and performance!** 🚀
