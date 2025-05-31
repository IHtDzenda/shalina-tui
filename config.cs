using System.Text.Json;
using System.Text.Json.Serialization;
using Core.Rendering.Search;
using SixLabors.ImageSharp.PixelFormats;
using Spectre.Console;
using Color = SixLabors.ImageSharp.Color;

namespace Core;

public struct Config
{
  public void UpdateAlignment()
  {
    var width = AnsiConsole.Console.Profile.Width / 2;
    var height = AnsiConsole.Console.Profile.Height;

    if (height > width)
      this.vertical = true;
    else
      this.vertical = false;
  }
  public void UpdateResolution()
  {
    if (this.vertical)
    {
      this.resolution.width = (short)((AnsiConsole.Console.Profile.Width / 2) - 1);
      this.resolution.height = (short)(((AnsiConsole.Console.Profile.Height * (this.layout == Layout.Map ? 4 : 3)) / 4));
    }
    else
    {
      this.resolution.width = (short)(((AnsiConsole.Console.Profile.Width * (this.layout == Layout.Map ? 4 : 3)) / 8) - 2);
      this.resolution.height = (short)AnsiConsole.Console.Profile.Height;
    }
  }
  [JsonPropertyName("lat")]
  public double latitude { get; set; } = 50.0753684;
  [JsonPropertyName("lon")]
  public double longitude { get; set; } = 14.4050773;
  [JsonPropertyName("zoom")]
  public Byte zoom { get; set; } = 14;
  [JsonPropertyName("resolution")]
  [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
  public (short width, short height) resolution; // Resolution in pixels
  public bool vertical = false;
  [JsonPropertyName("colorScheme")]
  public Dictionary<string, Rgb24> colorScheme { get; init; } = new Dictionary<string, Rgb24>
  {
    { "water", Color.DarkBlue },
    { "land", Color.Gray },
    { "grass", Color.Green },
    { "tram", Util.ParseHexColor("#7A0603") },
    { "subway", Color.Purple },
    { "rail", Util.ParseHexColor("#251E62") },
    { "bus", Util.ParseHexColor("#007DA8") },
    { "trolleybus", Util.ParseHexColor("80166F") },
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
  public bool hideRegional { get; set; } = true;
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


  public Config(double latitude, double longitude, (short width, short height) resolution, Byte zoom, Dictionary<string, Rgb24> colorScheme)
  {
    this.latitude = latitude;
    this.longitude = longitude;
    this.zoom = zoom;
    this.resolution = resolution;
    this.colorScheme = colorScheme;
    this.UpdateResolution();
  }
  public Config(double latitude, double longitude)
  {
    this.latitude = latitude;
    this.longitude = longitude;
    this.UpdateResolution();
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
      Config defaultConfig = new Config();
      defaultConfig.Save();
      return defaultConfig;
    }
    var config = LoadFromFile(Core.Util.GetConfigPath());
    config.UpdateResolution();
    return config;
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
