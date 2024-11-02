using Mapbox.VectorTile.Geometry;
using SixLabors.ImageSharp.PixelFormats;

namespace Core.Api.Maps
{

  public enum RouteType
  {
    Bus,
    Tram,
    Subway,
    Rail,
    Ferry,
    Trolleybus,
    Other
  }
  public static class LatLngExtensions
  {
    public static LatLng Add(this LatLng a, LatLng b)
    {
      return new LatLng { Lat = a.Lat + b.Lat, Lng = a.Lng + b.Lng };
    }
    public static LatLng Subtract(this LatLng a, LatLng b)
    {
      return new LatLng { Lat = a.Lat - b.Lat, Lng = a.Lng - b.Lng };
    }
    public static LatLng Multiply(this LatLng a, double b)
    {
      return new LatLng { Lat = a.Lat * b, Lng = a.Lng * b };
    }
    public static LatLng Divide(this LatLng a, double b)
    {
      return new LatLng { Lat = a.Lat / b, Lng = a.Lng / b };
    }
  }
  public class GeoData
  {
    public List<List<LatLng>> geometry { get; set; }
    public string routeId { get; set; }
    public string routeDisplayNumber { get; set; }
    public string routeNameLong { get; set; }
    public Rgb24 routeColor { get; set; }
    public string routeUrl { get; set; }
    public bool isSubsitute { get; set; }
    public bool isNightRoute { get; set; }
    public RouteType routeType { get; set; }
  }
  public interface GeoDataInterface
  {

    public abstract Task<GeoData[]> getData((LatLng min, LatLng max) boundingBox, bool useCache, Config config);
  }
}
