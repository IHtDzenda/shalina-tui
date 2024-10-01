using System.Runtime.InteropServices;

namespace Core.Util
{
  public class Util
  {
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
      else if (os == OSPlatform.Linux)
      {
        string homepath = Environment.GetEnvironmentVariable("HOME")!;
        cacheDir = homepath + @"/.cache/shalina/" + pathSuffix;
      }
      else if (os == OSPlatform.OSX)
      {
        throw new NotImplementedException("MacOS is not supported yet!");
      }
      if (Directory.Exists(cacheDir))
      {
        return cacheDir;
      }
      Directory.CreateDirectory(cacheDir);
      return cacheDir;


    }
  }

}
