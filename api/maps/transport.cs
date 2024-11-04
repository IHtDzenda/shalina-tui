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
}
public abstract class TransportInterface
{
  private Transport[] transportsCache;
  private (LatLng min, LatLng max) boundingBox;

  private void CacheThread(Config config)
  {
    while (true)
    {
      Thread.Sleep(5000); // TODO: Configurable
      transportsCache = getData(
        boundingBox,
        config,
        false).Result; // The whole Czech Republic
    }
  }

  public abstract Task<Transport[]> getTransports((LatLng min, LatLng max) boundingBox, Config config);

  public async Task<Transport[]> getData((LatLng min, LatLng max) boundingBox, Config config, bool useCache = true)
  {
    if (!useCache)
      return await getTransports(boundingBox, config);

    if (transportsCache == null)
    {
      transportsCache = await getTransports(boundingBox, config);
      this.boundingBox = boundingBox;
      Thread thread = new Thread(() => CacheThread(config));
      thread.IsBackground = true;
      thread.Start();
    }
    if (boundingBox.min.Equals(this.boundingBox.min) && boundingBox.max.Equals(this.boundingBox.max))
      return transportsCache;
    else
    {
      transportsCache = await getTransports(boundingBox, config);
      this.boundingBox = boundingBox;
    }
    return transportsCache;
  }
}
