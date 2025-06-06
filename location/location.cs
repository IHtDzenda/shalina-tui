using Core.Geometry;
using Mapbox.VectorTile.Geometry;

namespace Core.Location;

public class Location
{
  public required LatLng latLng { get; set; }
  public BoundingBox? boundingBox { get; set; }

  private double? _rotation;
  public double? rotation
  {
    get => _rotation;
    set => _rotation = value % 360; // Normalize rotation
  }

  public double? speed { get; set; }     // in km/h
  public double? altitude { get; set; }  // in meters

  public override string ToString()
  {
    return $"Location(LatLng: {latLng}, Rotation: {rotation}, Speed: {speed}, Altitude: {altitude})";
  }
}

public abstract class LocationGetter
{
  private Location locationCache;
  private Config config;
  private Task cacheTask;
  private CancellationTokenSource cancellationTokenSource;
  private readonly TimeSpan updateInterval = TimeSpan.FromSeconds(3);

  protected abstract Task<Location> InternalGetLocationAsync(Config config);

  public async Task<Location> GetLocationAsync(Config config, bool useCache = true)
  {
    if (!useCache)
      return await InternalGetLocationAsync(config);

    this.config = config;

    if (locationCache == null)
    {
      locationCache = await InternalGetLocationAsync(config);

      if (cacheTask == null || cacheTask.IsCompleted)
      {
        cancellationTokenSource = new CancellationTokenSource();
        cacheTask = Task.Run(() => CacheLoopAsync(cancellationTokenSource.Token));
      }
    }

    return locationCache;
  }

  private async Task CacheLoopAsync(CancellationToken token)
  {
    DateTime lastUpdate = DateTime.MinValue;

    while (!token.IsCancellationRequested)
    {
      TimeSpan delay = (lastUpdate + updateInterval) - DateTime.Now;
      if (delay > TimeSpan.Zero)
      {
        try
        {
          await Task.Delay(delay, token);
        }
        catch (TaskCanceledException)
        {
          break; // Graceful exit
        }
      }

      try
      {
        locationCache = await InternalGetLocationAsync(config);
        lastUpdate = DateTime.Now;
      }
      catch
      {
        // Optional: log or handle retrieval failure
      }
    }
  }

  public void StopCacheLoop()
  {
    cancellationTokenSource?.Cancel();
  }
}
