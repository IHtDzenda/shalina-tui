using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Mapbox.VectorTile.Geometry;

namespace Core.Api.Maps.Prague;
public class PidTransport
{
  [JsonPropertyName("lat")]
  public double Latitude { get; set; }

  [JsonPropertyName("lon")]
  public double Longitude { get; set; }

  [JsonPropertyName("tripId")]
  public string TripId { get; set; }

  [JsonPropertyName("route")]
  public string Route { get; set; }

  [JsonPropertyName("routeType")]
  public int RouteType { get; set; }

  [JsonPropertyName("bearing")]
  public int? Bearing { get; set; }

  [JsonPropertyName("delay")]
  public int Delay { get; set; }

  public bool Inactive { get; set; }

  [JsonPropertyName("statePosition")]
  public string StatePosition { get; set; }

  [JsonPropertyName("vehicle")]
  public string Vehicle { get; set; }
}

public class PidTransportsResponse
{

  public string Bottom { get; set; }

  [JsonPropertyName("alert")]
  public string Alert { get; set; }

  [JsonPropertyName("trips")]
  public Dictionary<string, PidTransport> Trips { get; set; }
}
public class PidData : TransportInterface
{
  public async Task<Transport[]> getData((LatLng min, LatLng max) boundingBox, bool useCache, Config config)
  {
    string url = "https://mapa.pid.cz/getData.php";
    string jsonData = "{\"action\":\"getData\",\"bounds\":[" + boundingBox.min.Lng + "," + boundingBox.min.Lat + "," + boundingBox.max.Lng + "," + boundingBox.max.Lat + "]}";

    using (HttpClient client = new HttpClient())
    {
      var content = new StringContent(jsonData, Encoding.UTF8, "application/json");
      HttpResponseMessage response = await client.PostAsync(url, content);
      if (!response.IsSuccessStatusCode)
      {
        throw new Exception("Request failed!");
      }
      string responseBody = await response.Content.ReadAsStringAsync();
      JsonSerializerOptions options = new JsonSerializerOptions
      {
        PropertyNameCaseInsensitive = true
      };
      PidTransportsResponse jsonResponse = JsonSerializer.Deserialize<PidTransportsResponse>(responseBody, options);
      if (jsonResponse != null && jsonResponse != null && jsonResponse.Trips.Count > 0)
      {
        Transport[] transports = new Transport[jsonResponse.Trips.Count];
        int i = 0;
        foreach (var item in jsonResponse.Trips)
        {
          transports[i] = new Transport
          {
            lat = item.Value.Latitude,
            lon = item.Value.Longitude,
            lineName = item.Value.Route,
            delay = item.Value.Delay,
            tripId = item.Value.TripId
          };
          i++;
        }
        return transports;
      }
      else
      {
        return Array.Empty<Transport>();
      }
    }
  }
}
