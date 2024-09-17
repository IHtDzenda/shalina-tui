using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Spectre.Console;
namespace Core
{
  public class ImageProcessing
  {
    static bool PassThreshold(Rgba32 col)
    {
      if ((col.R > 215 && col.G < 190 && col.B < 190) || col.R < 120 && col.G < 120 && col.B < 120)
      {
        return true;
      }
      return false;
    }
    public static Image Threshold(Image<Rgba32> img, Int16 resolution)
    {
      var outImg = new Image<Rgba32>(resolution, resolution, new Rgba32(255, 255, 255, 255));
      for (int x = 0; x < outImg.Width; x++)
      {
        for (int y = 0; y < outImg.Height; y++)
        {
          Int16 count = 0;
          for (int testX = 0; testX < (img.Width / outImg.Width); testX++)
          {
            for (int testY = 0; testY < (img.Height / outImg.Height); testY++)
            {
              //outImg[x,y] = img[(int)(x * ((double)img.Width/(double)outImg.Width)), (int)(y* ((double)img.Height/(double)outImg.Height))];
              if (PassThreshold(img[(int)(x * ((double)img.Width / (double)outImg.Width)) + testX, (int)(y * ((double)img.Height / (double)outImg.Height)) + testY]))
              {
                count++;
              }
            }
          }
          if( count > img.Height / outImg.Height)
                outImg[x, y] = SixLabors.ImageSharp.Color.Black;
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
