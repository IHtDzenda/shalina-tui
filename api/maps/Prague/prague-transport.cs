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
public class PidLiveData : TransportInterface
{
  private static TripState GetTripState(PidTransport transport)
  {
    if (transport.Inactive)
      return TripState.Inactive;
    return transport.StatePosition switch
    {
      "at_stop" => TripState.AtStop,
      "not_public" => TripState.NotPublic,
      "on_track" => TripState.Active,
      _ => TripState.Unknown
    };
  }
  static Dictionary<int, RouteType> routeTypeMap = new Dictionary<int, RouteType>
    {
      { 3, RouteType.Bus },
      { 11, RouteType.Trolleybus },
      { 7, RouteType.Ferry },
      { 1, RouteType.Subway },
      { 2, RouteType.Rail },
      { 0, RouteType.Tram },
    };
  public override async Task<Dictionary<RouteType, Dictionary<string, Transport>>> getTransports((LatLng min, LatLng max) boundingBox, Config config)
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

      if (jsonResponse == null || jsonResponse.Trips.Count == 0)
        return new Dictionary<RouteType, Dictionary<string, Transport>>();

      int count = jsonResponse.Trips.Count;
      Dictionary<RouteType, Dictionary<string, Transport>> transports = new Dictionary<RouteType, Dictionary<string, Transport>>(Enum.GetValues(typeof(RouteType)).Length);
      foreach (var type in Enum.GetValues(typeof(RouteType)).Cast<RouteType>())
      {
        transports[type] = new Dictionary<string, Transport>(jsonResponse.Trips.Where(// Pre-allocate dictionary for better performance
              trip => routeTypeMap.GetValueOrDefault(trip.Value.RouteType, RouteType.Other) == type).Count());
      }
      for (int tripIndex = 0; tripIndex < count; tripIndex++)
      {
        PidTransport transport = jsonResponse.Trips.ElementAt(tripIndex).Value;
        RouteType routeType = routeTypeMap.GetValueOrDefault(transport.RouteType, RouteType.Other);
        transports[routeType].Add(transport.TripId, new Transport
        {
          lat = transport.Latitude,
          lon = transport.Longitude,
          lineName = transport.Route,
          delay = transport.Delay,
          tripId = transport.TripId,
          state = GetTripState(transport),
          routeType = routeType
        });
      }
      return transports;
    }
  }
}
