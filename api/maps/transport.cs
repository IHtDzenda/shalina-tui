using Mapbox.VectorTile.Geometry;

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
public abstract class TransportInterface
{
  private Dictionary<RouteType, Dictionary<string, Transport>> transportsCache;
  private BoundingBox boundingBox;

  private void CacheThread(Config config)
  {
    BoundingBox getExpandedBoundingBox(BoundingBox boundingBox) =>
      new BoundingBox {
        min = boundingBox.min.Subtract(boundingBox.max.Subtract(boundingBox.min).Divide(2)),
        max = boundingBox.max.Add(boundingBox.max.Subtract(boundingBox.min).Divide(2)),
      };
    while (true)
    {
      Thread.Sleep(1000); // TODO: Configurable
      transportsCache = getData(
        getExpandedBoundingBox(boundingBox),
        config,
        false).Result;
    }
  }

  public abstract Task<Dictionary<RouteType, Dictionary<string, Transport>>> getTransports(BoundingBox boundingBox, Config config);

  public async Task<Dictionary<RouteType, Dictionary<string, Transport>>> getData(BoundingBox boundingBox, Config config, bool useCache = true)
  {
    if (!useCache)
      return await getTransports(boundingBox, config);

    this.boundingBox = boundingBox;
    if (transportsCache == null)
    {
      transportsCache = await getTransports(boundingBox, config);
      Thread thread = new Thread(() => CacheThread(config));
      thread.IsBackground = true;
      thread.Start();
    }
    return transportsCache;
  }
}
