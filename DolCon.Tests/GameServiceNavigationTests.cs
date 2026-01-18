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

    [Fact]
    public void GetModifierKeyName_OnMacOS_ReturnsOption()
    {
        // This test documents the expected behavior on macOS
        // Note: This will only pass when run on macOS
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            // Act
            var result = GameService.GetModifierKeyName();

            // Assert
            result.Should().Be("Option");
        }
    }

    [Fact]
    public void GetModifierKeyName_OnWindowsOrLinux_ReturnsAlt()
    {
        // This test documents the expected behavior on Windows/Linux
        // Note: This will only pass when run on Windows or Linux
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ||
            RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            // Act
            var result = GameService.GetModifierKeyName();

            // Assert
            result.Should().Be("Alt");
        }
    }
}
