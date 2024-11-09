using Core.Api.Maps;
using Core.Api.Maps.Prague;
using Core.Api.VectorTiles;
using Mapbox.VectorTile;
using Mapbox.VectorTile.Geometry;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Color = SixLabors.ImageSharp.Color;

namespace Core.Rendering;

public static class Renderer
{
  const int LINE_WIDTH = 1;
  public delegate void LayerFunction(Config config, LatLng coordinate, VectorTileLayer layer, (int x, int y) tile, byte zoom, (LatLng min, LatLng max) boundingBox);

  static Dictionary<string, LayerFunction> vectorTileLayers = new Dictionary<string, LayerFunction> {
    { "ocean", RenderVectorWater },
    { "greenspace", RenderVectorGreenspace },
    { "water", RenderVectorWater },
    { "waterway", RenderVectorWater },
    { "wetland", RenderVectorGreenspace },
  };
  static List<GeoDataInterface> cityGeoData = new List<GeoDataInterface> { new PidGeoData() };
  static List<StopsInterface> cityStopsData = new List<StopsInterface> { new PidStopData() };
  static List<TransportInterface> cityLiveData = new List<TransportInterface> { new PidLiveData() };


  static DrawingOptions drawingOptions = new DrawingOptions
  {
    GraphicsOptions = new GraphicsOptions
    {
      Antialias = false // Disable anti-aliasing
    }
  };

  static CanvasImageWithText image;
  private static void RenderVectorWater(Config config, LatLng coordinate, VectorTileLayer layer, (int x, int y) tile, byte zoom, (LatLng min, LatLng max) boundingBox)
  {
    for (int featureIdx = 0; featureIdx < layer.FeatureCount(); featureIdx++)
    {
      VectorTileFeature feature = layer.GetFeature(featureIdx);
      try
      {
        if (feature.GetValue("intermittent") != null || feature.GetValue("tunnel") != null)
        {
          continue;
        }
      }
      catch { }
      RenderVectorFeature(feature, tile, zoom, boundingBox, config.colorScheme["water"], layer.Extent, drawingOptions);
    }
  }
  private static void RenderVectorGreenspace(Config config, LatLng coordinate, VectorTileLayer layer, (int x, int y) tile, byte zoom, (LatLng min, LatLng max) boundingBox)
  {
    for (int featureIdx = 0; featureIdx < layer.FeatureCount(); featureIdx++)
      RenderVectorFeature(layer.GetFeature(featureIdx), tile, zoom, boundingBox, config.colorScheme["grass"], layer.Extent, drawingOptions);
  }
  private static void RenderVectorFeature(VectorTileFeature feature, (int x, int y) tile, byte zoom, (LatLng min, LatLng max) boundingBox, Rgb24 color, ulong extent, DrawingOptions drawingOptions)
  {
    if (feature.GeometryType == GeomType.POLYGON)
    {
      RenderVectorFeature(feature, tile, zoom, boundingBox, color, extent, 3, (ctx, points) => ctx.FillPolygon(drawingOptions, Brushes.Solid(color), points.ToArray()));
    }
    else if (feature.GeometryType == GeomType.LINESTRING)
    {
      RenderVectorFeature(feature, tile, zoom, boundingBox, color, extent, 2, (ctx, points) => ctx.DrawLine(drawingOptions, Brushes.Solid(color), LINE_WIDTH, points.ToArray()));
    }
    else
    {
      throw new NotImplementedException($"Geometry type {feature.GeometryType} not implemented");
    }
  }
  private static void RenderVectorFeature(VectorTileFeature feature, (int x, int y) tile, byte zoom, (LatLng min, LatLng max) boundingBox, Rgb24 color, ulong extent, byte minPoints, Action<IImageProcessingContext, List<PointF>> operation)
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

