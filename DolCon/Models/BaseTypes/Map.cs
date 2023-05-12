namespace DolCon.Models.BaseTypes;

using Newtonsoft.Json;

public class Map
{
    public Info info { get; set; }
    public Settings settings { get; set; }
    public Coords coords { get; set; }
    
    [Newtonsoft.Json.JsonProperty("cells")]
    public MapCollections Collections { get; set; }
    public Biomes biomes { get; set; }
    public List<NameBasis> nameBases { get; set; }
    public Party Party { get; set; }
    public Guid CurrentPlayerId { get; set; }
}

public class MapCollections
{
    public List<Cell> cells { get; set; }
    public List<object> features { get; set; }
    public List<Culture> cultures { get; set; }
    public List<Burg> burgs { get; set; }
    public List<State> states { get; set; }
    [Newtonsoft.Json.JsonConverter(typeof(ProvincesConverter))]
    public List<Province> provinces { get; set; }
    public List<Religion> religions { get; set; }
    public List<River> rivers { get; set; }
    public List<Marker> markers { get; set; }
}

public class ProvincesConverter : Newtonsoft.Json.JsonConverter<List<Province>>
{
    public override void WriteJson(JsonWriter writer, List<Province>? value, JsonSerializer serializer)
    {
        serializer.Serialize(writer, value);
    }

    public override List<Province> ReadJson(JsonReader reader, Type objectType, List<Province>? existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        var provinces = new List<Province>{new()};
        while (reader.Read())
        {
            if (reader.TokenType == JsonToken.EndArray) break;
            var province = new Province();
            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.EndObject) break;
                if (reader.TokenType != JsonToken.PropertyName) continue;
                var propertyName = reader.Value.ToString();
                reader.Read();
                switch (propertyName)
                {
                    case "i":
                        province.i = (int)(long)reader.Value;
                        break;
                    case "state":
                        province.state = (int)(long)reader.Value;
                        break;
                    case "center":
                        province.center = (int)(long)reader.Value;
                        break;
                    case "burg":
                        province.burg = (int)(long)reader.Value;
                        break;
                    case "name":
                        province.name = reader.Value.ToString() ?? "";
                        break;
                    case "formName":
                        province.formName = reader.Value.ToString() ?? "";
                        break;
                    case "fullName":
                        province.fullName = reader.Value.ToString() ?? "";
                        break;
                    case "color":
                        province.color = reader.Value.ToString() ?? "";
                        break;
                    case "coa":
                        province.coa = serializer.Deserialize<Coa>(reader) ?? new Coa();
                        break;
                }
            }
            provinces.Add(province);
        }
        return provinces;
    }
}
