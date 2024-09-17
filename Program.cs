using Core.Api.Maps;
using Core.Debug;
public class Program
{
  static void Main(string[] args)
  {

    double latitude = 50.0715200;
    double longitude = 14.403497;
    Console.WriteLine($"Location: Prague, Czech Republic | Latitude = {latitude}, Longitude = {longitude}");
    Tile[] tiles = GetOpenStreetMapTileUrl(latitude, longitude, 16); // Zoom level 10


  }

  static Tile[] GetOpenStreetMapTileUrl(double latitude, double longitude, int zoom)
  {
    MapsApi mapsApi = new MapsApi(MapProviders.MapPropiversName.Thunderforest);
    Tile[] tiles = mapsApi.GetNeighbourTiles(latitude, longitude, zoom);
    mapsApi.ConcatImages(tiles);
    return tiles;
  }
}