  // Render data from vector tiles (water, greenspace, etc.) - background
  private static void RenderVectorTiles(Config config, LatLng coordinate, (LatLng min, LatLng max) boundingBox)
  {
    byte zoom = config.zoom > 14 ? (byte)14 : config.zoom;
    (int x, int y)[] tiles = Conversion.GetTiles(coordinate, zoom);
    for (int i = 0; i < tiles.Length; i++)
    {
      VectorTile tile = VectorTiles.GetTile(Conversion.ConvertTileToGPS(tiles[i].x, tiles[i].y, zoom), zoom);
      foreach (var layer in tile.LayerNames())
      {
        if (vectorTileLayers.ContainsKey(layer))
        {
          vectorTileLayers[layer](config, coordinate, tile.GetLayer(layer), tiles[i], zoom, boundingBox);
        }
      }
    }
  }

  // Render data from geojson files (public transport, etc.) - city specific data(shows routes)
  private static void RenderGeoData(Config config, LatLng coordinate, (LatLng min, LatLng max) boundingBox)
  {
    foreach (var city in cityGeoData)
    {
      var data = city.getData(boundingBox, true, config).Result;
      List<GeoData> geoDataList = new List<GeoData>();
      foreach (var geoData in data)
      {
        RouteType type = geoData.Key;
        geoDataList.AddRange(geoData.Value.Where(x => config.query.MatchSingle((type, x))).Select(x => x.Value));
      }
      foreach (var geoData in geoDataList)
      {
        if (geoData.geometry == null)
        {
          continue;
        }
        foreach (var line in geoData.geometry)
        {
          image.Mutate(ctx =>
          {
            ctx.DrawLine(drawingOptions, Brushes.Solid(geoData.routeColor), LINE_WIDTH, line.Select(x => Conversion.ConvertGPSToPixel(x, boundingBox, (image.Width, image.Height))).ToArray());
          });
        }
      }
    }
  }
  // Render stop data (public transport, etc.) - city specific data(shows stops)
  private static void RenderStopData(Config config, LatLng coordinate, (LatLng min, LatLng max) boundingBox)
  {
    foreach (var city in cityStopsData)
    {
      Stop[] data = city.getData(boundingBox, config).Result;
      foreach (var stop in data.Where(x => config.query.MatchSingle(x)))
      {
        PointF point = Conversion.ConvertGPSToPixel(stop.location, boundingBox, (image.Width, image.Height));
        if (point.X < 0 || point.X >= image.Width || point.Y < 0 || point.Y >= image.Height)
          continue;
        image.AddText(new CanvasText((int)point.X + 1, (int)point.Y, stop.name, Color.Black));
        image.Image[(int)point.X, (int)point.Y] = stop.color;
      }
    }
  }

  // Render live data (public transport, etc.) - city specific data(shows vehicles)
  private static void RenderLiveData(Config config, LatLng coordinate, (LatLng min, LatLng max) boundingBox)
  {
    foreach (var city in cityLiveData)
    {
      var data = city.getData(boundingBox, config).Result;
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
        PointF point = Conversion.ConvertGPSToPixel(location, boundingBox, (image.Width, image.Height));
        if (point.X < 0 || point.X >= image.Width || point.Y < 0 || point.Y >= image.Height)
          continue;
        image.AddText(new CanvasText((int)point.X, (int)point.Y, transport.lineName, Color.Black));
        image.Image[(int)point.X, (int)point.Y] = Color.White;
      }
    }
  }
  public static CanvasImageWithText RenderMap(Config config, bool renderLive)
  {
    if (image == null || image.Width != config.resolution.width || image.Height != config.resolution.height)
      image = new CanvasImageWithText(new Image<Rgb24>(config.resolution.width, config.resolution.height, config.colorScheme["land"]));
    else
    {
      image.Image.Mutate(ctx => ctx.Clear(config.colorScheme["land"]));
      image.ClearTexts();
    }

    LatLng coord = new LatLng { Lat = config.latitude, Lng = config.longitude };
    (LatLng min, LatLng max) boundingBox = Conversion.GetBoundingBox(coord, config.zoom, (image.Width, image.Height));
    RenderVectorTiles(config, coord, boundingBox);
    if(config.zoom > 9)
      RenderGeoData(config, coord, boundingBox);
    if (renderLive && config.zoom > 12)
      RenderLiveData(config, coord, boundingBox);
    if(config.zoom > 12)
      RenderStopData(config, coord, boundingBox);

    return image;
  }
}

