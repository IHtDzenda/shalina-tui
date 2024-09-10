using Core.Api.Maps;
using Spectre.Console;
public class Program
{
  static void Main(string[] args)
  {
    double latitude = 50.0755;
    double longitude = 14.4378;
    Console.WriteLine($"Location: Prague, Czech Republic | Latitude = {latitude}, Longitude = {longitude}");
    string path = GetOpenStreetMapTileUrl(latitude, longitude, 16); // Zoom level 10
    var image = new CanvasImage(path);
    image.MaxWidth(20);
    AnsiConsole.Write(image);
  }

  static string GetOpenStreetMapTileUrl(double latitude, double longitude, int zoom)
  {
    int tileX = (int)((longitude + 180.0) / 360.0 * (1 << zoom));
    int tileY = (int)((1.0 - Math.Log(Math.Tan(latitude * Math.PI / 180.0) + 1.0 / Math.Cos(latitude * Math.PI / 180.0)) / Math.PI) / 2.0 * (1 << zoom));
    MapsApi mapsApi = new MapsApi(MapProviders.MapPropiversName.Thunderforest);
    return mapsApi.GetTiles(latitude, longitude, zoom);
  }

}
