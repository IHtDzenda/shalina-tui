using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Core.Api.Maps
{

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

    [JsonPropertyName("inactive")]
    public bool Inactive { get; set; }

    [JsonPropertyName("statePosition")]
    public string StatePosition { get; set; }

    [JsonPropertyName("vehicle")]
    public string Vehicle { get; set; }
  }

  public class PidTransportsResponse
  {
    [JsonPropertyName("bottom")]
    public string Bottom { get; set; }

    [JsonPropertyName("alert")]
    public string Alert { get; set; }

    [JsonPropertyName("trips")]
    public Dictionary<string, PidTransport> Trips { get; set; }
  }
  public class Transport
  {
    public double lat { get; set; }
    public double lon { get; set; }
    public string? lineName { get; set; }
    public int delay { get; set; }
    public string? tripId { get; set; }
  }
  public interface TransportInterface
  {
    public async Task<Transport[]> getData()
    {
      throw new NotImplementedException();
    }
  }
  public class PidData : TransportInterface
  {
    public async Task<Transport[]> getData(double lonFrom = 7.483183523110626, double latFrom = 46.74962999246071, double lonTo = 21.737944391598802, double latTo = 53.26248060357497)
    {
      string url = "https://mapa.pid.cz/getData.php";
      string jsonData = "{\"action\":\"getData\",\"bounds\":[" + lonFrom + "," + latFrom + "," + lonTo + "," + latTo + "]}";

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
          Console.WriteLine("No transports found.");
          return Array.Empty<Transport>();
        }
      }
    }
  }
}
