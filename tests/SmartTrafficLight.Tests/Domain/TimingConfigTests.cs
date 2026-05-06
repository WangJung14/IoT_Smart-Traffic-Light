using SmartTrafficLight_Domain.ValueObjects;

namespace SmartTrafficLight.Tests.Domain;

/// <summary>
/// Unit tests cho Value Object TimingConfig.
/// Kiểm tra logic validation thời gian đèn.
/// </summary>
public class TimingConfigTests
{
    [Fact]
    public void Constructor_WithValidDurations_ShouldCreateInstance()
    {
        // Arrange & Act
        var config = new TimingConfig(30, 3, 20);

        // Assert
        Assert.Equal(30, config.GreenDuration);
        Assert.Equal(3, config.YellowDuration);
        Assert.Equal(20, config.RedDuration);
    }

    [Fact]
    public void Constructor_WithZeroDurations_ShouldCreateInstance()
    {
        // Arrange & Act
        var config = new TimingConfig(0, 0, 0);

        // Assert
        Assert.Equal(0, config.GreenDuration);
        Assert.Equal(0, config.YellowDuration);
        Assert.Equal(0, config.RedDuration);
    }

    [Theory]
    [InlineData(-1, 3, 20)]
    [InlineData(30, -1, 20)]
    [InlineData(30, 3, -1)]
    [InlineData(-5, -5, -5)]
    public void Constructor_WithNegativeDurations_ShouldThrowArgumentException(int green, int yellow, int red)
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentException>(() => new TimingConfig(green, yellow, red));
    }

    [Fact]
    public void TwoTimingConfigs_WithSameValues_ShouldBeEqual()
    {
        // TimingConfig is a record, so value equality applies
        var config1 = new TimingConfig(30, 3, 20);
        var config2 = new TimingConfig(30, 3, 20);

        Assert.Equal(config1, config2);
    }

    [Fact]
    public void TwoTimingConfigs_WithDifferentValues_ShouldNotBeEqual()
    {
        var config1 = new TimingConfig(30, 3, 20);
        var config2 = new TimingConfig(60, 5, 30);

        Assert.NotEqual(config1, config2);
    }
}
