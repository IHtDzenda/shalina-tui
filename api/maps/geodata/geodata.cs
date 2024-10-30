using System.Text.Json;
using System.Text.Json.Serialization;

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
  public struct GPSData
  {
    public double lat { get; set; }
    public double lon { get; set; }
    public GPSData(double latitude, double longitude)
    {
      lat = latitude;
      lon = longitude;
    }
    public override string ToString()
    {
      return $"lat: {lat}, lon: {lon}";
    }
    public static GPSData operator -(GPSData a, GPSData b)
    {
      return new GPSData(a.lat - b.lat, a.lon - b.lon);
    }
    public static GPSData operator +(GPSData a, GPSData b)
    {
      return new GPSData(a.lat + b.lat, a.lon + b.lon);
    }
    public static GPSData operator /(GPSData a, double b)
    {
      return new GPSData(a.lat / b, a.lon / b);
    }
    public static GPSData operator *(GPSData a, double b)
    {
      return new GPSData(a.lat * b, a.lon * b);
    }
  }
  public class GeoData
  {
    public List<GPSData> geometry { get; set; }
    public string routeId { get; set; }
    public string routeDisplayNumber { get; set; }
    public string routeNameLong { get; set; }
    public string routeColor { get; set; }
    public string routeUrl { get; set; }
    public bool isSubsitute { get; set; }
    public bool isNightRoute { get; set; }
    public RouteType routeType { get; set; }
  }
  public interface GeoDataInterface
  {

    public async Task<GeoData[]> getData()
    {
      throw new NotImplementedException();
    }
  }
}
