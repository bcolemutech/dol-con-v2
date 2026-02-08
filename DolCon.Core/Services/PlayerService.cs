namespace DolCon.Core.Services;

using Models;

public interface IPlayerService
{
    Player SetPlayer(string name, bool npc);
    Player SetPlayer(string name, bool npc, PlayerAbilities abilities);
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

    public Player SetPlayer(string name, bool npc, PlayerAbilities abilities)
    {
        var newPlayer = new Player
        {
            Name = name,
            Npc = npc,
            Abilities = abilities
        };
        SaveGameService.Party.Players.Add(newPlayer);
        return newPlayer;
    }
}
