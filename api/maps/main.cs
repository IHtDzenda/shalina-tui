using System.Net;
using System.Runtime.InteropServices;

namespace Core.Api.Maps
{
  public class MapsApi
  {
    public MapProviders[] mapProviders = new MapProviders[]
    {
      new MapProviders
      {
        name = MapProviders.MapPropiversName.OpenStreetMap,
        tileUrl = "https://a.tile.openstreetmap.org/",
        urlSufix = "",
        cacheDirSuffix = "osm"
      },
      new MapProviders
      {
        name = MapProviders.MapPropiversName.Thunderforest,
        tileUrl = "https://a.tile.thunderforest.com/transport/",
        urlSufix = "?apikey=6e5478c8a4f54c779f85573c0e399391",//not mine :)
        cacheDirSuffix = "thf"
      },
    };
    public MapProviders currentMapProvider { get; set; }

    public MapsApi(MapProviders.MapPropiversName mapPropiversName)
    {
      currentMapProvider = mapProviders.FirstOrDefault(x => x.name == mapPropiversName) ?? mapProviders.First();
    }
    public string GetTiles(double latitude, double longitude, int zoom)
    {
      int tileX = (int)((longitude + 180.0) / 360.0 * (1 << zoom));
      int tileY = (int)((1.0 - Math.Log(Math.Tan(latitude * Math.PI / 180.0) + 1.0 / Math.Cos(latitude * Math.PI / 180.0)) / Math.PI) / 2.0 * (1 << zoom));
      return CheckForFileCache($"{currentMapProvider.cacheDirSuffix}-{zoom}-{tileX}-{tileY}.png", $"{currentMapProvider.tileUrl}{zoom}/{tileX}/{tileY}.png");
    }
    private string CheckForFileCache(string fileName, string url)
    {
      string cacheDir = CheckForCacheDir();
      string filePath = cacheDir + fileName;
      if (File.Exists(filePath))
      {
        Console.WriteLine($"Using cached file {filePath}");
        return filePath;
      }
      using (WebClient client = new WebClient())
      {
        Console.WriteLine($"Downloading {url}{currentMapProvider.urlSufix} to {filePath}");
        client.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/74.0.3729.169 Safari/537.36");
        client.DownloadFile(url+currentMapProvider.urlSufix, filePath);
      }
      return filePath;
    }

    static OSPlatform GetOs()
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
    static string CheckForCacheDir(string pathSuffix = "")
    {
      OSPlatform os = GetOs();
      string cacheDir = "";
      if (os == OSPlatform.Windows)
      {
        cacheDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\shalina\cache\" + pathSuffix;
      }
      else if (os == OSPlatform.Linux)
      {
        string homepath = Environment.GetEnvironmentVariable("HOME");
        cacheDir = homepath + @"/.cache/shalina/" + pathSuffix;
      }
      else if (os == OSPlatform.OSX)
      {
        throw new Exception("MacOS is not supported yet!");
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
