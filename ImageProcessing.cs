using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Core.Rendering
{
  public struct ColorScheme
  {
    public Rgb24 Water;
    public Rgb24 Land;
    public Rgb24 Grass;
    public Rgb24 Trams;
    public Rgb24 Buses;
  }
  public static class ImageProcessing
  {
    private static void PixelIterBox(Image<Rgb24> inputImg, Func<Rgb24, bool>[] predicates, Rgb24 color, Image<Rgb24> outImg)
    {
      for (int y = 0; y < outImg.Height; y++)
      {
        for (int x = 0; x < (outImg.Width / 2); x++)
        {
          int count = 0;
          for (int testX = 0; testX < (inputImg.Width / outImg.Width); testX++)
          {
            for (int testY = 0; testY < (inputImg.Height / outImg.Height); testY++)
            {
              Rgb24 col = inputImg[(int)(x * 2 * ((double)inputImg.Width / (double)outImg.Width)) + testX, (int)(y * ((double)inputImg.Height / (double)outImg.Height)) + testY];
              foreach (var predicate in predicates)
              {
                if (predicate(col))
                {
                  count++;
                  break;
                }
              }
            }
          }
          //if (Math.Pow(count, 2) > (inputImg.Width / outImg.Width) * (inputImg.Height / outImg.Height))
          if (count > 1)
          {
            outImg[x * 2, y] = color;
            outImg[x * 2 + 1, y] = color;
          }
        }
      }
    }
    private static void DualPixelIterBox(Image<Rgb24> inputImg, Func<Rgb24, bool>[] predicates, Rgb24 mergeColor, Rgb24 color, Image<Rgb24> outImg)
    {
      for (int y = 0; y < outImg.Height; y++)
      {
        for (int x = 0; x < (outImg.Width / 2); x++)
        {
          int count = 0;
          for (int testX = 0; testX < (inputImg.Width / outImg.Width); testX++)
          {
            for (int testY = 0; testY < (inputImg.Height / outImg.Height); testY++)
            {
              Rgb24 col = inputImg[(int)(x * 2 * ((double)inputImg.Width / (double)outImg.Width)) + testX, (int)(y * ((double)inputImg.Height / (double)outImg.Height)) + testY];
              foreach (var predicate in predicates)
              {
                if (predicate(col))
                {
                  count++;
                  break;
                }
              }
            }
          }
          //if (Math.Pow(count, 2) > (inputImg.Width / outImg.Width) * (inputImg.Height / outImg.Height))
          if (count > 1)
          {
            if (outImg[2 * x, y] != mergeColor)
            {
              outImg[x * 2, y] = color;
            }
            outImg[x * 2 + 1, y] = color;
          }
        }
      }
    }
    private static Func<Rgb24, bool> ExactMatch(int red, int green, int blue)
    {
      return (Rgb24 col) => (red == -1 || col.R == red) && (green == -1 || col.G == green) && (blue == -1 || col.B == blue);
    }
    private static Func<Rgb24, bool> RatioMatch(double red, double green, double blue, double exactness)
    {
      return (Rgb24 col) =>
      {
        double actualRed = ((double)col.R) / red;
        double actualGreen = ((double)col.G) / green;
        double actualBlue = ((double)col.B) / blue;

        return (red == -1 || green == -1 || (Math.Abs(actualRed - actualGreen) < exactness)) &&
                (red == -1 || blue == -1 || (Math.Abs(actualRed - actualBlue) < exactness)) &&
                (green == -1 || blue == -1 || (Math.Abs(actualGreen - actualBlue) < exactness));
      };
    }

    private static void AddWater(Image<Rgb24> inputImg, Config cfg, Image<Rgb24> outImg)
    {
      Func<Rgb24, bool>[] predicates = [
        ExactMatch(63, 68, 142)
      ];
      PixelIterBox(inputImg, predicates, cfg.colorScheme.Water, outImg);
    }
    private static void AddGrass(Image<Rgb24> inputImg, Config cfg, Image<Rgb24> outImg)
    {
      Func<Rgb24, bool>[] predicates = [
        ExactMatch(45, 97, 66)
      ];
      PixelIterBox(inputImg, predicates, cfg.colorScheme.Grass, outImg);
    }
    private static void AddTrams(Image<Rgb24> inputImg, Config cfg, Image<Rgb24> outImg)
    {
      Func<Rgb24, bool>[] predicates = [
         (Rgb24 col) => ((RatioMatch(1.1, 1.0, -1, 44)(col) && col.B < 80 && col.R > 100) && !ExactMatch(200, 182, 59)(col)) || ExactMatch(74, 70, 38)(col)
      ];
      PixelIterBox(inputImg, predicates, cfg.colorScheme.Trams, outImg);
    }
    private static void AddBuses(Image<Rgb24> inputImg, Config cfg, Image<Rgb24> outImg)
    {
      Func<Rgb24, bool>[] predicates = [
         RatioMatch(4.9,1, 1.46, 2.2)
      ];
      DualPixelIterBox(inputImg, predicates, cfg.colorScheme.Trams, cfg.colorScheme.Buses, outImg);
    }
    private static Action<Image<Rgb24>, Config, Image<Rgb24>>[] Layers = [AddGrass, AddWater, AddTrams, AddBuses];

    public static Image<Rgb24> RunLayers(Image<Rgb24> img, Config cfg)
    {
      var outImg = new Image<Rgb24>(cfg.resolution * 2, cfg.resolution, cfg.colorScheme.Land);
      foreach (var layer in Layers)
      {
        layer(img, cfg, outImg);
      }
      return outImg;
    }
  }
}
