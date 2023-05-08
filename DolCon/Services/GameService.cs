namespace DolCon.Services;

using Spectre.Console;

public interface IGameService
{
    Task Start();
}

public class GameService : IGameService
{
    public Task Start()
    { 
        return Task.CompletedTask;
    }
}
