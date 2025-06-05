using System.Text.Json;
using System.Text.Json.Serialization;
using Core.Geometry;
using Mapbox.VectorTile.Geometry;
using SixLabors.ImageSharp.PixelFormats;
using Color = SixLabors.ImageSharp.Color;


namespace Core.Api.Maps;

[JsonConverter(typeof(RouteTypeConverter))]
public enum RouteType
{
  Bus,
  Tram,
  Subway,
  Train,
  Ferry,
  Trolleybus,
  CableCar,
  Other
}

public class RouteTypeConverter : JsonConverter<RouteType>
{
  public override RouteType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
  {
    string value = reader.GetString()?.ToLowerInvariant();

    return value switch
    {
      "bus" => RouteType.Bus,
      "tram" => RouteType.Tram,
      "metro" => RouteType.Subway,
      "subway" => RouteType.Subway,
      "rail" => RouteType.Train,
      "train" => RouteType.Train,
      "ferry" => RouteType.Ferry,
      "trolleybus" => RouteType.Trolleybus,
      _ => RouteType.Other // Default case for unknown values
    };
  }

  public override void Write(Utf8JsonWriter writer, RouteType value, JsonSerializerOptions options)
  {
    writer.WriteStringValue(value.ToString());
  }
}

public class GeoData
{
  public List<List<LatLng>> geometry { get; }
  public BoundingBox boundingBox { get; }
  public string routeId { get; }
  public string routeNameLong { get; }
  public Rgb24 routeColor { get; }
  public string routeUrl { get; }
  public bool isSubsitute { get; }
  public bool isNightRoute { get; }

  public GeoData(Config config, List<List<LatLng>> _geometry, string _routeId, string _routeNameLong, RouteType _routeType, string _routeUrl, bool _isSubstitute, bool _isNightRoute, Rgb24? customColor = null)
  {
    this.geometry = _geometry;
    this.boundingBox = new BoundingBox(_geometry.SelectMany(i => i).ToList());
    this.routeId = _routeId;
    this.routeNameLong = _routeNameLong;
    if (customColor.HasValue)
      this.routeColor = customColor.Value;
    else
      this.routeColor = config.colorScheme[_routeType.ToString().ToLower()];

    this.routeUrl = _routeUrl;
    this.isSubsitute = _isSubstitute;
    this.isNightRoute = _isNightRoute;
  }
  public override string ToString()
  {
    return $"GeoData(routeId: {routeId}, routeNameLong: {routeNameLong}, routeColor: {routeColor}, isSubstitute: {isSubsitute}, isNightRoute: {isNightRoute})";
  }
}

public abstract class GeoDataProvider
{
  private Dictionary<RouteType, Dictionary<string, GeoData>> geoDataCache;
  private BoundingBox boundingBox;
  private Config config;
  private bool useDailyCache = true;

  private Task cacheTask;
  private CancellationTokenSource cancellationTokenSource;

  private async Task CacheLoopAsync(CancellationToken token)
  {
    DateTime lastUpdate = DateTime.MinValue;
    TimeSpan updateInterval = TimeSpan.FromSeconds(60);

    while (!token.IsCancellationRequested)
    {
      TimeSpan waitTime = (lastUpdate + updateInterval) - DateTime.Now;
      if (waitTime > TimeSpan.Zero)
      {
        try
        {
          await Task.Delay(waitTime, token);
        }
        catch (TaskCanceledException)
        {
          break; // Graceful exit
        }
      }

      geoDataCache = await internalGetLocationAsync(boundingBox, config, useDailyCache);
      lastUpdate = DateTime.Now;
    }
  }

  public abstract Task<Dictionary<RouteType, Dictionary<string, GeoData>>> internalGetLocationAsync(BoundingBox boundingBox, Config config, bool useCache = true);

  public async Task<Dictionary<RouteType, Dictionary<string, GeoData>>> getGeoDataAsync(
    BoundingBox boundingBox,
    Config config,
    bool useCache = false,
    bool useDailyCache = true)
  {
    if (!useCache)
      return await internalGetLocationAsync(boundingBox, config, useDailyCache);

    this.boundingBox = boundingBox;
    this.config = config;
    this.useDailyCache = useDailyCache;

    if (geoDataCache == null)
    {
      geoDataCache = await internalGetLocationAsync(boundingBox, config, useDailyCache);

      if (cacheTask == null || cacheTask.IsCompleted)
      {
        cancellationTokenSource = new CancellationTokenSource();
        cacheTask = Task.Run(() => CacheLoopAsync(cancellationTokenSource.Token));
      }
    }

    return geoDataCache;
  }

  public void StopCacheLoop()
  {
    cancellationTokenSource?.Cancel();
  }
}
