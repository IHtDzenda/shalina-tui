using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Spectre.Console;
namespace Core
{
  public class ImageProcessing
  {
    static Rgba32? PassThreshold(Rgba32 col)
    {
      if ((col.R > 190 && col.G < 120 && col.B < 120 && col.G > 40 && col.B > 40) || (col.R == 246))
      {
        return SixLabors.ImageSharp.Color.Red;
      }

      else if (col.R < 120 && col.R == col.G && col.G == col.B)
      {
        return SixLabors.ImageSharp.Color.Black;
      }

      else if (col.R < 190 && col.G < 230 & col.B == 255)
      {
        return SixLabors.ImageSharp.Color.SkyBlue;
      }

      else if (col.R == 210 && col.G == 242 & col.B == 215)
      {
        return SixLabors.ImageSharp.Color.PaleGreen;
      }

      return null;
    }
    public static Image Threshold(Image<Rgba32> img, Int16 resolution)
    {
      resolution = (short) (resolution * 1);
      var outImg = new Image<Rgba32>(resolution, resolution, new Rgba32(247, 247, 247, 255));
      for (int x = 0; x < outImg.Width; x++)
      {
        for (int y = 0; y < outImg.Height; y++)
        {

          Rgba32?[] colors = new Rgba32?[(img.Width / outImg.Width) * (img.Height / outImg.Height)];

          for (int testX = 0; testX < (img.Width / outImg.Width); testX++)
          {
            for (int testY = 0; testY < (img.Height / outImg.Height); testY++)
            {
              //outImg[x,y] = img[(int)(x * ((double)img.Width/(double)outImg.Width)), (int)(y* ((double)img.Height/(double)outImg.Height))];
              colors[testX * (img.Height / outImg.Height) + testY] = (PassThreshold(img[(int)(x * ((double)img.Width / (double)outImg.Width)) + testX, (int)(y * ((double)img.Height / (double)outImg.Height)) + testY]));
            }
          }
          try
          {
            var possiblePixelColors = colors.Where(col => col != null)
              .GroupBy(n => n)
              .Where(g => Math.Pow(g.Count(), 2) > (img.Width / outImg.Width) * (img.Height / outImg.Height) )
              .OrderByDescending(g => g.Count())
              .OrderByDescending(g => (g.First() == SixLabors.ImageSharp.Color.SkyBlue || g.First() == SixLabors.ImageSharp.Color.PaleGreen) ? 0 : 1);
            if(possiblePixelColors.Any(g => g.Key == SixLabors.ImageSharp.Color.Red) && possiblePixelColors.Any(g => g.Key == SixLabors.ImageSharp.Color.Black) ){
              outImg[x, y] = SixLabors.ImageSharp.Color.DarkRed;
              continue;
            }
            Rgba32? newPixel = possiblePixelColors.First()?.Key;
            if (newPixel != null)
            {
              outImg[x, y] = newPixel.Value;
            }
          }
          catch
          {
            continue;
          }
        }
      }
      return outImg;
    }
    public static CanvasImage ImageSharpToCanvasImage(Image img)
    {
      using (var stream = new MemoryStream())
      {
        img.Save(stream, new SixLabors.ImageSharp.Formats.Png.PngEncoder());
        stream.Position = 0;

        return new CanvasImage(stream);

      }
    }
  }
}
