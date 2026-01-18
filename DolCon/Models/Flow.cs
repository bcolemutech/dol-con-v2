using DolCon.Enums;

namespace DolCon.Models;

public class Flow
{
    public ConsoleKeyInfo? Key { get; set; }
    public Screen Screen { get; set; } = Screen.Home;
    public bool Redirect { get; set; }
    public bool ShowExitConfirmation { get; set; }
}