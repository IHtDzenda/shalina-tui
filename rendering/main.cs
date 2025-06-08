using Core.Api.Maps;
using Core.Api.VectorTiles;
using Core.Geometry;
using Mapbox.VectorTile;
using Mapbox.VectorTile.Geometry;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Color = SixLabors.ImageSharp.Color;

namespace Core.Rendering;

public static class Renderer
{
  const int LINE_WIDTH = 1;
  public delegate void LayerFunction(Config config, VectorTileLayer layer, (int x, int y) tile, byte zoom);

  static Dictionary<string, LayerFunction> vectorTileLayers = new Dictionary<string, LayerFunction> {
    { "ocean", RenderVectorWater },
    { "greenspace", RenderVectorGreenspace },
    { "water", RenderVectorWater },
    { "waterway", RenderVectorWater },
    { "wetland", RenderVectorGreenspace },
    // { "admin", ShowDebugVectorData },
    { "water-feature", ShowDebugVectorData },
    // { "place-label", RenderVectorPlaceLabel },
  };


  static DrawingOptions drawingOptions = new DrawingOptions
  {
    GraphicsOptions = new GraphicsOptions
    {
      Antialias = false // Disable anti-aliasing
    }
  };

  static CanvasImageWithText image = new CanvasImageWithText(new Image<Rgb24>(1, 1, Color.Transparent));

  private static void RenderVectorWater(Config config, VectorTileLayer layer, (int x, int y) tile, byte zoom)
  {
    for (int featureIdx = 0; featureIdx < layer.FeatureCount(); featureIdx++)
    {
      VectorTileFeature feature = layer.GetFeature(featureIdx);
      try
      {
        if (feature.Layer.Keys.Contains("intermittent") ||
            feature.Layer.Keys.Contains("tunnel"))
        {
          continue;
        }
      }
      catch { }
      RenderVectorFeature(config, feature, tile, zoom, config.colorScheme["water"], layer.Extent, drawingOptions);
    }
  }
  private static void RenderVectorGreenspace(Config config, VectorTileLayer layer, (int x, int y) tile, byte zoom)
  {
    for (int featureIdx = 0; featureIdx < layer.FeatureCount(); featureIdx++)
      RenderVectorFeature(config, layer.GetFeature(featureIdx), tile, zoom, config.colorScheme["grass"], layer.Extent, drawingOptions);
  }

  private static void RenderVectorFeature(Config config, VectorTileFeature feature, (int x, int y) tile, byte zoom, Rgb24 color, ulong extent, DrawingOptions drawingOptions)
  {
    switch (feature.GeometryType)
    {
      case GeomType.POLYGON:

        RenderVectorFeature(feature, tile, zoom, config.boundingBox, color, extent, 3, (ctx, points) => ctx.FillPolygon(drawingOptions, Brushes.Solid(color), points.ToArray()));
        break;
      case GeomType.LINESTRING:
        RenderVectorFeature(feature, tile, zoom, config.boundingBox, color, extent, 2, (ctx, points) => ctx.DrawLine(drawingOptions, Brushes.Solid(color), LINE_WIDTH, points.ToArray()));
        break;

      case GeomType.POINT:
        RenderVectorFeature(feature, tile, zoom, config.boundingBox, color, extent, 1, (ctx, points) => ctx.Fill(drawingOptions, Brushes.Solid(color), new EllipsePolygon(points[0], LINE_WIDTH)));
        break;

      default:
        throw new NotImplementedException("Unknown geometry type");
    };

  }

  private static void RenderVectorFeature(VectorTileFeature feature, (int x, int y) tile, byte zoom, BoundingBox boundingBox, Rgb24 color, ulong extent, byte minPoints, Action<IImageProcessingContext, List<PointF>> operation)
  {
    foreach (var part in feature.Geometry<int>())
    {
      List<PointF> points = new List<PointF>();
      foreach (var geom in part)
      {
        LatLng latLng = geom.ToLngLat(zoom, (ulong)tile.x, (ulong)tile.y, extent);
        points.Add(Conversion.ConvertGPSToPixel(latLng,
                boundingBox
              , (image.Width, image.Height)));
      }
      if (points.Count < minPoints)
      {
        continue;
      }
      // Draw from points
      image.Mutate(ctx => operation(ctx, points));
    }
  }
  private static void RenderVectorPlaceLabel(Config config, VectorTileLayer layer, (int x, int y) tile, byte zoom)
  {
    for (int featureIdx = 0; featureIdx < layer.FeatureCount(); featureIdx++)
    {
      VectorTileFeature feature = layer.GetFeature(featureIdx);
      if (feature.GeometryType != GeomType.POINT)
        continue;
      try
      {
        if (feature.Layer.Keys.Contains("intermittent") ||
            feature.Layer.Keys.Contains("tunnel"))
        {
          continue;
        }
      }
      catch { }
      string name = feature.GetValue("name")?.ToString() ?? "Unknown";
      LatLng latLng = feature.Geometry<int>().First().First().ToLngLat(zoom, (ulong)tile.x, (ulong)tile.y, layer.Extent);
      PointF point = Conversion.ConvertGPSToPixel(latLng, config.boundingBox, (image.Width, image.Height));
      if (point.X < 0 || point.X >= image.Width || point.Y < 0 || point.Y >= image.Height)
        continue;
      image.AddText(new CanvasText((int)point.X, (int)point.Y, name, Color.Black));
    }
  }

