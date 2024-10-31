using Core.Api.Maps;
using Core.Api.Maps.Prague;
using Core.Api.VectorTiles;
using Mapbox.VectorTile;
using Mapbox.VectorTile.Geometry;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Core.Rendering;

public static class Renderer
{
  public delegate void LayerFunction(Config config, VectorTileLayer layer, (int x, int y) tile, byte zoom, (LatLng min, LatLng max) boundingBox);

  static Dictionary<string, LayerFunction> vectorTileLayers = new Dictionary<string, LayerFunction> { };
  static List<GeoDataInterface> cityGeoData = new List<GeoDataInterface> { new PragueGeoData() };
  static List<TransportInterface> cityLiveData = new List<TransportInterface> { new PidData() };


  static DrawingOptions drawingOptions = new DrawingOptions
  {
    GraphicsOptions = new GraphicsOptions
    {
      Antialias = false // Disable anti-aliasing
    }
  };

  static Image<Rgb24> image;
  static List<CanvasText> texts = new List<CanvasText>();
  private static void RenderVectorTiles(Config config, LatLng coordinate, byte zoom, (LatLng min, LatLng max) boundingBox)
  {
    (int x, int y)[] tiles = Conversion.GetTiles(coordinate, zoom);
    for (int i = 0; i < tiles.Length; i++)
    {
      VectorTile tile = VectorTiles.GetTile(Conversion.ConvertTileToGPS(tiles[i].x, tiles[i].y, zoom), zoom);
      foreach (var layer in tile.LayerNames())
      {
        if (vectorTileLayers.ContainsKey(layer))
        {
          vectorTileLayers[layer](config, tile.GetLayer(layer), tiles[i], zoom, boundingBox);
        }
      }
    }
  }
  private static void RenderGeoData(Config config, LatLng coordinate, byte zoom, (LatLng min, LatLng max) boundingBox)
  {
    foreach (var city in cityGeoData)
    {
      GeoData[] data = city.getData(boundingBox).Result;
      foreach (var geoData in data)
      {
        if (geoData.geometry == null)
        {
          continue;
        }
        foreach(var line in geoData.geometry)
        {
          Console.WriteLine(geoData.routeColor);
          image.Mutate(ctx =>
          {
            ctx.DrawLine(drawingOptions, Brushes.Solid(config.colorScheme.Subway), 1, line.Select(x => Conversion.ConvertGPSToPixel(x, boundingBox, (image.Width, image.Height))).ToArray());
          });
        }
      }
    }
  }
  public static CanvasImageWithText RenderMap(Config config)
  {
    if (image == null)
    {
      image = new Image<Rgb24>(config.resolution, config.resolution, config.colorScheme.Land);
    }
    LatLng coord = new LatLng { Lat = config.latitude, Lng = config.longitude };
    (LatLng min, LatLng max) boundingBox = Conversion.GetBoundingBox(coord, config.zoom);
    RenderVectorTiles(config, coord, config.zoom > 14 ? (byte)14 : config.zoom, boundingBox);
    RenderGeoData(config, coord, config.zoom, boundingBox);
    //RenderLiveData(config, coord, config.zoom, boundingBox);

    return new CanvasImageWithText(image, texts.ToArray());
  }
}

