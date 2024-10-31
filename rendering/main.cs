using Core.Api.Maps;
using Mapbox.VectorTile.Geometry;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;

namespace Core.Rendering;

public static class Renderer
{
  static DrawingOptions drawingOptions = new DrawingOptions
  {
    GraphicsOptions = new GraphicsOptions
    {
      Antialias = false // Disable anti-aliasing
    }
  };

}

