using System.Text.Json;
using System.Text.Json.Serialization;
using Mapbox.VectorTile.Geometry;
using SixLabors.ImageSharp;

namespace Core.Api.Maps.Prague;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum WheelChairAccess
{
  notPossible,
  possible,
  unknown
}
public class TrafficTypeConvertor : JsonConverter<RouteType>
{
  public override RouteType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
  {
    string value = reader.GetString()?.ToLowerInvariant();

    if(value.StartsWith("metro", StringComparison.InvariantCultureIgnoreCase))
    {
      return RouteType.Subway;
      
    }
    return value switch
    {
      "bus" => RouteType.Bus,
      "ferry" => RouteType.Ferry,
      "train" => RouteType.Rail,
      "tram" => RouteType.Tram,
      "trolleybus" => RouteType.Trolleybus,
      _ => RouteType.Other
    };
  }

  public override void Write(Utf8JsonWriter writer, RouteType value, JsonSerializerOptions options)
  {
    writer.WriteStringValue(value.ToString());
  }
}
public class PidLine
{
  [JsonPropertyName("id")]
  public int Id { get; set; }
  [JsonPropertyName("name")]
  public string Name { get; set; }
  [JsonPropertyName("type")]
  public RouteType Type { get; set; }
  [JsonPropertyName("direction")]
  public string Direction { get; set; }
  [JsonPropertyName("direction2")]
  public string Direction2 { get; set; }
}
public class PidStop
{
  [JsonPropertyName("id")]
  public string Id { get; set; }
  [JsonPropertyName("platform")]
  public string Platform { get; set; }
  [JsonPropertyName("altIdosName")]
  public string AltIdosName { get; set; }
  [JsonPropertyName("lat")]
  public double Lat { get; set; }
  [JsonPropertyName("lon")]
  public double Lon { get; set; }
  [JsonPropertyName("jtskX")]
  public double JtskX { get; set; }
  [JsonPropertyName("jtskY")]
  public double JtskY { get; set; }
  [JsonPropertyName("zone")]
  public string Zone { get; set; }
  [JsonPropertyName("mainTrafficType")]
  public RouteType MainTrafficType { get; set; }
  [JsonPropertyName("wheelchairAccess")]
  public WheelChairAccess WheelchairAccess { get; set; }
  [JsonPropertyName("gtfsIds")]
  public List<string> GtfsIds { get; set; }
  [JsonPropertyName("lines")]
  public List<PidLine> Lines { get; set; }
}

public class PidStopGroup
{
  [JsonPropertyName("name")]
  public string Name { get; set; }
  [JsonPropertyName("districtCode")]
  public string DistrictCode { get; set; }
  [JsonPropertyName("idosCategory")]
  public int IdosCategory { get; set; }
  [JsonPropertyName("idosName")]
  public string IdosName { get; set; }
  [JsonPropertyName("fullName")]
  public string FullName { get; set; }
  [JsonPropertyName("uniqueName")]
  public string UniqueName { get; set; }
  [JsonPropertyName("node")]
  public int Node { get; set; }
  [JsonPropertyName("cis")]
  public int Cis { get; set; }
  [JsonPropertyName("avgLat")]
  public double AvgLat { get; set; }
  [JsonPropertyName("avgLon")]
  public double AvgLon { get; set; }
  [JsonPropertyName("avgJtskX")]
  public double AvgJtskX { get; set; }
  [JsonPropertyName("avgJtskY")]
  public double AvgJtskY { get; set; }
  [JsonPropertyName("municipality")]
  public string Municipality { get; set; }
  [JsonPropertyName("mainTrafficType")]
  public RouteType MainTrafficType { get; set; }
  [JsonPropertyName("stops")]
  public List<PidStop> Stops { get; set; }
}
public class PidStopResponse
{
  [JsonPropertyName("generatedAt")]
  public DateTime GeneratedAt { get; set; }
  [JsonPropertyName("dataFormatVersion")]
  public string DataFormatVersion { get; set; }
  [JsonPropertyName("stopGroups")]
  public List<PidStopGroup> StopGroups { get; set; }
}
public class PidStopData : StopsInterface
{
  public override async Task<Stop[]> getStops((LatLng min, LatLng max) boundingBox, Config config)
  {
    var url = "https://data.pid.cz/stops/json/stops.json";
    string date = DateTime.Now.ToString("yyyy-MM-dd");
    string filePath = $"{Util.CheckForCacheDir()}/PIDStops_{date}.json";
    PidStopResponse pidStops;
    using (HttpClient client = new HttpClient())
    {
      HttpResponseMessage response = await client.GetAsync(url);
      if (!response.IsSuccessStatusCode)
      {
        throw new Exception("Request failed!");
      }
      JsonSerializerOptions options = new JsonSerializerOptions
      {
        PropertyNameCaseInsensitive = true,
        Converters = { new TrafficTypeConvertor() }
      };
      pidStops = await JsonSerializer.DeserializeAsync<PidStopResponse>(await response.Content.ReadAsStreamAsync(), options);
      File.WriteAllText(filePath, JsonSerializer.Serialize(pidStops, options));
    }
    return pidStops.StopGroups.Select(sg =>
    {
      return new Stop
      {
        name = sg.Name,
        id = sg.UniqueName,
        location = new LatLng { Lat = sg.AvgLat, Lng = sg.AvgLon },
        municipality = sg.Municipality,
        mainRouteType = sg.MainTrafficType,
        color = Color.Black,
      };
    }).ToArray();
  }
}

