using Core.Api.Maps;
using Core.Rendering;
using Spectre.Console;
using Core.Api.VectorTiles;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;
using Mapbox.VectorTile;
using Mapbox.VectorTile.Geometry;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
namespace Core.Debug
{
  public class Debug
  {
    public static void PrintTransports()
    {

      PidData pidData = new PidData();
      Transport[] transports = pidData.getData().Result;
      AnsiConsole.WriteLine("Transports:");
      for (int i = 0; i < transports.Length; i++)
      {
        AnsiConsole.Markup($"[green]TripId: {transports[i].tripId} [/]");
        AnsiConsole.Markup($"[red]Line: {transports[i].lineName} [/]");
        AnsiConsole.Markup($"[blue]Delay: {transports[i].delay} [/]");
        AnsiConsole.Markup($"[white]Latitude: {transports[i].lat} [/]");
        AnsiConsole.Markup($"[yellow]Longitude: {transports[i].lon} [/]");
        AnsiConsole.WriteLine();
      }
    }
    public static void LoadAndPrintConfig()
    {
      Config config = new Config();
      AnsiConsole.WriteLine("Config:");
      AnsiConsole.Markup($"[green]Latitude: {config.latitude} [/]");
      AnsiConsole.Markup($"[red]Longitude: {config.longitude} [/]");
      AnsiConsole.Markup($"[blue]Resolution: {config.resolution} [/]");
      AnsiConsole.Markup($"[white]Zoom: {config.zoom} [/]");
      AnsiConsole.Markup($"[yellow]ColorScheme: [/]");
      AnsiConsole.Markup($"[rgb({config.colorScheme.Water.R},{config.colorScheme.Water.G},{config.colorScheme.Water.B})]Water: {config.colorScheme.Water} [/]");
      AnsiConsole.Markup($"[rgb({config.colorScheme.Land.R},{config.colorScheme.Land.G},{config.colorScheme.Land.B})]Land: {config.colorScheme.Land} [/]");
      AnsiConsole.Markup($"[rgb({config.colorScheme.Grass.R},{config.colorScheme.Grass.G},{config.colorScheme.Grass.B})]Grass: {config.colorScheme.Grass} [/]");
      AnsiConsole.Markup($"[rgb({config.colorScheme.Tram.R},{config.colorScheme.Tram.G},{config.colorScheme.Tram.B})]Tram: {config.colorScheme.Tram} [/]");
      AnsiConsole.WriteLine();
      //config.Save();
      config = Config.Load();
      AnsiConsole.Markup($"[green]Latitude: {config.latitude} [/]");
      AnsiConsole.Markup($"[red]Longitude: {config.longitude} [/]");
      AnsiConsole.Markup($"[blue]Resolution: {config.resolution} [/]");
      AnsiConsole.Markup($"[white]Zoom: {config.zoom} [/]");
      AnsiConsole.Markup($"[yellow]ColorScheme: [/]");
      AnsiConsole.Markup($"[rgb({config.colorScheme.Water.R},{config.colorScheme.Water.G},{config.colorScheme.Water.B})]Water: {config.colorScheme.Water} [/]");
      AnsiConsole.Markup($"[rgb({config.colorScheme.Land.R},{config.colorScheme.Land.G},{config.colorScheme.Land.B})]Land: {config.colorScheme.Land} [/]");
      AnsiConsole.Markup($"[rgb({config.colorScheme.Grass.R},{config.colorScheme.Grass.G},{config.colorScheme.Grass.B})]Grass: {config.colorScheme.Grass} [/]");
      AnsiConsole.Markup($"[rgb({config.colorScheme.Tram.R},{config.colorScheme.Tram.G},{config.colorScheme.Tram.B})]Tram: {config.colorScheme.Tram} [/]");
    }
    public static void PrintBoundingBox()
    {
      Config config = Config.Load();
      LatLng coord = new LatLng { Lat = config.latitude, Lng = config.longitude };
      (LatLng min, LatLng max) boundingBox = Conversion.GetBoundingBox(coord, config.zoom);
      AnsiConsole.MarkupLine($"[green]Min: {boundingBox.min.Lat}, {boundingBox.min.Lng} [/]");
      AnsiConsole.MarkupLine($"[red]Max: {boundingBox.max.Lat}, {boundingBox.max.Lng} [/]");
    }
    public static void SimpleRender()
    {
      var drawingOptions = new DrawingOptions
      {
        GraphicsOptions = new GraphicsOptions
        {
          Antialias = false // Disable anti-aliasing
        }
      };
      Config config = Config.Load();
      LatLng coord = new LatLng { Lat = config.latitude, Lng = config.longitude };
      byte zoom = config.zoom > 14 ? (byte)14 : config.zoom;
      (int x, int y)[] tiles = Conversion.GetTiles(coord, zoom);
      Image<Rgb24> image = new Image<Rgb24>(config.resolution, config.resolution, config.colorScheme.Land);
      for (int i = 0; i < tiles.Length; i++)
      {
        VectorTile tile = VectorTiles.GetTile(Conversion.ConvertTileToGPS(tiles[i].x, tiles[i].y, zoom), zoom);
        const string layerName = "water";
        if (!tile.LayerNames().Contains(layerName))
        {
          continue;
        }
        VectorTileLayer layer = tile.GetLayer(layerName);
        for (int featureIdx = 0; featureIdx < layer.FeatureCount(); featureIdx++)
        {
          VectorTileFeature feature = layer.GetFeature(featureIdx);
          foreach (var part in feature.Geometry<int>())
          {
            List<PointF> points = new List<PointF>();
            foreach (var geom in part)
            {
              LatLng latLng = geom.ToLngLat(zoom, (ulong)tiles[i].x, (ulong)tiles[i].y, layer.Extent);
              points.Add(Conversion.ConvertGPSToPixel(latLng,
                      Conversion.GetBoundingBox(coord, config.zoom)
                    , (config.resolution, config.resolution)));
            }
            if (points.Count < 3)
            {
              continue;
            }
            // Draw a polygon
            image.Mutate(ctx => ctx.FillPolygon(drawingOptions, Brushes.Solid(config.colorScheme.Water), points.ToArray()));
          }
        }
      }
      for (int i = 0; i < tiles.Length; i++)
      {
        VectorTile tile = VectorTiles.GetTile(Conversion.ConvertTileToGPS(tiles[i].x, tiles[i].y, zoom), zoom);
        const string layerName = "greenspace";
        if (!tile.LayerNames().Contains(layerName))
        {
          continue;
        }
        VectorTileLayer layer = tile.GetLayer(layerName);
        for (int featureIdx = 0; featureIdx < layer.FeatureCount(); featureIdx++)
        {
          VectorTileFeature feature = layer.GetFeature(featureIdx);
          foreach (var part in feature.Geometry<int>())
          {
            List<PointF> points = new List<PointF>();
            foreach (var geom in part)
            {
              LatLng latLng = geom.ToLngLat(zoom, (ulong)tiles[i].x, (ulong)tiles[i].y, layer.Extent);
              points.Add(Conversion.ConvertGPSToPixel(latLng,
                      Conversion.GetBoundingBox(coord, config.zoom)
                    , (config.resolution, config.resolution)));
            }
            if (points.Count < 3)
            {
              continue;
            }
            // Draw a polygon
            image.Mutate(ctx => ctx.FillPolygon(drawingOptions, Brushes.Solid(config.colorScheme.Grass), points.ToArray()));
          }
        }
      }
      for (int i = 0; i < tiles.Length; i++)
      {
        VectorTile tile = VectorTiles.GetTile(Conversion.ConvertTileToGPS(tiles[i].x, tiles[i].y, zoom), zoom);
        const string layerName = "bus-route";
        if (!tile.LayerNames().Contains(layerName))
        {
          continue;
        }
        VectorTileLayer layer = tile.GetLayer(layerName);
        for (int featureIdx = 0; featureIdx < layer.FeatureCount(); featureIdx++)
        {
          VectorTileFeature feature = layer.GetFeature(featureIdx);
          foreach (var part in feature.Geometry<int>())
          {
            List<PointF> points = new List<PointF>();
            foreach (var geom in part)
            {
              LatLng latLng = geom.ToLngLat(zoom, (ulong)tiles[i].x, (ulong)tiles[i].y, layer.Extent);
              points.Add(Conversion.ConvertGPSToPixel(latLng,
                      Conversion.GetBoundingBox(coord, config.zoom)
                    , (config.resolution, config.resolution)));
            }
            if (points.Count < 3)
            {
              continue;
            }
            // Draw a polygon
            image.Mutate(ctx => ctx.DrawLine(drawingOptions, Brushes.Solid(config.colorScheme.Bus),1, points.ToArray()));
          }
        }
      }
      List<CanvasText> texts = new List<CanvasText>();
      for (int i = 0; i < tiles.Length; i++)
      {
        VectorTile tile = VectorTiles.GetTile(Conversion.ConvertTileToGPS(tiles[i].x, tiles[i].y, zoom), zoom);
        const string layerName = "bus-stop-label";
        if (!tile.LayerNames().Contains(layerName))
        {
          continue;
        }
        VectorTileLayer layer = tile.GetLayer(layerName);
        for(int featureIdx = 0; featureIdx < layer.FeatureCount(); featureIdx++)
        {
          VectorTileFeature feature = layer.GetFeature(featureIdx);
          foreach (var part in feature.Geometry<int>())
          {
            foreach (var geom in part)
            {
              LatLng latLng = geom.ToLngLat(zoom, (ulong)tiles[i].x, (ulong)tiles[i].y, layer.Extent);
              PointF point = Conversion.ConvertGPSToPixel(latLng,
                      Conversion.GetBoundingBox(coord, config.zoom)
                    , (config.resolution, config.resolution));
              texts.Add(new CanvasText ((int)point.X, (int)point.Y, feature.GetProperties()["name"].ToString(), SixLabors.ImageSharp.Color.Black)); 
            }
          }
        }
      }
      AnsiConsole.WriteLine("SimpleRender:");
      AnsiConsole.Write(new CanvasImageWithText(image, texts.ToArray()).NoMaxWidth());
    }
  }
}
