using Core.Api.Maps;
using Spectre.Console;
using Core.Rendering;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Color = SixLabors.ImageSharp.Color;
using Core.Debug;

public struct Config
{
  public double latitude;
  public double longitude;
  public Int16 resolution;
  public Byte zoom;
  public ColorScheme colorScheme;
}

public class Program
{
  static void Main(string[] args)
  {

    Config cfg = new Config
    {
      resolution = 48,
      zoom = 15,
      longitude = 14.4118794,
      latitude = 50.0732811,
      colorScheme = new ColorScheme
      {
        Water = Color.DarkBlue,
        Land = Color.LightGray,
        Grass = Color.Green,
        Buses = Color.Red,
        Trams = new Rgb24(30, 30, 30)
      }
    };
    Console.WriteLine($"Location: Prague, Czech Republic | Latitude = {cfg.latitude}, Longitude = {cfg.longitude}");
    Image<Rgb24> inputImage = GetOpenStreetMapTileUrl(cfg.latitude, cfg.longitude, cfg.zoom);
    AnsiConsole.Write(new CanvasImageWithText(inputImage).MaxWidth(48).AddText(new CanvasText(11, 12, "AAAAAAAAAA")));


    Image<Rgb24> thresholdImg = ImageProcessing.RunLayers(inputImage, cfg);
    CanvasImageWithText image = new CanvasImageWithText(thresholdImg).PixelWidth(1);
    image.AddText(new CanvasText(19 * 2, 44, "SSPŠ"));
    AnsiConsole.Write(image);
    Debug.PrintVectorTiles(cfg);
  }

  static Image<Rgb24> GetOpenStreetMapTileUrl(double latitude, double longitude, int zoom)
  {
    MapsApi mapsApi = new MapsApi(MapProviders.MapPropiversName.Thunderforest);
    Image<Rgb24>[] images = mapsApi.GetNeighbourTiles(latitude, longitude, zoom);
    return mapsApi.ConcatImages(images);
  }
}
