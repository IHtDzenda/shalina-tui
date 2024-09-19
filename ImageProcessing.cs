using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Core.Rendering
{
  public class ImageProcessing
  {
    static Rgb24? PassThreshold(Rgb24 col)
    {
      if ((col.R > 190 && col.G < 120 && col.B < 120 && col.G > 40 && col.B > 40) || (col.R == 246))
      {
        return SixLabors.ImageSharp.Color.Red;
      }

      else if (col.R < 130 && col.R == col.G && col.G == col.B)
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
    public static Image<Rgb24> Threshold(Image<Rgb24> img, Int16 resolution)
    {
      resolution = (short)(resolution * 1);
      var outImg = new Image<Rgb24>(resolution, resolution, new Rgb24(247, 247, 247));
      for (int x = 0; x < outImg.Width; x++)
      {
        for (int y = 0; y < outImg.Height; y++)
        {

          Rgb24?[] colors = new Rgb24?[(img.Width / outImg.Width) * (img.Height / outImg.Height)];

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
              .Where(g => Math.Pow(g.Count(), 2) * 1 > (img.Width / outImg.Width) * (img.Height / outImg.Height))
              .OrderByDescending(g => g.Count())
              .OrderByDescending(g => (g.First() == SixLabors.ImageSharp.Color.SkyBlue || g.First() == SixLabors.ImageSharp.Color.PaleGreen) ? 0 : 1);

            if (possiblePixelColors.Any(g => g.Key == SixLabors.ImageSharp.Color.Red) && possiblePixelColors.Any(g => g.Key == SixLabors.ImageSharp.Color.Black))
            {
              outImg[x, y] = SixLabors.ImageSharp.Color.DarkRed;
              continue;
            }
            Rgb24? newPixel = possiblePixelColors.First()?.Key;
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
  }
}
