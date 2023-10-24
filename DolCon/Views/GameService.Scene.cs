using DolCon.Services;
using Spectre.Console;

namespace DolCon.Views;

using Enums;

public partial class GameService
{
    private void RenderScene(ConsoleKeyInfo value)
    {
        ProcessScene(value);
        switch (_scene.Type)
        {
            case SceneType.Dialogue:
                RenderDialog(value);
                break;
            case SceneType.Battle:
                RenderBattle(value);
                break;
            case SceneType.Shop:
                RenderShop(value);
                break;
            case SceneType.None:
            default:
                _scene.IsCompleted = true;
                RenderNotReady();
                break;
        }

        _controls.Update(
            new Panel(
                    Align.Center(
                        new Markup("Select an option above."),
                        VerticalAlignment.Middle))
                .Expand());
        _ctx.Refresh();
    }

    private void RenderShop(ConsoleKeyInfo value)
    {
        var thisChar = value.KeyChar.ToString();
        var cleanChar = thisChar.First().ToString();
        var tryParse = int.TryParse(cleanChar, out var selection);

        if (tryParse)
        {
            _scene.SelectedService = (Service)selection;
        }
        
        if (_scene.SelectedService == null)
        {
            var location = SaveGameService.CurrentLocation;
            var serviceTable = new Table();
            _display.Update(
                new Panel(
                    new Rows(
                        Align.Center(
                            new Markup($"[bold]Select a service to continue.[/]")
                        ),
                        Align.Center(
                            serviceTable
                        )
                    )));
            _ctx.Refresh();
            
            serviceTable.AddColumn("Key");
            serviceTable.AddColumn("Service");

            var services = location.Type.Services;
            foreach (var service in services)
            {
                serviceTable.AddRow(((int)service).ToString(), Enum.GetName(service));
            }
        }
        else
        {
            //TODO: Implement shop
        }
    }

    private void RenderBattle(ConsoleKeyInfo value)
    {
        throw new NotImplementedException();
    }

    private void RenderDialog(ConsoleKeyInfo value)
    {
        throw new NotImplementedException();
    }

    private void ProcessScene(ConsoleKeyInfo value)
    {
        var thisChar = value.KeyChar.ToString();
        var cleanChar = thisChar.First().ToString();
        var tryParse = int.TryParse(cleanChar, out var selection);
    }
}