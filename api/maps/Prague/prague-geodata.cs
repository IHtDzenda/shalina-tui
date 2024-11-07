using System.Text.Json;
using System.Text.Json.Serialization;
using Mapbox.VectorTile.Geometry;


namespace Core.Api.Maps.Prague;
public class PragueGeoDataResponseFeature
{
  public string type { get; set; }
  public PragueGeoDataResponseGeometry geometry { get; set; }
  public PragueGeoDataResponseProperties properties { get; set; }
}


public class PragueGeoDataResponseGeometry
{
  public string type { get; set; }

  [JsonConverter(typeof(CoordinatesConverter))]
  public List<List<LatLng>> coordinates { get; set; }
}

public class PragueGeoDataResponseProperties
{
  public int OBJECTID { get; set; }
  public string route_id { get; set; }
  public string route_short_name { get; set; }
  public string route_long_name { get; set; }
  public string route_type { get; set; }
  public string route_url { get; set; }
  public string route_color { get; set; }
  public string is_night { get; set; }
  public string is_regional { get; set; }
  public string is_substitute_transport { get; set; }
  public string validity { get; set; }
  public double Shape_Length { get; set; }
}

public class PragueGeoDataResponse
{
  public List<PragueGeoDataResponseFeature> features { get; set; }
}

public class CoordinatesConverter : JsonConverter<List<List<LatLng>>>
{
  public override List<List<LatLng>> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
  {
    var gpsDataList = new List<List<LatLng>>();

    if (reader.TokenType != JsonTokenType.StartArray)
    {
      throw new JsonException("Expected an array for coordinates.");
    }

    using (var document = JsonDocument.ParseValue(ref reader))
    {
      var firstElement = document.RootElement[0];

      if (firstElement.ValueKind == JsonValueKind.Array && firstElement[0].ValueKind == JsonValueKind.Number)
      {
        // LineString (2D array of doubles)
        var coordinates = JsonSerializer.Deserialize<List<List<double>>>(document.RootElement.GetRawText(), options);
        if (coordinates != null)
        {
          List<LatLng> gpsData = new List<LatLng>();
          foreach (var point in coordinates)
          {
            gpsData.Add(new LatLng { Lat = point[1], Lng = point[0] });
          }
          if (gpsData.Count > 0)
            gpsDataList.Add(gpsData);
        }
      }
      else if (firstElement.ValueKind == JsonValueKind.Array && firstElement[0].ValueKind == JsonValueKind.Array)
      {
        // MultiLineString (3D array of doubles)
        var coordinates = JsonSerializer.Deserialize<List<List<List<double>>>>(document.RootElement.GetRawText(), options);
        if (coordinates != null)
        {
          foreach (var line in coordinates)
          {
            List<LatLng> gpsData = new List<LatLng>();
            foreach (var point in line)
            {
              gpsData.Add(new LatLng { Lat = point[1], Lng = point[0] });
            }
            if (gpsData.Count > 0)
              gpsDataList.Add(gpsData);
          }
        }
      }
      else
      {
        throw new JsonException("Invalid coordinate structure.");
      }
    }

    return gpsDataList;
  }

  public override void Write(Utf8JsonWriter writer, List<List<LatLng>> value, JsonSerializerOptions options)
  {
    throw new NotImplementedException();
  }
}

public class PidGeoData : GeoDataInterface
{
  private static Dictionary<RouteType, Dictionary<string, GeoData>> geoDataCache;
  private static DateTime lastCacheUpdate = DateTime.MinValue;
  static Dictionary<string, RouteType> routeTypeMap = new Dictionary<string, RouteType>
    {
      { "3", RouteType.Bus },
      { "11", RouteType.Trolleybus },
      { "7", RouteType.Ferry },
      { "1", RouteType.Subway },
      { "2", RouteType.Rail },
      { "0", RouteType.Tram },
    };
  public async Task<Dictionary<RouteType, Dictionary<string, GeoData>>> getData((LatLng min, LatLng max) boundingBox, bool useCache, Config config)
  {
    if (geoDataCache != null && useCache && geoDataCache.Count > 0 && lastCacheUpdate.Day == DateTime.Now.Day)
    {
      return geoDataCache;
    }
    string date = DateTime.Now.ToString("yyyy-MM-dd");
    string filePath = $"{Util.CheckForCacheDir()}/geodata_{date}.json";
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
    int count = jsonResponse.features.Count;
    if(config.hideRegional)
      count = jsonResponse.features.Where(f => f.properties.is_regional != "1").Count();

    Dictionary<RouteType, Dictionary<string, GeoData>> geoData = new Dictionary<RouteType, Dictionary<string, GeoData>>(Enum.GetValues(typeof(RouteType)).Length);
    foreach(var type in Enum.GetValues(typeof(RouteType)).Cast<RouteType>()){
      geoData[type] = new Dictionary<string, GeoData>(jsonResponse.features.Where(// Pre-allocate dictionary for better performance
            feature => routeTypeMap.GetValueOrDefault(feature.properties.route_type, RouteType.Other) == type).Count());
        }

    foreach (var feature in jsonResponse.features)
    {
      if (config.hideRegional && feature.properties.is_regional == "1") // Skip regional routes if config says to not show them
        continue;

      geoData[routeTypeMap.GetValueOrDefault(feature.properties.route_type, RouteType.Other)][feature.properties.route_short_name] = new GeoData
      {
        geometry = feature.geometry.coordinates,
        routeId = feature.properties.route_id,
        routeNameLong = feature.properties.route_long_name,
        routeColor = Util.ParseHexColor(feature.properties.route_color),
        routeUrl = feature.properties.route_url,
        isSubsitute = feature.properties.is_substitute_transport == "1",
        isNightRoute = feature.properties.is_night == "1"
      };
    }
    geoDataCache = geoData;
    lastCacheUpdate = DateTime.Now;
    return geoData;
  }
}
