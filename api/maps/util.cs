using System.Runtime.InteropServices;
using System.IO.Compression;
using SixLabors.ImageSharp.PixelFormats;

namespace Core
{
  public class Util
  {
    public static bool IsGzip(byte[] data)
    {
      return data.Length > 2 && data[0] == 0x1f && data[1] == 0x8b;
    }
    public static byte[] DecompressGzip(byte[] gzipData)
    {
      using (var compressedStream = new MemoryStream(gzipData))
      using (var gzipStream = new GZipStream(compressedStream, CompressionMode.Decompress))
      using (var outputStream = new MemoryStream())
      {
        gzipStream.CopyTo(outputStream);
        return outputStream.ToArray();
      }
    }
    public static OSPlatform GetOs()
    {
      if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
      {
        return OSPlatform.OSX;
      }

      if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
      {
        return OSPlatform.Linux;
      }

      if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
      {
        return OSPlatform.Windows;
      }
      throw new Exception("Cannot determine operating system!");
    }
    public static string CheckForCacheDir(string pathSuffix = "")
    {
      OSPlatform os = GetOs();
      string cacheDir = "";
      if (os == OSPlatform.Windows)
      {
        cacheDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\shalina\cache\" + pathSuffix;
      }
      else if (os == OSPlatform.Linux||os==OSPlatform.OSX)
      {
        string homepath = Environment.GetEnvironmentVariable("HOME")!;
        cacheDir = homepath + @"/.cache/shalina/" + pathSuffix;
      }
      if (Directory.Exists(cacheDir))
      {
        return cacheDir;
      }
      Directory.CreateDirectory(cacheDir);
      return cacheDir;
    }
    public static string GetConfigPath()
    {
      OSPlatform os = GetOs();
      string configPath = "";
      if (os == OSPlatform.Windows)
      {
        configPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\shalina\config.json";
      }
      else if (os == OSPlatform.Linux || os == OSPlatform.OSX)
      {
        string homepath = Environment.GetEnvironmentVariable("HOME")!;
        configPath = homepath + @"/.config/shalina.json";
      }
      return configPath;
    }
    public static Rgb24 ParseHexColor(string hex)
    {
      if (hex == null || hex.Length != 6 || !hex.Substring(1).All(c => "0123456789ABCDEF".Contains(c)))
      {
        throw new ArgumentException("Invalid hex color format.");
      }
      return new Rgb24(
        byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber),
        byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber),
        byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber)
      );
    }
  }
}