  private static void ShowDebugVectorData(Config config, VectorTileLayer layer, (int x, int y) tile, byte zoom)
  {
    for (int featureIdx = 0; featureIdx < layer.FeatureCount(); featureIdx++)
    {
      VectorTileFeature feature = layer.GetFeature(featureIdx);
      if (feature.GeometryType != GeomType.POLYGON &&
          feature.GeometryType != GeomType.LINESTRING
          )
        continue;
      if (feature.GetValue("type").ToString() != "weir")
        continue;

      foreach (var part in feature.Geometry<int>())
      {
        List<PointF> points = new List<PointF>();
        foreach (var geom in part)
        {
          LatLng latLng = geom.ToLngLat(zoom, (ulong)tile.x, (ulong)tile.y, layer.Extent);
          points.Add(Conversion.ConvertGPSToPixel(latLng,
                  config.boundingBox
                , (image.Width, image.Height)));
        }
        if (points.Count < 2)
        {
          continue;
        }
        // Draw from points
        image.AddSubPixelLine(
          points.Select(p => new PointF(p.X, p.Y)).ToArray(),
          new Rgb24((byte)(config.colorScheme["water"].R ), (byte)(config.colorScheme["water"].G), (byte)(config.colorScheme["water"].B - 20)) // Darker color for debug
        );
      }
    }
  }

  // Render data from vector tiles (water, greenspace, etc.) - background
  private static void RenderVectorTiles(Config config)
  {
    var coordinate = config.boundingBox.Center();
    byte zoom = Math.Min((byte)14, (byte)config.boundingBox.Zoom());
    (int x, int y)[] tiles = Conversion.GetTiles(coordinate, zoom, 1);
    for (int i = 0; i < tiles.Length; i++)
    {
      VectorTile tile = VectorTiles.GetTile((tiles)[i], zoom);
      foreach (var layer in tile.LayerNames())
      {
        if (vectorTileLayers.ContainsKey(layer))
        {
          vectorTileLayers[layer](config, tile.GetLayer(layer), tiles[i], zoom);
        }
      }
    }
  }

  // Render data from geojson files (public transport, etc.) - city specific data(shows routes)
  private static void RenderGeoData(Config config)
  {
    foreach (var city in config.cityGeoData)
    {
      var data = city.GetGeoDataAsync(config.boundingBox, config).Result;
      List<GeoData> geoDataList = new List<GeoData>();
      foreach (var geoData in data)
      {
        RouteType type = geoData.Key;
        geoDataList.AddRange(geoData.Value.Where(x => config.query.MatchSingle((type, x))).Select(x => x.Value));
      }
      foreach (var geoData in geoDataList)
      {
        if (geoData.geometry == null)
          continue;
        if (!config.boundingBox.Overlaps(geoData.boundingBox))
        {
          continue;
        }
        foreach (var line in geoData.geometry)
        {
          image.Mutate(ctx =>
          {
            ctx.DrawLine(drawingOptions, Brushes.Solid(geoData.routeColor), LINE_WIDTH, line.Select(x => Conversion.ConvertGPSToPixel(x, config.boundingBox, (image.Width, image.Height))).ToArray());
          });
        }
      }
    }
  }
  // Render stop data (public transport, etc.) - city specific data(shows stops)
  private static void RenderStopData(Config config)
  {
    foreach (var city in config.cityStopsData)
    {
      Stop[] data = city.GetStops(config.boundingBox, config).Result;
      foreach (var stop in data.Where(x => config.boundingBox.Contains(x.location)).Where(x => config.query.MatchSingle(x)))
      {
        PointF point = Conversion.ConvertGPSToPixel(stop.location, config.boundingBox, (image.Width, image.Height));
        if (point.X < 0 || point.X >= image.Width || point.Y < 0 || point.Y >= image.Height)
          continue;
        image.AddText(new CanvasText((int)point.X + 1, (int)point.Y, stop.name, Color.Black));
        image.Image[(int)point.X, (int)point.Y] = stop.color;
      }
    }
  }

  // Render live data (public transport, etc.) - city specific data(shows vehicles)
  private static void RenderLiveData(Config config)
  {
    foreach (var city in config.cityLiveData)
    {
      var data = city.GetTransportDataAsync(config.boundingBox, config).Result;
      List<Transport> transports = new List<Transport>();
      foreach (var transport in data)
      {
        RouteType type = transport.Key;
        transports.AddRange(transport.Value.Where(x => config.query.MatchSingle((type, x))).Select(x => x.Value));
      }
      foreach (var transport in transports)
      {
        if (transport.state == TripState.Inactive || transport.state == TripState.NotPublic) //TODO: Configurable
          continue;
        LatLng location = new LatLng { Lat = transport.lat, Lng = transport.lon };
        PointF point = Conversion.ConvertGPSToPixel(location, config.boundingBox, (image.Width, image.Height));
        if (point.X < 0 || point.X >= image.Width || point.Y < 0 || point.Y >= image.Height)
          continue;
        image.AddText(new CanvasText((int)point.X, (int)point.Y, transport.lineName, Color.Black));
        image.Image[(int)point.X, (int)point.Y] = Color.White;
      }
    }
  }
  public static async Task<CanvasImageWithText> RenderMapAsync(Config config, bool renderLive)
  {
    if (image == null || image.Width != config.resolution.width || image.Height != config.resolution.height)
      image = new CanvasImageWithText(new Image<Rgb24>(config.resolution.width, config.resolution.height, config.colorScheme["land"]));

 else
      image.Clear(config.colorScheme["land"]);


    RenderVectorTiles(config);
    RenderGeoData(config); //TODO render only if ...
    RenderLiveData(config);//TODO render only if ...
    RenderStopData(config);//TODO render only if ...

    return image;
  }
}

