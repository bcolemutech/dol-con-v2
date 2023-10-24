namespace DolCon.Models;

using Enums;

public class Scene
{
    public MoveStatus MoveStatus { get; set; }
    public string Message { get; set; }
    public bool IsCompleted { get; set; }
    public SceneType Type { get; set; }
    public Service? SelectedService { get; set; }
}