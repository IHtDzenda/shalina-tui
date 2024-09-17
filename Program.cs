using Core.Api.Maps;
using Spectre.Console;
using Core;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

public static class Config
{
  public static Int16 resolution = 50;
  public static Byte zoom = 16;
}

public class Program
{
  static void Main(string[] args)
  {
    double latitude = 50.0755;
    double longitude = 14.4378;
    Console.WriteLine($"Location: Prague, Czech Republic | Latitude = {latitude}, Longitude = {longitude}");
    Image<Rgba32> inputImage = GetOpenStreetMapTileUrl(latitude, longitude, Config.zoom);
    AnsiConsole.Write(ImageProcessing.ImageSharpToCanvasImage(inputImage).MaxWidth(768));

    var thresholdImg = ImageProcessing.Threshold(inputImage, Config.resolution);
    CanvasImage image = ImageProcessing.ImageSharpToCanvasImage(thresholdImg);
    image.MaxWidth(Config.resolution);
    AnsiConsole.Write(image);
  }

  static Image<Rgba32> GetOpenStreetMapTileUrl(double latitude, double longitude, int zoom)
  {
    MapsApi mapsApi = new MapsApi(MapProviders.MapPropiversName.Thunderforest);
    Tile[] tiles = mapsApi.GetNeighbourTiles(latitude, longitude, zoom);
    return mapsApi.ConcatImages(tiles);
  }
}
