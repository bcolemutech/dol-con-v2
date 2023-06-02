namespace DolCon.Models;

public class Party
{
    public List<Player> Players { get; set; } = new();
    public int Cell { get; set; }
    public int? Burg { get; set; }
    public Guid? Location { get; set; }
    public double Stamina { get; set; }
    
    public bool TryMove(double cost)
    {
        if (Stamina < cost)
        {
            return false;
        }

        Stamina -= cost;
        return true;
    }
}
