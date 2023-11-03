using Spectre.Console;

namespace DolCon.Views;

public partial class GameService
{
    private void RenderInventory()
    {
        _display.Update(
            new Columns(
                new Panel(
                    new Markup("[bold]Inventory[/]")
                ),
                new Panel(
                    new Markup("[bold]Selected Details[/]")
                )
            )
        );
        _ctx.Refresh();
        _controls.Update(
            new Panel(
                    Align.Center(
                        new Rows(
                            new Markup(
                                "[bold]Standard controls[/]: ([green bold]H[/])ome | ([green bold]N[/])avigation | [Red bold]Esc[/] to exit")
                        ),
                        VerticalAlignment.Middle))
                .Expand());
        _ctx.Refresh();
    }
}