using Mapbox.VectorTile.Geometry;
using Core;
using Core.Api.Maps;
using SixLabors.ImageSharp;

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

public abstract class StopsInterface
{
  private Stop[] transportsCache;
  private (LatLng min, LatLng max) boundingBox;
  private DateTime lastUpdated = DateTime.MinValue;

  public abstract Task<Stop[]> getStops((LatLng min, LatLng max) boundingBox, Config config, bool useCache = true);

  public async Task<Stop[]> getData((LatLng min, LatLng max) boundingBox, Config config, bool useCache = true)
  {
    if (!useCache)
      return await getStops(boundingBox, config);

    this.boundingBox = boundingBox;
    if (transportsCache == null || lastUpdated.Day != DateTime.Now.Day)
    {
      transportsCache = await getStops(boundingBox, config);
      lastUpdated = DateTime.Now;
    }
    return transportsCache;
  }
}

