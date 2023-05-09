namespace DolCon.Views;

using Spectre.Console;

public partial class GameService
{
    private void RenderNotReady()
    {
        _display.Update(
            new Panel(
                Align.Center(
                    new Rows(
                        new Markup($"[bold]{_screen.ToString()} screen is not ready!!![/]")
                    ),
                    VerticalAlignment.Middle)));
        _ctx.Refresh();
    }
}
