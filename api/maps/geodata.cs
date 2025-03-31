using System.Text.Json;
using System.Text.Json.Serialization;
using Core.Rendering;
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
  CableCar,
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
public struct BoundingBox
{
  public LatLng min { get; set; }
  public LatLng max { get; set; }
  public BoundingBox(List<LatLng> points)
  {
    var lngSorted = points.Select(i => i.Lng).Order();
    var latSorted = points.Select(i => i.Lat).Order();
    this.min = new LatLng { Lat = latSorted.First(), Lng = lngSorted.First() };
    this.max = new LatLng { Lat = latSorted.Last(), Lng = lngSorted.Last() };
  }
  public BoundingBox(LatLng center, byte zoom, (int width, int height) imageSize)
  {
    // Get the tile coordinates for the center point
    (int tileX, int tileY) = Conversion.GetTileFromGPS(center, zoom);

    // Calculate the southwest and northeast corners of the bounding box
    LatLng southwest = Conversion.ConvertTileToGPS(tileX, tileY + 1, zoom); // Bottom-left corner
    LatLng northeast = Conversion.ConvertTileToGPS(tileX + 1, tileY, zoom); // Top-right corner

    LatLng diff = northeast.Subtract(southwest);
    if (imageSize.width > imageSize.height)
      diff.Lat = diff.Lat / ((double)imageSize.width / imageSize.height);
    else
      diff.Lng = diff.Lng / ((double)imageSize.height / imageSize.width);

    // Return the minimum and maximum LatLng
    this.min = center.Subtract(diff.Divide(2));
    this.max = center.Add(diff.Divide(2));
  }
  public bool Contains(LatLng other)
  {
    return min.LessThan(other) && max.GreaterThan(other);
  }
  public bool Overlaps(BoundingBox other)
  {
            return this.min.Lng < other.max.Lng && this.max.Lng > other.min.Lng &&
               this.min.Lat < other.max.Lat && this.max.Lat > other.min.Lat;
  }
  public override String ToString(){
    return $"({this.min}) - ({this.max})";
  }
}

public class GeoData
{
  public List<List<LatLng>> geometry { get; }
  public BoundingBox boundingBox { get; }
  public string routeId { get; }
  public string routeNameLong { get; }
  public Rgb24 routeColor { get; }
  public string routeUrl { get; }
  public bool isSubsitute { get; }
  public bool isNightRoute { get; }

  public GeoData(Config config, List<List<LatLng>> _geometry, string _routeId, string _routeNameLong, RouteType _routeType, string _routeUrl, bool _isSubstitute, bool _isNightRoute, Rgb24? customColor = null)
  {
    this.geometry = _geometry;
    this.boundingBox = new BoundingBox(_geometry.SelectMany(i => i).ToList());
    this.routeId = _routeId;
    this.routeNameLong = _routeNameLong;
    if (customColor.HasValue)
      this.routeColor = customColor.Value;
    else
      this.routeColor = config.colorScheme[_routeType.ToString().ToLower()];

    this.routeUrl = _routeUrl;
    this.isSubsitute = _isSubstitute;
    this.isNightRoute = _isNightRoute;
  }
}

public interface GeoDataInterface
{
  public abstract Task<Dictionary<RouteType, Dictionary<string, GeoData>>> getData(BoundingBox boundingBox, bool useCache, Config config);
}
