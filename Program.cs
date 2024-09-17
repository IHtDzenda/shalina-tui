using Core.Api.Maps;
using Spectre.Console;
using Core;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

public class Program
{
  static void Main(string[] args)
  {
    const byte zoom = 16;
    const Int16 resolution = 64;
    double latitude = 50.0755;
    double longitude = 14.4378;
    Console.WriteLine($"Location: Prague, Czech Republic | Latitude = {latitude}, Longitude = {longitude}");
    string path = GetOpenStreetMapTileUrl(latitude, longitude, zoom); // Zoom level 10

    CanvasImage image;
    using (Image<Rgba32> inputImage = Image.Load<Rgba32>(path))
    {
      var thresholdImg = ImageProcessing.Threshold(inputImage, resolution);
      // var thresholdImg = inputImage;
      using (var stream = new MemoryStream())
      {
        thresholdImg.Save(stream, new SixLabors.ImageSharp.Formats.Png.PngEncoder());
        stream.Position = 0;

        image = new CanvasImage(stream);
      }
    }
    image.MaxWidth(resolution);
    AnsiConsole.Write(image);
    AnsiConsole.Write(new CanvasImage(path).MaxWidth(resolution));
  }

  static string GetOpenStreetMapTileUrl(double latitude, double longitude, int zoom)
  {
    MapsApi mapsApi = new MapsApi(MapProviders.MapPropiversName.Thunderforest);
    return mapsApi.GetTiles(latitude, longitude, zoom);
  }

}
