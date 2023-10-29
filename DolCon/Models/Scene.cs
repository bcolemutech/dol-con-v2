namespace DolCon.Models;

using Enums;

public class Scene
{
    public MoveStatus MoveStatus { get; set; }
    public string Message { get; set; }
    public bool IsCompleted { get; set; } = true;
    public SceneType Type { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public Dictionary<int, ShopSelection> Selections { get; set; } = new();
    public ServiceType? SelectedService { get; set; }
    public int Selection { get; set; }
    public Location? Location { get; set; }

    public void Reset()
    {
        Title = null;
        Description = null;
        Selections = new Dictionary<int, ShopSelection>();
        SelectedService = null;
        Selection = 0;
        Location = null;
    }
}