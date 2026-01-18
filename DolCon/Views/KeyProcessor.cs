namespace DolCon.Views;

using Enums;
using Models;

public static class KeyProcessor
{
    public static ExitConfirmationResult ProcessExitConfirmation(Flow flow, ConsoleKeyInfo key)
    {
        if (!flow.ShowExitConfirmation)
        {
            return ExitConfirmationResult.NotApplicable;
        }

        if (key.Key == ConsoleKey.Y)
        {
            return ExitConfirmationResult.Exit;
        }

        if (key.Key == ConsoleKey.N || key.Key == ConsoleKey.Escape)
        {
            flow.ShowExitConfirmation = false;
            return ExitConfirmationResult.Cancel;
        }

        return ExitConfirmationResult.Ignored;
    }
}
