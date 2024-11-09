using Core.Api.Maps;
using Mapbox.VectorTile.Geometry;
using SixLabors.ImageSharp;

namespace Core.Rendering;

public static class Conversion
{
  public static PointF ConvertGPSToPixel(LatLng coord, (LatLng min, LatLng max) boundingBox, (int width, int height) imageSize)
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
  public static (int, int)[] GetTiles(LatLng coord, byte zoom)
  {
    LatLng center = GetTileCenter(coord, zoom);
    int x = center.Lng > coord.Lng ? -1 : 1;
    int y = center.Lat > coord.Lat ? 1 : -1;

    (int centerX, int centerY) = GetTileFromGPS(center, zoom);
    // Calculate the coordinates of the surrounding tiles
    return new (int, int)[]
    {
      (centerX, centerY),
      (centerX + x, centerY),
      (centerX, centerY + y),
      (centerX + x, centerY + y)
    };
  }
  public static (LatLng min, LatLng max) GetBoundingBox(LatLng center, byte zoom, (int width, int height) imageSize)
  {
    // Get the tile coordinates for the center point
    (int tileX, int tileY) = GetTileFromGPS(center, zoom);

    // Calculate the southwest and northeast corners of the bounding box
    LatLng southwest = ConvertTileToGPS(tileX, tileY + 1, zoom); // Bottom-left corner
    LatLng northeast = ConvertTileToGPS(tileX + 1, tileY, zoom); // Top-right corner

    LatLng diff = northeast.Subtract(southwest);
    if(imageSize.width > imageSize.height)
      diff.Lat = diff.Lat * (double)imageSize.width / imageSize.height;
    else
      diff.Lng = diff.Lng * (double)imageSize.height / imageSize.width;

    // Return the minimum and maximum LatLng
    return (center.Subtract(diff.Divide(2)), center.Add(diff.Divide(2)));
  }
}
