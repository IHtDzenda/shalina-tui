using Spectre.Console;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Processors.Transforms;
using Spectre.Console.Rendering;


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
    internal SixLabors.ImageSharp.Image<Rgb24> Image { get; }

    internal CanvasText?[,] texts;

    public CanvasImageWithText(string filename)
    {
      Image = SixLabors.ImageSharp.Image.Load<Rgb24>(filename);
      this.texts = new CanvasText?[this.Width, this.Height];
    }
    public CanvasImageWithText(string filename, CanvasText[] texts)
    {
      Image = SixLabors.ImageSharp.Image.Load<Rgb24>(filename);
      this.texts = new CanvasText?[this.Width, this.Height];
      foreach (CanvasText text in texts)
      {
        AddText(text);
      }
    }

    public CanvasImageWithText(ReadOnlySpan<byte> data)
    {
      Image = SixLabors.ImageSharp.Image.Load<Rgb24>(data);
      this.texts = new CanvasText?[this.Width, this.Height];
    }

    public CanvasImageWithText(Stream data)
    {
      Image = SixLabors.ImageSharp.Image.Load<Rgb24>(data);
      this.texts = new CanvasText?[this.Width, this.Height];
    }
    public CanvasImageWithText(SixLabors.ImageSharp.Image<Rgb24> image)
    {
      Image = image;
      this.texts = new CanvasText?[this.Width, this.Height];
    }
    public CanvasImageWithText(SixLabors.ImageSharp.Image<Rgb24> image, CanvasText[] texts)
    {
      Image = image;
      this.texts = new CanvasText?[this.Width, this.Height];
      foreach (CanvasText text in texts)
      {
        this.AddText(text);
      }
    }

    public CanvasImageWithText AddText(CanvasText text)
    {
      if(text.x < 0 || text.y < 0 || text.x >= this.Width || text.y >= this.Height)
      {
        return this;
      }
      this.texts[text.x, text.y] = text;
      return this;
    }
    public void ClearTexts()
    {
      this.texts = new CanvasText?[this.Width, this.Height];
    }


    protected override Measurement Measure(RenderOptions options, int maxWidth)
    {
      if (PixelWidth < 0)
      {
        throw new InvalidOperationException("Pixel width must be greater than zero.");
      }

      var width = MaxWidth ?? Width;
      if (maxWidth < width * PixelWidth)
      {
        return new Measurement(maxWidth, maxWidth);
      }

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
        CanvasText? currentText = null;
        for (var x = 0; x < width; x++)
        {
          var color = image[x, y];

          if(currentText != null && x > currentText.Value.x + (currentText.Value.text.Length - 1) / this.PixelWidth)
          {
            currentText = null;
          }
          if (this.texts[x, y] != null)
          {
            currentText = this.texts[x, y];
          }

          string pixel = "";
          if (currentText != null)
          {
            for (byte i = 0; i < this.PixelWidth; i++)
            {
              if ((x - currentText.Value.x) * this.PixelWidth + i >= currentText.Value.text.Length)
              {
                break;
              }
              pixel += currentText.Value.text[(x - currentText.Value.x ) * this.PixelWidth  + i];
            }
          }
          pixel = pixel.PadRight(PixelWidth, ' ');
          yield return new Segment(pixel, new Style(background: new Color(color.R, color.G, color.B), foreground: new Color(currentText?.color.R ?? 255, currentText?.color.G ?? 255, currentText?.color.B ?? 255)));
        }

        yield return Segment.LineBreak;
      }
    }
  }
}
