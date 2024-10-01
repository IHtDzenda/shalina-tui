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

    public async Task<string> getData()
    {
      throw new NotImplementedException();
    }
  }

  public class PragueGeoData : GeoDataInterface
  {
    public async Task<string> getData(bool useCache = true)
    {

      useCache = false;
      string date = DateTime.Now.ToString("yyyy-MM-dd");
      string filePath = $"{Util.Util.CheckForCacheDir()}/geodata_{date}.json";
      string content = "";
      if (!File.Exists(filePath) || !useCache)
      {
        string url = "https://data.pid.cz/geodata/Linky_WGS84.json";
        using (HttpClient client = new HttpClient())
        {
          HttpResponseMessage response = await client.GetAsync(url);
          if (!response.IsSuccessStatusCode)
          {
            throw new Exception("Request failed!");
          }
          content = await response.Content.ReadAsStringAsync();
          File.WriteAllText(filePath, content);
        }
      }
      else
      {
        content = File.ReadAllText(filePath);
      }
      JsonSerializerOptions options = new JsonSerializerOptions
      {
        PropertyNameCaseInsensitive = true,
        MaxDepth = 64,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
      };
      PragueGeoDataResponse jsonResponse = JsonSerializer.Deserialize<PragueGeoDataResponse>(content, options);
      GeoData[] geoData = new GeoData[jsonResponse.features.Count];

      int i = 0;
      foreach (var item in jsonResponse.features)
      {
        geoData[i] = new GeoData
        {
          geometry = item.geometry.coordinates,
          routeId = item.properties.route_id,
          routeDisplayNumber = item.properties.route_short_name,
          routeNameLong = item.properties.route_long_name,
          routeColor = item.properties.route_color,
          routeUrl = item.properties.route_url,
          isSubsitute = item.properties.is_substitute_transport == "1",
          isNightRoute = item.properties.is_night == "1",
        };
        i++;
        Console.WriteLine(item.properties.route_id);
      }
      return "";
    }
  }
}
