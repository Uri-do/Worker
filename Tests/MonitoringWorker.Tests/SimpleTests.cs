using Xunit;

namespace MonitoringWorker.Tests;

/// <summary>
/// Simple tests to verify the testing infrastructure works
/// </summary>
public class SimpleTests
{
    [Fact]
    public void SimpleTest_ShouldPass()
    {
        // Arrange
        var expected = 42;
        
        // Act
        var actual = 42;
        
        // Assert
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData(1, 2, 3)]
    [InlineData(5, 5, 10)]
    [InlineData(-1, 1, 0)]
    public void AddNumbers_ShouldReturnCorrectSum(int a, int b, int expected)
    {
        // Act
        var result = a + b;
        
        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void StringTest_ShouldWork()
    {
        // Arrange
        var input = "MonitoringWorker";
        
        // Act
        var result = input.ToUpper();
        
        // Assert
        Assert.Equal("MONITORINGWORKER", result);
        Assert.Contains("MONITORING", result);
    }
}
