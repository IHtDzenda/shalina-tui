using Core.Api.Maps;
using Mapbox.VectorTile.Geometry;
using SixLabors.ImageSharp;

namespace Core.Rendering;

public static class Conversion
{
  public static Point ConvertGPSToPixel(LatLng coord, (LatLng min, LatLng max) boundingBox, (int width, int height) imageSize)
  {
    // Convert lat/lon to x/y based on the bounding box and image size
    int x = (int)((coord.Lng - boundingBox.min.Lng) / (boundingBox.max.Lng - boundingBox.min.Lng) * imageSize.width);
    int y = imageSize.height - (int)((coord.Lat - boundingBox.min.Lat) / (boundingBox.max.Lat - boundingBox.min.Lat) * imageSize.height); // Invert y-axis for image coordinates
    return new Point(x, y);
  }
  public static LatLng ConvertTileToGPS(int tileX, int tileY, byte zoom)
  {
    double n = Math.PI - (2.0 * Math.PI * tileY) / (1 << zoom);
    double centerLatitude = 180.0 / Math.PI * Math.Atan(0.5 * (Math.Exp(n) - Math.Exp(-n)));
    double centerLongitude = tileX / (double)(1 << zoom) * 360.0 - 180.0;

    return new LatLng { Lat = centerLatitude, Lng = centerLongitude };
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
    return ConvertTileToGPS(tileX, tileY, zoom);
  }

  public static (int, int)[] GetTiles(LatLng coord, byte zoom)
  {
    (int centerX, int centerY) = GetTileFromGPS(coord, zoom);
    LatLng center = ConvertTileToGPS(centerX, centerY, zoom);
    int x = center.Lng > coord.Lng ? 1 : -1;
    int y = center.Lat > coord.Lat ? 1 : -1;

    // Calculate the coordinates of the surrounding tiles
    return new (int, int)[]
    {
      (centerX, centerY),
      (centerX + x, centerY),
      (centerX, centerY + y),
      (centerX + x, centerY + y)
    };
  }
  public static (LatLng min, LatLng max) GetBoundingBox(LatLng center, byte zoom)
  {
    (int x, int y)[] images = GetTiles(center, zoom);
    LatLng diff = ConvertTileToGPS(images[0].x, images[0].y, zoom).Subtract(ConvertTileToGPS(images[3].x, images[3].y, zoom));
    LatLng min = center.Subtract(diff.Divide(2));
    LatLng max = center.Add(diff.Divide(2));
    return (min, max);
  }
}
