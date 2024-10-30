using System.Text.Json;
using System.Text.Json.Serialization;
using SixLabors.ImageSharp.PixelFormats;
using Spectre.Console;
using Color = SixLabors.ImageSharp.Color;

namespace Core;

public struct ColorScheme
{
  [JsonPropertyName("water")]
  public readonly Rgb24 Water { get; init;}
  [JsonPropertyName("land")]
  public readonly Rgb24 Land { get; init;}
  [JsonPropertyName("grass")]
  public readonly Rgb24 Grass { get; init;}
  [JsonPropertyName("tram")]
  public readonly Rgb24 Tram { get; init;}
  [JsonPropertyName("subway")]
  public readonly Rgb24 Subway { get; init;}
  [JsonPropertyName("rail")]
  public readonly Rgb24 Rail { get; init;}
  [JsonPropertyName("bus")]
  public readonly Rgb24 Bus { get; init;}
  [JsonPropertyName("ferry")]
  public readonly Rgb24 Ferry { get; init;}
  [JsonPropertyName("trolleybus")]
  public readonly Rgb24 Trolleybus { get; init;}
  public ColorScheme(Rgb24 Water, Rgb24 Land, Rgb24 Grass, Rgb24 Tram, Rgb24 Subway, Rgb24 Rail, Rgb24 Bus, Rgb24 Ferry, Rgb24 Trolleybus)
  {
    this.Water = Water;
    this.Land = Land;
    this.Grass = Grass;
    this.Tram = Tram;
    this.Subway = Subway;
    this.Rail = Rail;
    this.Bus = Bus;
    this.Ferry = Ferry;
    this.Trolleybus = Trolleybus;
  }
  public static ColorScheme Default = new ColorScheme
  (
    Color.DarkBlue,
    Color.Gray,
    Color.Green,
    new Rgb24(30, 30, 30),
    new Rgb24(30, 30, 30),
    new Rgb24(30, 30, 30),
    Color.Red,
    new Rgb24(30, 30, 30),
    new Rgb24(30, 30, 30)
  );
}
public struct Config
{
  [JsonPropertyName("lat")]
  public readonly double latitude { get; init; } = 50.0753684;
  [JsonPropertyName("lon")]
  public readonly double longitude { get; init; } = 14.4050773;
  public readonly short resolution = AnsiConsole.Profile.Height > 32  && AnsiConsole.Profile.Width > 48// No get; means it won't be serialized
    ? (short)(AnsiConsole.Profile.Height - 8)
    : throw new Exception("Terminal is too small(minimal height is 32  and width 48)");
  [JsonPropertyName("zoom")]
  public readonly Byte zoom { get; init; } = 14;
  [JsonPropertyName("colorScheme")]
  public readonly ColorScheme colorScheme { get; init; } = ColorScheme.Default;

  public Config(double latitude, double longitude, Int16 resolution, Byte zoom, ColorScheme colorScheme)
  {
    this.latitude = latitude;
    this.longitude = longitude;
    this.resolution = resolution;
    this.zoom = zoom;
    this.colorScheme = colorScheme;
  }
  public Config(double latitude, double longitude)
  {
    this.latitude = latitude;
    this.longitude = longitude;
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
  public override Rgb24 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)  {
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

    // Parse hex values for R, G, B
    byte r = Convert.ToByte(hex.Substring(1, 2), 16);
    byte g = Convert.ToByte(hex.Substring(3, 2), 16);
    byte b = Convert.ToByte(hex.Substring(5, 2), 16);

    return new Rgb24(r, g, b);
  }

  public override void Write(Utf8JsonWriter writer, Rgb24 value, JsonSerializerOptions options)
  {
    // Format as a hex string, e.g., "#FFEEDD"
    string hex = $"#{value.R:X2}{value.G:X2}{value.B:X2}";
    writer.WriteStringValue(hex);
  }
}
