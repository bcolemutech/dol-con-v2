namespace DolCon.Services;

using Models;

public interface IPlayerService
{
    Player SetPlayer(string name, bool npc);
}

public class PlayerService : IPlayerService
{
    public Player SetPlayer(string name, bool npc)
    {
        var newPlayer = new Player
        {
            Name = name,
            Npc = npc
        };
        SaveGameService.Party.Players.Add(newPlayer);
        return newPlayer;
    }
}
