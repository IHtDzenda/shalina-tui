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
