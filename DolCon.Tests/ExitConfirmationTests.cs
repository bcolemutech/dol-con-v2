namespace DolCon.Tests;

using DolCon.Enums;
using DolCon.Models;
using DolCon.Views;
using FluentAssertions;

public class ExitConfirmationTests
{
    [Fact]
    public void ShowExitConfirmation_DefaultsToFalse()
    {
        var flow = new Flow();

        flow.ShowExitConfirmation.Should().BeFalse();
    }

    [Fact]
    public void ShowExitConfirmation_CanBeSetToTrue()
    {
        var flow = new Flow { ShowExitConfirmation = true };

        flow.ShowExitConfirmation.Should().BeTrue();
    }

    [Fact]
    public void ProcessExitConfirmation_WithYKey_ReturnsExit()
    {
        var flow = new Flow { ShowExitConfirmation = true };
        var key = new ConsoleKeyInfo('Y', ConsoleKey.Y, false, false, false);

        var result = KeyProcessor.ProcessExitConfirmation(flow, key);

        result.Should().Be(ExitConfirmationResult.Exit);
    }

    [Fact]
    public void ProcessExitConfirmation_WithLowercaseYKey_ReturnsExit()
    {
        var flow = new Flow { ShowExitConfirmation = true };
        var key = new ConsoleKeyInfo('y', ConsoleKey.Y, false, false, false);

        var result = KeyProcessor.ProcessExitConfirmation(flow, key);

        result.Should().Be(ExitConfirmationResult.Exit);
    }

    [Fact]
    public void ProcessExitConfirmation_WithNKey_ReturnsCancelAndClearsFlag()
    {
        var flow = new Flow { ShowExitConfirmation = true };
        var key = new ConsoleKeyInfo('N', ConsoleKey.N, false, false, false);

        var result = KeyProcessor.ProcessExitConfirmation(flow, key);

        result.Should().Be(ExitConfirmationResult.Cancel);
        flow.ShowExitConfirmation.Should().BeFalse();
    }

    [Fact]
    public void ProcessExitConfirmation_WithLowercaseNKey_ReturnsCancelAndClearsFlag()
    {
        var flow = new Flow { ShowExitConfirmation = true };
        var key = new ConsoleKeyInfo('n', ConsoleKey.N, false, false, false);

        var result = KeyProcessor.ProcessExitConfirmation(flow, key);

        result.Should().Be(ExitConfirmationResult.Cancel);
        flow.ShowExitConfirmation.Should().BeFalse();
    }

    [Fact]
    public void ProcessExitConfirmation_WithEscapeKey_ReturnsCancelAndClearsFlag()
    {
        var flow = new Flow { ShowExitConfirmation = true };
        var key = new ConsoleKeyInfo('\u001b', ConsoleKey.Escape, false, false, false);

        var result = KeyProcessor.ProcessExitConfirmation(flow, key);

        result.Should().Be(ExitConfirmationResult.Cancel);
        flow.ShowExitConfirmation.Should().BeFalse();
    }

    [Fact]
    public void ProcessExitConfirmation_WithOtherKey_ReturnsIgnored()
    {
        var flow = new Flow { ShowExitConfirmation = true };
        var key = new ConsoleKeyInfo('X', ConsoleKey.X, false, false, false);

        var result = KeyProcessor.ProcessExitConfirmation(flow, key);

        result.Should().Be(ExitConfirmationResult.Ignored);
        flow.ShowExitConfirmation.Should().BeTrue();
    }

    [Fact]
    public void ProcessExitConfirmation_WhenNotShowing_ReturnsNotApplicable()
    {
        var flow = new Flow { ShowExitConfirmation = false };
        var key = new ConsoleKeyInfo('Y', ConsoleKey.Y, false, false, false);

        var result = KeyProcessor.ProcessExitConfirmation(flow, key);

        result.Should().Be(ExitConfirmationResult.NotApplicable);
    }
}
