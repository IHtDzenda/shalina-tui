using Core.Api.Maps;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Core.Rendering;
public static class Connection
{
  public static void DrawPathOnImage(Image<Rgb24> image, List<GPSData> gpsCoordinates, (GPSData min, GPSData max) boundingBox, Color color)
  {
    // Convert GPS coordinates to pixel coordinates
    Point[] pathPoints = gpsCoordinates
      .Select(coord => Conversion.ConvertGPSToPixel(coord, boundingBox, (image.Width, image.Height)))
      .ToArray();

    // Manually draw pixels for each line segment between consecutive points
    for (int i = 1; i < pathPoints.Length; i++)
    {
      DrawLine(image, pathPoints[i - 1], pathPoints[i], color);
    }
  }
  // Draw a line using Bresenham's line algorithm(https://en.wikipedia.org/wiki/Bresenham%27s_line_algorithm)
  public static void DrawLine(Image<Rgb24> image, Point p1, Point p2, Color color)
  {
    int x0 = p1.X;
    int y0 = p1.Y;
    int x1 = p2.X;
    int y1 = p2.Y;

    int dx = Math.Abs(x1 - x0), sx = x0 < x1 ? 1 : -1;
    int dy = -Math.Abs(y1 - y0), sy = y0 < y1 ? 1 : -1;
    int err = dx + dy, e2; // error value e_xy

    while (true)
    {
      if (x0 >= 0 && x0 < image.Width && y0 >= 0 && y0 < image.Height)
      {
        image[x0, y0] = color; // Set the pixel
      }

      if (x0 == x1 && y0 == y1) break;
      e2 = 2 * err;
      if (e2 >= dy) { err += dy; x0 += sx; } // Move x direction
      if (e2 <= dx) { err += dx; y0 += sy; } // Move y direction
    }
  }
}
