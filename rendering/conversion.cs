using Core.Api.Maps;
using SixLabors.ImageSharp;

namespace Core.Rendering;

public static class Conversion
{
  public static Point ConvertGPSToPixel(GPSData coord, (GPSData min, GPSData max) boundingBox, (int width, int height) imageSize)
  {
    // Convert lat/lon to x/y based on the bounding box and image size
    int x = (int)((coord.lon - boundingBox.min.lon) / (boundingBox.max.lon - boundingBox.min.lon) * imageSize.width);
    int y = imageSize.height - (int)((coord.lat - boundingBox.min.lat) / (boundingBox.max.lat - boundingBox.min.lat) * imageSize.height); // Invert y-axis for image coordinates
    return new Point(x, y);
  }
  public static GPSData GetImageCenter(int tileX, int tileY, byte zoom)
  {
    double n = Math.PI - (2.0 * Math.PI * tileY) / (1 << zoom);
    double centerLatitude = 180.0 / Math.PI * Math.Atan(0.5 * (Math.Exp(n) - Math.Exp(-n)));
    double centerLongitude = tileX / (double)(1 << zoom) * 360.0 - 180.0;

    return new GPSData(centerLatitude, centerLongitude);
  }
  public static GPSData GetImageCenter(GPSData coord, byte zoom)
  {
    int tileX = (int)((coord.lon + 180.0) / 360.0 * (1 << zoom));
    int tileY = (int)((1.0 - Math.Log(Math.Tan(coord.lat * Math.PI / 180.0) + 1.0 / Math.Cos(coord.lat * Math.PI / 180.0)) / Math.PI) / 2.0 * (1 << zoom));

    // Calculate the center of the tile in degrees
    return GetImageCenter(tileX, tileY, zoom);
  }
  public static GPSData[] GetImages(GPSData coord, byte zoom)
  {
    GPSData center = GetImageCenter(coord, zoom);
    //TODO
  }
}
