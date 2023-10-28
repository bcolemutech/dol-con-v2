using DolCon.Enums;

namespace DolCon.Models;

public class Flow
{
    public ConsoleKeyInfo Key { get; set; }
    public Screen? Redirect { get; set; }
}