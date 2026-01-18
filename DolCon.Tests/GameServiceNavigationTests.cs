namespace DolCon.Tests;

using DolCon.Views;
using FluentAssertions;
using System.Runtime.InteropServices;

public class GameServiceNavigationTests
{
    [Fact]
    public void GetModifierKeyName_ReturnsPlatformSpecificKey()
    {
        // Act
        var result = GameService.GetModifierKeyName();

        // Assert
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            result.Should().Be("Option", "macOS uses the Option key as the modifier");
        }
        else
        {
            result.Should().Be("Alt", "Windows and Linux use the Alt key as the modifier");
        }
    }

    [SkippableFact]
    public void GetModifierKeyName_OnMacOS_ReturnsOption()
    {
        // Skip if not running on macOS
        Skip.IfNot(RuntimeInformation.IsOSPlatform(OSPlatform.OSX), "Test only runs on macOS");

        // Act
        var result = GameService.GetModifierKeyName();

        // Assert
        result.Should().Be("Option");
    }

    [SkippableFact]
    public void GetModifierKeyName_OnWindowsOrLinux_ReturnsAlt()
    {
        // Skip if not running on Windows or Linux
        Skip.IfNot(
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ||
            RuntimeInformation.IsOSPlatform(OSPlatform.Linux),
            "Test only runs on Windows or Linux");

        // Act
        var result = GameService.GetModifierKeyName();

        // Assert
        result.Should().Be("Alt");
    }
}
