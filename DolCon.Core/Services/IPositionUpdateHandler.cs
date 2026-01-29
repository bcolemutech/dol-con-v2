namespace DolCon.Core.Services;

/// <summary>
/// Interface for handling position updates when the player moves.
/// This replaces the IImageService.ProcessSvg() dependency from the console app.
/// </summary>
public interface IPositionUpdateHandler
{
    /// <summary>
    /// Called when the player's position has been updated.
    /// </summary>
    void OnPositionUpdated();
}

/// <summary>
/// Default no-op implementation of IPositionUpdateHandler.
/// </summary>
public class NoOpPositionUpdateHandler : IPositionUpdateHandler
{
    public void OnPositionUpdated() { }
}
