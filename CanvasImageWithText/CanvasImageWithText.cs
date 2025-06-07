using Spectre.Console;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Processors.Transforms;
using Spectre.Console.Rendering;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;


namespace Core.Rendering
{

  public struct CanvasText
  {
    public int x;
    public int y;
    public string text;
    public Rgb24 color;
    public CanvasText(int x, int y, string text, Rgb24 color)
    {
      this.x = x;
      this.y = y;
      this.text = text;
      this.color = color;
    }
  }

  // Modified version of https://github.com/spectreconsole/spectre.console/blob/main/src/Extensions/Spectre.Console.ImageSharp/CanvasImage.cs with text support
  // without the use of Canvas, so there is code from https://github.com/spectreconsole/spectre.console/blob/main/src/Spectre.Console/Widgets/Canvas.cs aswell
  public class CanvasImageWithText : Renderable
  {
    private static readonly IResampler _defaultResampler = KnownResamplers.Hermite;

    public int Width => Image.Width;
    public int Height => Image.Height;
    public int? MaxWidth { get; set; }
    public int PixelWidth { get; set; } = 2;
    public IResampler? Resampler { get; set; }
    internal SixLabors.ImageSharp.Image<Rgb24> Image { get; private set; }
    internal SixLabors.ImageSharp.Image<Rgba32> SubPixels { get; private set; }

    internal Stack<CanvasText>[,] texts;

    public CanvasImageWithText(string filename)
    {
      Image = SixLabors.ImageSharp.Image.Load<Rgb24>(filename);
      SubPixels = new SixLabors.ImageSharp.Image<Rgba32>(this.Width, this.Height, new Rgba32(0, 0, 0, 0));
      this.texts = new Stack<CanvasText>[this.Width, this.Height];
    }
    public CanvasImageWithText(string filename, CanvasText[] texts)
    {
      Image = SixLabors.ImageSharp.Image.Load<Rgb24>(filename);
      SubPixels = new SixLabors.ImageSharp.Image<Rgba32>(this.Width, this.Height, new Rgba32(0, 0, 0, 0));
      this.texts = new Stack<CanvasText>[this.Width, this.Height];
      foreach (CanvasText text in texts)
      {
        AddText(text);
      }
    }

    public CanvasImageWithText(ReadOnlySpan<byte> data)
    {
      Image = SixLabors.ImageSharp.Image.Load<Rgb24>(data);
      SubPixels = new SixLabors.ImageSharp.Image<Rgba32>(this.Width, this.Height, new Rgba32(0, 0, 0, 0));
      this.texts = new Stack<CanvasText>[this.Width, this.Height];
    }

    public CanvasImageWithText(Stream data)
    {
      Image = SixLabors.ImageSharp.Image.Load<Rgb24>(data);
      SubPixels = new SixLabors.ImageSharp.Image<Rgba32>(this.Width, this.Height, new Rgba32(0, 0, 0, 0));
      this.texts = new Stack<CanvasText>[this.Width, this.Height];
    }
    public CanvasImageWithText(SixLabors.ImageSharp.Image<Rgb24> image)
    {
      Image = image;
      SubPixels = new SixLabors.ImageSharp.Image<Rgba32>(this.Width, this.Height, new Rgba32(0, 0, 0, 0));
      this.texts = new Stack<CanvasText>[this.Width, this.Height];
    }
    public CanvasImageWithText(SixLabors.ImageSharp.Image<Rgb24> image, CanvasText[] texts)
    {
      Image = image;
      SubPixels = new SixLabors.ImageSharp.Image<Rgba32>(this.Width, this.Height, new Rgba32(0, 0, 0, 0));
      this.texts = new Stack<CanvasText>[this.Width, this.Height];
      foreach (CanvasText text in texts)
      {
        this.AddText(text);
      }
    }

    public CanvasImageWithText AddText(CanvasText text)
    {
      if (text.x < 0 || text.y < 0 || text.x >= this.Width || text.y >= this.Height)
        return this;

      if (this.texts[text.x, text.y] == null)
        this.texts[text.x, text.y] = new Stack<CanvasText>();

      this.texts[text.x, text.y].Push(text);
      return this;
    }
    public void ClearTexts()
    {
      this.texts = new Stack<CanvasText>[this.Width, this.Height];
    }

    public void AddSubPixel(int x, int y, Rgb24 color)
    {
      if (x < 0 || y < 0 || x >= this.Width || y >= this.Height)
        throw new ArgumentOutOfRangeException("Sub-pixel coordinates are out of bounds.");

      SubPixels[x, y] = new Rgba32(color.R, color.G, color.B, 255);
    }

    public void ClearSubPixels()
    {
      SubPixels = new SixLabors.ImageSharp.Image<Rgba32>(this.Width, this.Height, new Rgba32(0, 0, 0, 0));
    }
    public void Clear(Rgb24 color)
    {
      Image.Mutate(i => i.Clear(color));
      SubPixels.Mutate(i => i.Clear(new Rgba32(0, 0, 0, 0)));
      ClearTexts();
    }
    
