namespace DolCon.WorldForge;

using DolCon.Core.Services;

/// <summary>Streams bake progress to the console: transient status lines and persistent events.</summary>
public sealed class ConsoleProvisioningCallback : IMapProvisioningCallback
{
    public void OnStatus(string message) => Console.WriteLine($"  … {message}");

    public void OnEvent(string message) => Console.WriteLine($"  ✓ {message}");
}
