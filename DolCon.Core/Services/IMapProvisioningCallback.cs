namespace DolCon.Core.Services;

/// <summary>
/// Interface for receiving status updates during map loading and provisioning.
/// </summary>
public interface IMapProvisioningCallback
{
    /// <summary>
    /// Called to update the current status message.
    /// </summary>
    void OnStatus(string message);

    /// <summary>
    /// Called when a notable event occurs during provisioning.
    /// </summary>
    void OnEvent(string message);
}

/// <summary>
/// Default no-op implementation of IMapProvisioningCallback.
/// </summary>
public class NoOpMapProvisioningCallback : IMapProvisioningCallback
{
    public void OnStatus(string message) { }
    public void OnEvent(string message) { }
}
