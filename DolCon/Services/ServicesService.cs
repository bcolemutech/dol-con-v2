using System.Text.Json;
using DolCon.Enums;
using DolCon.Models;

namespace DolCon.Services;

public interface IServicesService
{
    IEnumerable<Service> GetServices(ServiceType serviceType, Rarity locationRarity);
}

public class ServicesService : IServicesService
{
    private readonly List<Service> _services;

    public ServicesService()
    {
        var servicesPath = Path.Combine(AppContext.BaseDirectory, "Resources", "Services.json");
        var json = File.ReadAllText(servicesPath);
        _services = JsonSerializer.Deserialize<List<Service>>(json) ?? new List<Service>();
    }

    public IEnumerable<Service> GetServices(ServiceType serviceType, Rarity locationRarity)
    {
        return _services.Where(s => s.Type == serviceType && s.Rarity <= locationRarity);
    }
}