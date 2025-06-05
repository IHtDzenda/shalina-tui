using Mapbox.VectorTile.Geometry;
using Core;
using Core.Api.Maps;
using SixLabors.ImageSharp;
using Core.Geometry;

public class Stop
{
  public string name { get; set; }
  public string id { get; set; }
  public string alternativeName { get; set; }
  public LatLng location { get; set; }
  public string municipality { get; set; }
  public RouteType mainRouteType { get; set; }
  public RouteType[] routeTypes { get; set; }
  public (RouteType type, string name)[] lines { get; set; }
  public Color color { get; set; }
}

public abstract class StopsDataProvider
{
  private Stop[] stopsCache;
  private BoundingBox boundingBox;
  private Config config;
  private bool useDailyCache = true;

  private Task cacheTask;
  private CancellationTokenSource cancellationTokenSource;

  public abstract Task<Stop[]> internalGetStopsAsync(BoundingBox boundingBox, Config config, bool useCache = true);

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
          break; // Exit loop on cancellation
        }
      }

      stopsCache = await internalGetStopsAsync(boundingBox, config, useDailyCache);
      lastUpdate = DateTime.Now;
    }
  }

  public async Task<Stop[]> getStops(
    BoundingBox boundingBox,
    Config config,
    bool useCache = false,
    bool useDailyCache = true)
  {
    if (!useCache)
      return await internalGetStopsAsync(boundingBox, config, useDailyCache);

    this.boundingBox = boundingBox;
    this.config = config;
    this.useDailyCache = useDailyCache;

    if (stopsCache == null)
    {
      stopsCache = await internalGetStopsAsync(boundingBox, config, useDailyCache);

      if (cacheTask == null || cacheTask.IsCompleted)
      {
        cancellationTokenSource = new CancellationTokenSource();
        cacheTask = Task.Run(() => CacheLoopAsync(cancellationTokenSource.Token));
      }
    }

    return stopsCache;
  }

  public void StopCacheLoop()
  {
    cancellationTokenSource?.Cancel();
  }
}

