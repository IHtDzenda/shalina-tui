using Core.Api.Maps;
using Mapbox.VectorTile.Geometry;
using SixLabors.ImageSharp;
using Mapbox.VectorTile.Geometry;

namespace Core.Geometry;

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
  public LatLng Center()
  {
    return new LatLng
    {
      Lat = (this.min.Lat + this.max.Lat) / 2,
      Lng = (this.min.Lng + this.max.Lng) / 2
    };
  }

  public double Zoom()
  {
    double lngSpan = this.max.Lng - this.min.Lng % 360;
    double latSpan = this.max.Lat - this.min.Lat % 360;

    // 360 degrees is the width of the world, so we compare it to the current width
    double zoom = Math.Min(Math.Log2(360.0 / lngSpan), Math.Log2(360.0 / latSpan));
    return zoom;
  }

  public static BoundingBox operator +(BoundingBox self, LatLng offset){
    self.min = self.min.Add(offset);
    self.max = self.max.Add(offset);
    return self;
  }
  public static BoundingBox operator -(BoundingBox self, LatLng offset){
    self.min = self.min.Subtract(offset);
    self.max = self.max.Subtract(offset);
    return self;
  }

  public static BoundingBox operator *(BoundingBox self, double factor)
  {
    var center = self.Center();
    var diff = self.max.Subtract(self.min).Multiply(factor / 2);
    return new BoundingBox
    {
      min = center.Subtract(diff),
      max = center.Add(diff)
    };
  }
  public static BoundingBox operator / (BoundingBox self, double factor)
  {
    var center = self.Center();
    var diff = self.max.Subtract(self.min).Divide(factor * 2);
    return new BoundingBox
    {
      min = center.Subtract(diff),
      max = center.Add(diff)
    };
  }
  /// <summary>
  /// Calculates the ratio of the bounding box's width to its height.
  /// The width is defined as the difference in longitude, and the height as the difference in latitude.
  /// If either dimension is zero, an exception is thrown.
  /// </summary>
  /// <returns>
  /// The ratio of width to height.
  /// </returns>
  /// <exception cref="ArgumentException">
  /// Thrown when the bounding box has zero width or height.
  /// </exception>
  public double Ratio()
  {
    double lngSpan = this.max.Lng - this.min.Lng;
    double latSpan = this.max.Lat - this.min.Lat;

    if (lngSpan == 0 || latSpan == 0)
      throw new ArgumentException("BoundingBox has zero width or height.");

    return lngSpan / latSpan;
  }
  public void ExpandToRatio(double ratio)
  {
    double currentRatio = this.Ratio();
    if (currentRatio > ratio)
    {
      // Too wide, adjust height
      double newHeight = (this.max.Lng - this.min.Lng) / ratio;
      double centerLat = (this.max.Lat + this.min.Lat) / 2;
      this.min = new LatLng { Lat = centerLat - newHeight / 2, Lng = this.min.Lng };
      this.max = new LatLng { Lat = centerLat + newHeight / 2, Lng = this.max.Lng };
    }
    else if (currentRatio < ratio)
    {
      // Too tall, adjust width
      double newWidth = (this.max.Lat - this.min.Lat) * ratio;
      double centerLng = (this.max.Lng + this.min.Lng) / 2;
      this.min = new LatLng { Lat = this.min.Lat, Lng = centerLng - newWidth / 2 };
      this.max = new LatLng { Lat = this.max.Lat, Lng = centerLng + newWidth / 2 };
    }
  }
  public void ShrinkToRatio(double ratio)
  {
    double currentRatio = this.Ratio();
    if (currentRatio > ratio)
    {
      // Too wide, adjust height
      double newHeight = (this.max.Lng - this.min.Lng) / ratio;
      double centerLat = (this.max.Lat + this.min.Lat) / 2;
      this.min = new LatLng{Lat = centerLat - newHeight / 2, Lng = this.min.Lng};
      this.max = new LatLng{Lat = centerLat + newHeight / 2, Lng = this.max.Lng};
    }
    else if (currentRatio < ratio)
    {
      // Too tall, adjust width
      double newWidth = (this.max.Lat - this.min.Lat) * ratio;
      double centerLng = (this.max.Lng + this.min.Lng) / 2;
      this.min = new LatLng{Lat = this.min.Lat, Lng = centerLng - newWidth / 2};
      this.max = new LatLng{Lat = this.max.Lat, Lng = centerLng + newWidth / 2};
    }
  }

  public override String ToString()
  {
    return $"({this.min}) - ({this.max})";
  }
}

public static class Conversion
{
  public static PointF ConvertGPSToPixel(LatLng coord, BoundingBox boundingBox, (int width, int height) imageSize)
  {
    // Convert lat/lon to x/y based on the bounding box and image size
    int x = (int)((coord.Lng - boundingBox.min.Lng) / (boundingBox.max.Lng - boundingBox.min.Lng) * imageSize.width);
    int y = imageSize.height - (int)((coord.Lat - boundingBox.min.Lat) / (boundingBox.max.Lat - boundingBox.min.Lat) * imageSize.height); // Invert y-axis for image coordinates
    return new PointF(x, y);
  }
  public static LatLng ConvertTileToGPS(int tileX, int tileY, byte zoom)
  {
    // Calculate the total number of tiles at the given zoom level
    double n = Math.Pow(2, zoom);

    // Calculate longitude
    double lon_deg = tileX / n * 360.0 - 180.0;

    // Calculate latitude
    double lat_rad = Math.Atan(Math.Sinh(Math.PI * (1 - 2 * tileY / n)));
    double lat_deg = lat_rad * (180.0 / Math.PI);

    return new LatLng { Lat = lat_deg, Lng = lon_deg };
  }
  public static (int, int) GetTileFromGPS(LatLng coord, byte zoom)
  {
    int tileX = (int)((coord.Lng + 180.0) / 360.0 * (1 << zoom));
    int tileY = (int)((1.0 - Math.Log(Math.Tan(coord.Lat * Math.PI / 180.0) + 1.0 / Math.Cos(coord.Lat * Math.PI / 180.0)) / Math.PI) / 2.0 * (1 << zoom));

    return (tileX, tileY);
  }
  public static LatLng GetTileCenter(LatLng coord, byte zoom)
  {
    (int tileX, int tileY) = GetTileFromGPS(coord, zoom);

    // Get the longitude and latitude of the tile's corners
    LatLng southwest = ConvertTileToGPS(tileX, tileY + 1, zoom); // Southwest corner
    LatLng northeast = ConvertTileToGPS(tileX + 1, tileY, zoom); // Northeast corner

    // Calculate the center of the tile
    double centerLat = (southwest.Lat + northeast.Lat) / 2.0;
    double centerLng = (southwest.Lng + northeast.Lng) / 2.0;

    return new LatLng { Lat = centerLat, Lng = centerLng };
  }
  public static (int, int)[] GetTiles(LatLng coord, byte zoom, byte radius = 1)
  {
    // Get the center tile coordinates for the given LatLng and zoom level
    (int centerX, int centerY) = GetTileFromGPS(coord, zoom);

    // Calculate the surrounding tiles based on the radius
    List<(int, int)> tiles = new List<(int, int)>();
    for (int x = -radius; x <= radius; x++)
    {
      for (int y = -radius; y <= radius; y++)
      {
        tiles.Add((centerX + x, centerY + y));
      }
    }

    return tiles.ToArray();
  }
}
