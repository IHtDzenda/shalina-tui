using Mapbox.VectorTile.Geometry;

namespace Core.Api.Maps;

public enum TripState{
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
public interface TransportInterface
{
  public abstract Task<Transport[]> getData((LatLng min, LatLng max) boundingBox, bool useCache, Config config);
}
