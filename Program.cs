using Core.Api.Maps;
using Spectre.Console;
using Core.Rendering;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Core.Debug;


public class Program
{
  static void Main(string[] args)
  {
    Debug.LoadAndPrintConfig();
  }

  static Image<Rgb24> GetOpenStreetMapTileUrl(double latitude, double longitude, int zoom)
  {
    MapsApi mapsApi = new MapsApi(MapProviders.MapPropiversName.Thunderforest);
    Image<Rgb24>[] images = mapsApi.GetNeighbourTiles(latitude, longitude, zoom);
    return mapsApi.ConcatImages(images);
  }
}
