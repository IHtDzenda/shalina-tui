using System.Text.Json;
using System.Text.Json.Serialization;
using Core.Api.Maps;
using Core.Api.Maps.Prague;
using Core.Geometry;
using Core.Rendering.Search;
using Mapbox.VectorTile.Geometry;
using SixLabors.ImageSharp.PixelFormats;
using Spectre.Console;
using Color = SixLabors.ImageSharp.Color;

namespace Core;

public struct Resolution
{
  public short width { get; set; }
  public short height { get; set; }

  public Resolution(short width, short height)
  {
    this.width = width;
    this.height = height;
  }

  public double Ratio()
  {
    return (double)width / height;
  }

  public static implicit operator Resolution((short, short) v)
  {
    return new Resolution(v.Item1, v.Item2);
  }
  public static implicit operator (short, short)(Resolution v)
  {
    return (v.width, v.height);
  }
}
public struct Config
{
  [JsonPropertyName("view")]
  public BoundingBox boundingBox = new BoundingBox(new LatLng { Lat = 50.0753684, Lng = 14.4050773 }, 14, (1, 1));
  [JsonPropertyName("zoom")]
  public Byte zoom { get; set; } = 14;
  [JsonPropertyName("resolution")]
  [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
  public Resolution resolution { get; set; } = ((short)(AnsiConsole.Profile.Width / 3), (short)AnsiConsole.Profile.Height); // Resolution in pixels
  [JsonPropertyName("colorScheme")]
  public Dictionary<string, Rgb24> colorScheme { get; init; } = new Dictionary<string, Rgb24>
  {
    { "water", Color.DarkBlue },
    { "land", Color.Gray },
    { "grass", Color.Green },
    { "tram", Util.ParseHexColor("#7A0603") },
    { "subway", Color.Purple },
    { "train", Util.ParseHexColor("#251E62") },
    { "bus", Util.ParseHexColor("#007DA8") },
    { "trolleybus", Util.ParseHexColor("#80166F") },
    { "ferry", Util.ParseHexColor("#00B3CB") },
    { "other", Color.Lime }
  };
  public enum Layout
  {
    Map,
    Search,
    Config
  }
  [JsonPropertyName("hideRegional")]
  public bool hideRegional { get; set; } = false;
  [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
  public string userQuery { get; set; } = "";
  [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
  public UserQuery query { get; set; } = new UserQuery();
  [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
  public int cursorConfigIndex { get; set; } = 0;
  [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
  public bool sidebarSelected { get; set; } = false;
  [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
  public string newConfigValue { get; set; } = "";
  [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
  public Layout layout { get; set; } = Layout.Search;


  // Only runtime stuff
  [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
  public List<GeoDataProvider> cityGeoData = new List<GeoDataProvider> { new PidGeoData() };
  [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
  public List<StopsDataProvider> cityStopsData = new List<StopsDataProvider> { new PidStopData() };
  [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
  public List<TransportProvider> cityLiveData = new List<TransportProvider> { new PidLiveData() };


  public Config(LatLng center, (short width, short height) resolution, Byte zoom, Dictionary<string, Rgb24> colorScheme)
  {
    this.boundingBox = new BoundingBox(center, zoom, resolution);
    this.resolution = resolution;
    this.colorScheme = colorScheme;
  }

  public Config() { }

  static JsonSerializerOptions jsonOptions = new JsonSerializerOptions
  {
    PropertyNameCaseInsensitive = true,
    WriteIndented = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.Never,
    IgnoreReadOnlyFields = false,
    IgnoreReadOnlyProperties = false,
    Converters = { new Rgb24JsonSerializerExtension() },

  };
  // Reads a config json file and returns a Config object
  public static Config Load()
  {
    if (!System.IO.File.Exists(Core.Util.GetConfigPath()))
    {
      Config config = new Config();
      config.Save();
      return config;
    }
    return LoadFromFile(Core.Util.GetConfigPath());
  }
  public static Config LoadFromFile(string path)
  {
    return JsonSerializer.Deserialize<Config>(System.IO.File.ReadAllText(path), jsonOptions);
  }
  public void Save()
  {
    SaveToFile(Core.Util.GetConfigPath());
  }
  public void SaveToFile(string path)
  {
    System.IO.File.WriteAllText(path, JsonSerializer.Serialize(this, jsonOptions));
  }
}

public class Rgb24JsonSerializerExtension : JsonConverter<Rgb24>
{
  public override Rgb24 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
  {
    // Expecting a JSON string like "#FFEEDD"
    if (reader.TokenType != JsonTokenType.String)
    {
      throw new JsonException("Expected a string.");
    }

    string hex = reader.GetString();

    if (hex == null || hex.Length != 7 || hex[0] != '#')
    {
      throw new JsonException("Invalid hex color format.");
    }
    return Util.ParseHexColor(hex.Substring(1));
  }

  public override void Write(Utf8JsonWriter writer, Rgb24 value, JsonSerializerOptions options)
  {
    // Format as a hex string, e.g., "#FFEEDD"
    string hex = $"#{value.R:X2}{value.G:X2}{value.B:X2}";
    writer.WriteStringValue(hex);
  }
}
