using Core.Api.Maps;
using Spectre.Console;
using Core.Rendering;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

public static class Config
{
  public static Int16 resolution = 50;
  public static Byte zoom = 14;
}

public class Program
{
  static void Main(string[] args)
  {
    double latitude = 50.0793075;
    double longitude = 14.4054342;
    Console.WriteLine($"Location: Prague, Czech Republic | Latitude = {latitude}, Longitude = {longitude}");
    Image<Rgb24> inputImage = GetOpenStreetMapTileUrl(latitude, longitude, Config.zoom);
    //AnsiConsole.Write(new CanvasImageWithText(inputImage));
    

    Image<Rgb24> thresholdImg = ImageProcessing.Threshold(inputImage, Config.resolution);
    CanvasImageWithText image = new CanvasImageWithText(thresholdImg);
    image.MaxWidth(Config.resolution);
    image.AddText(new CanvasText(0, 0, ""));
    AnsiConsole.Write(image);
  }

  static Image<Rgb24> GetOpenStreetMapTileUrl(double latitude, double longitude, int zoom)
  {
    MapsApi mapsApi = new MapsApi(MapProviders.MapPropiversName.Thunderforest);
    Tile[] tiles = mapsApi.GetNeighbourTiles(latitude, longitude, zoom);
    return mapsApi.ConcatImages(tiles);
  }
}
