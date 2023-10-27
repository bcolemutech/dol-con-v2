namespace DolCon.Models;

using Enums;

public class Scene
{
    public MoveStatus MoveStatus { get; set; }
    public string Message { get; set; }
    public bool IsCompleted { get; set; }
    public SceneType Type { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public Dictionary<int, string> Selections { get; set; }
    public Service? SelectedService { get; set; }
    public int Selection { get; set; }
    public Location? Location { get; set; }
}