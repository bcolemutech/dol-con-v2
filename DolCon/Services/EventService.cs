using DolCon.Models;

namespace DolCon.Services;

public interface IEventService
{
    Scene ProcessEvent(Event thisEvent);
}

public class EventService : IEventService
{
    public Scene ProcessEvent(Event thisEvent)
    {
        throw new NotImplementedException();
    }
}