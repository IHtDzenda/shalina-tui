using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
namespace Core
{
  public class ImageProcessing
  {
    static bool PassThreshold(Rgba32 col)
    {
      if (col.R > 200 && col.G < 200 && col.B < 200)
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
          //outImg[x,y] = img[(int)(x * ((double)img.Width/(double)outImg.Width)), (int)(y* ((double)img.Height/(double)outImg.Height))];
          if (PassThreshold(img[(int)(x * ((double)img.Width/(double)outImg.Width)), (int)(y* ((double)img.Height/(double)outImg.Height))]))
          {
            outImg[x, y] = Color.Black;
          }
        }
      }
      return outImg;
    }
  }
}
