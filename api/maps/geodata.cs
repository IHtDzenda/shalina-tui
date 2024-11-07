using System.Text.Json;
using System.Text.Json.Serialization;
using Mapbox.VectorTile.Geometry;
using SixLabors.ImageSharp.PixelFormats;


namespace Core.Api.Maps;
[JsonConverter(typeof(RouteTypeConverter))]
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

public class RouteTypeConverter : JsonConverter<RouteType>
{
  public override RouteType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
  {
    string value = reader.GetString()?.ToLowerInvariant();

    return value switch
    {
      "bus" => RouteType.Bus,
      "tram" => RouteType.Tram,
      "metro" => RouteType.Subway,
      "subway" => RouteType.Subway,
      "rail" => RouteType.Rail,
      "ferry" => RouteType.Ferry,
      "trolleybus" => RouteType.Trolleybus,
      _ => RouteType.Other // Default case for unknown values
    };
  }

  public override void Write(Utf8JsonWriter writer, RouteType value, JsonSerializerOptions options)
  {
    writer.WriteStringValue(value.ToString());
  }
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
  public static bool Equals(this LatLng a, LatLng b)
  {
    return a.Lat == b.Lat && a.Lng == b.Lng;
  }
  public static bool GreaterThan(this LatLng a, LatLng b)
  {
    return a.Lat > b.Lat && a.Lng > b.Lng;
  }
  public static bool LessThan(this LatLng a, LatLng b)
  {
    return a.Lat < b.Lat && a.Lng < b.Lng;
  }
}

public class GeoData
{
  public List<List<LatLng>> geometry { get; set; }
  public string routeId { get; set; }
  public string routeNameLong { get; set; }
  public Rgb24 routeColor { get; set; }
  public string routeUrl { get; set; }
  public bool isSubsitute { get; set; }
  public bool isNightRoute { get; set; }
}
public interface GeoDataInterface
{
  public abstract Task<Dictionary<RouteType, Dictionary<string, GeoData>>> getData((LatLng min, LatLng max) boundingBox, bool useCache, Config config);
}
