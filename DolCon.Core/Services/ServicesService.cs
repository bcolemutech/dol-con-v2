using System.Collections;
using System.Reflection;
using System.Text.Json;
using DolCon.Core.Enums;
using DolCon.Core.Models;

namespace DolCon.Core.Services;

public interface IServicesService
{
    IEnumerable<Service> GetServices(ServiceType serviceType, Rarity locationRarity);
}

public class ServicesService : IServicesService
{
    private readonly List<Service> _services;

    public ServicesService()
    {
        const string servicesResourceName = "DolCon.Core.Resources.Services.json";
        var executingAssembly = Assembly.GetExecutingAssembly();
        var jsonStream = executingAssembly.GetManifestResourceStream(servicesResourceName);
        using var reader = new StreamReader(jsonStream ?? throw new InvalidOperationException());
        var json = reader.ReadToEnd();
        _services = JsonSerializer.Deserialize<List<Service>>(json) ?? new List<Service>();
    }

    public IEnumerable<Service> GetServices(ServiceType serviceType, Rarity locationRarity)
    {
        return _services.Where(s => s.Type == serviceType && s.Rarity <= locationRarity);
    }
}