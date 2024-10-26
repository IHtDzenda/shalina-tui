using System.Text.Json;
using System.Text.Json.Serialization;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Spectre.Console;
using Color = SixLabors.ImageSharp.Color;

namespace Core;

public struct ColorScheme
{
  public Color Water;
  public Color Land;
  public Color Grass;
  public Color Tram;
  public Color Subway;
  public Color Rail;
  public Color Bus;
  public Color Ferry;
  public Color Trolleybus;
}
public struct Config
{
  [JsonPropertyName("lat")]
  public double latitude;
  [JsonPropertyName("lon")]
  public double longitude;
  [JsonPropertyName("res")]
  public Int16 resolution;
  [JsonPropertyName("zoom")]
  public Byte zoom;
  [JsonPropertyName("colorScheme")]
  public ColorScheme colorScheme;

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
    this.resolution = (short)AnsiConsole.Profile.Width;
    this.zoom = 14;
    this.colorScheme = new ColorScheme
    {
      Water = Color.DarkBlue,
      Land = Color.LightGray,
      Grass = Color.Green,
      Bus = Color.Red,
      Tram = new Rgb24(30, 30, 30)
    };
  }

  static JsonSerializerOptions jsonOptions = new JsonSerializerOptions
  {
    PropertyNameCaseInsensitive = true,
    WriteIndented = true,
  };
  // Reads a config json file and returns a Config object
  public Config Load(){
    return LoadFromFile("/home/jare/.config/shalina.json");
  }
  public Config LoadFromFile(string path)
  {
    string json = System.IO.File.ReadAllText(path);
    return JsonSerializer.Deserialize<Config>(json, jsonOptions);
  }
  public void SaveToFile(string path)
  {
    string json = JsonSerializer.Serialize(this);
    System.IO.File.WriteAllText(path, json);
  }
}