    // https://en.wikipedia.org/wiki/Bresenham%27s_line_algorithm
    public void AddLine((int x, int y) start, (int x, int y) end, Rgb24 color)
    {
      int x0 = start.x;
      int y0 = start.y;
      int x1 = end.x;
      int y1 = end.y;

      int dx = Math.Abs(x1 - x0), sx = x0 < x1 ? 1 : -1;
      int dy = -Math.Abs(y1 - y0), sy = y0 < y1 ? 1 : -1;
      int err = dx + dy, e2; // error value e_xy
      while (true)
      {
        if (x0 >= 0 && x0 < this.Image.Width && y0 >= 0 && y0 < this.Image.Height)
        {
          this.Image[x0, y0] = color; // Set the pixel
        }

        if (x0 == x1 && y0 == y1) break;
        e2 = 2 * err;
        if (e2 >= dy) { err += dy; x0 += sx; } // Move x direction
        if (e2 <= dx) { err += dx; y0 += sy; } // Move y direction
      }
    }
    public void AddLine(PointF[] points, Rgb24 color)
    {
      if (points.Length < 2)
        throw new ArgumentException("At least two points are required to draw a line.");

      for (int i = 0; i < points.Length - 1; i++)
      {
        AddLine(
          ((int)points[i].X, (int)points[i].Y),
          ((int)points[i + 1].X, (int)points[i + 1].Y),
          color
        );
      }
    }
    public void AddSubPixelLine((int x, int y) start, (int x, int y) end, Rgb24 color)
    {
      int x0 = start.x;
      int y0 = start.y;
      int x1 = end.x;
      int y1 = end.y;

      int dx = Math.Abs(x1 - x0), sx = x0 < x1 ? 1 : -1;
      int dy = -Math.Abs(y1 - y0), sy = y0 < y1 ? 1 : -1;
      int err = dx + dy, e2; // error value e_xy
      while (true)
      {
        if (x0 >= 0 && x0 < this.Image.Width && y0 >= 0 && y0 < this.Image.Height)
        {
          this.SubPixels[x0, y0] = new Rgba32(color.R, color.G, color.B, 255); // Set the sub-pixel
        }

        if (x0 == x1 && y0 == y1) break;
        e2 = 2 * err;
        if (e2 >= dy) { err += dy; x0 += sx; } // Move x direction
        if (e2 <= dx) { err += dx; y0 += sy; } // Move y direction
      }
    }
    public void AddSubPixelLine(PointF[] points, Rgb24 color)
    {
      if (points.Length < 2)
        throw new ArgumentException("At least two points are required to draw a line.");

      for (int i = 0; i < points.Length - 1; i++)
      {
        AddSubPixelLine(
          ((int)points[i].X, (int)points[i].Y),
          ((int)points[i + 1].X, (int)points[i + 1].Y),
          color
        );
      }
    }


    protected override Measurement Measure(RenderOptions options, int maxWidth)
    {
      if (PixelWidth < 0)
        throw new InvalidOperationException("Pixel width must be greater than zero.");

      var width = MaxWidth ?? Width;
      if (maxWidth < width * PixelWidth)
        return new Measurement(maxWidth, maxWidth);

      return new Measurement(width * PixelWidth, width * PixelWidth);
    }

    /// <inheritdoc/>
    protected override IEnumerable<Segment> Render(RenderOptions options, int maxWidth)
    {
      var image = Image;

      var width = Width;

      var height = Height;

      // Got a max width?
      if (MaxWidth != null)
      {
        height = (int)(height * ((float)MaxWidth.Value) / Width);
        width = MaxWidth.Value;
      }

      // Exceed the max width when we take pixel width into account?
      if (width * PixelWidth > maxWidth)
      {
        height = (int)(height * (maxWidth / (float)(width * PixelWidth)));
        width = maxWidth / PixelWidth;
      }

      // Need to rescale the pixel buffer?
      if (width != Width || height != Height)
      {
        var resampler = Resampler ?? _defaultResampler;
        image = image.Clone(); // Clone the original image
        image.Mutate(i => i.Resize(width, height, resampler));
      }

      // Render the image
      for (var y = 0; y < height; y++)
      {
        Stack<CanvasText> currentText = new Stack<CanvasText>();
        for (var x = 0; x < width; x++)
        {
          var color = image[x, y];

          if (currentText.Count > 0 && x > currentText.Peek().x + (currentText.Peek().text.Length - 1) / this.PixelWidth)
          {
            currentText.Pop();
          }
          if (this.texts[x, y] != null)
          {
            foreach (CanvasText text in this.texts[x, y])
            {
              currentText.Push(text);
            }
          }

          string pixel = "";
          if (currentText.Count > 0)
          {
            for (byte i = 0; i < this.PixelWidth; i++)
            {
              if ((x - currentText.Peek().x) * this.PixelWidth + i >= currentText.Peek().text.Length)
              {
                break;
              }
              pixel += currentText.Peek().text[(x - currentText.Peek().x) * this.PixelWidth + i];
            }
          }
          pixel = pixel.PadRight(PixelWidth, ' ');
          if (SubPixels[x, y] != new Rgba32(0, 0, 0, 0))
          {
            for (int i = 0; i < PixelWidth; i++)
            {
              if (i % 2 == 0)
                yield return new Segment(pixel[i].ToString(), new Style(background: new Spectre.Console.Color(SubPixels[x, y].R, SubPixels[x, y].G, SubPixels[x, y].B), foreground: currentText.Count > 0 ? new Spectre.Console.Color(currentText.Peek().color.R, currentText.Peek().color.G, currentText.Peek().color.B) : Spectre.Console.Color.Default));
              else
                yield return new Segment(pixel[i].ToString(), new Style(background: new Spectre.Console.Color(color.R, color.G, color.B), foreground: currentText.Count > 0 ? new Spectre.Console.Color(currentText.Peek().color.R, currentText.Peek().color.G, currentText.Peek().color.B) : Spectre.Console.Color.Default));
            }
          }
          else
          {
            yield return new Segment(pixel, new Style(background: new Spectre.Console.Color(color.R, color.G, color.B), foreground: currentText.Count > 0 ? new Spectre.Console.Color(currentText.Peek().color.R, currentText.Peek().color.G, currentText.Peek().color.B) : Spectre.Console.Color.Default));
          }
        }

        yield return Segment.LineBreak;
      }
    }
  }
}
