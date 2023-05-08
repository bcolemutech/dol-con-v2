namespace DolCon.Services;

public class DolSaveGameException : Exception
{
    public DolSaveGameException(string message) : base(message)
    {
    }
}