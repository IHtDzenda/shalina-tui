using Core.Geometry;

namespace Core.Api.Maps;

public enum TripState
{
  Unknown,
  Active,
  AtStop,
  NotPublic,
  Inactive
}

public class Transport
{

  public double lat { get; set; }
  public double lon { get; set; }
  public string? lineName { get; set; }
  public int delay { get; set; }
  public string? tripId { get; set; }
  public TripState state { get; set; }
  public RouteType routeType { get; set; }
}
public abstract class TransportProvider
{
  private Dictionary<RouteType, Dictionary<string, Transport>> transportsCache;
  private BoundingBox boundingBox;
  private Config config;
  private Task cacheTask;
  private CancellationTokenSource cancellationTokenSource;

  private async Task CacheLoopAsync(CancellationToken token)
  {
    DateTime lastUpdate = DateTime.MinValue;
    TimeSpan updateInterval = TimeSpan.FromSeconds(1);

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
          break;
        }
      }

      transportsCache = await InternalGetTransportDataAsync(boundingBox * 2, config);
      lastUpdate = DateTime.Now;
    }
  }

  public abstract Task<Dictionary<RouteType, Dictionary<string, Transport>>> InternalGetTransportDataAsync(BoundingBox boundingBox, Config config);

  public async Task<Dictionary<RouteType, Dictionary<string, Transport>>> GetTransportDataAsync(BoundingBox boundingBox, Config config, bool useCache = true)
  {
    if (!useCache)
      return await InternalGetTransportDataAsync(boundingBox, config);

    this.boundingBox = boundingBox;
    this.config = config;

    if (transportsCache == null)
    {
      transportsCache = await InternalGetTransportDataAsync(boundingBox, config);

      if (cacheTask == null || cacheTask.IsCompleted)
      {
        cancellationTokenSource = new CancellationTokenSource();
        cacheTask = Task.Run(() => CacheLoopAsync(cancellationTokenSource.Token));
      }
    }

    return transportsCache;
  }

  public void StopCacheLoop()
  {
    cancellationTokenSource?.Cancel();
  }
}
