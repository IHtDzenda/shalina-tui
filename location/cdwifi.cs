using System.Text.Json;
using System.Text.Json.Serialization;
using Mapbox.VectorTile.Geometry;
namespace Core.Location.CDWiFi;

// http://cdwifi.cz/portal/api/vehicle/info
class CDWiFiVehicleInfo
{
  [JsonPropertyName("id")]
  public string Id { get; set; }
  [JsonPropertyName("deviceId")]
  public string DeviceId { get; set; }
  [JsonPropertyName("name")]
  public string Name { get; set; }
  [JsonPropertyName("group")]
  public string Group { get; set; }
  [JsonPropertyName("connextionId")]
  public int ConnexionId { get; set; }
  [JsonPropertyName("gpsLat")]
  public double GpsLat { get; set; }
  [JsonPropertyName("gpsLng")]
  public double GpsLng { get; set; }
  [JsonPropertyName("speed")]
  public int Speed { get; set; }
  [JsonPropertyName("altitude")]
  public double Altitude { get; set; }
}

public class CdWiFiLocation : LocationGetter
{
  const string url = "http://cdwifi.cz/portal/api/vehicle/info";

  protected async override Task<Location> InternalGetLocationAsync(Config config)
  {
    using (HttpClient client = new HttpClient())
    {
      HttpResponseMessage response = await client.GetAsync(url);
      if (!response.IsSuccessStatusCode)
      {
        throw new Exception("Request failed!");
      }
      CDWiFiVehicleInfo json = await JsonSerializer.DeserializeAsync<CDWiFiVehicleInfo>(await response.Content.ReadAsStreamAsync());
      return new Location{ 
        latLng = new LatLng{
          Lat = json.GpsLat,
          Lng = json.GpsLng
        },
        rotation = null,
        speed = json.Speed,
        altitude = json.Altitude
      };
    }
  }
}
