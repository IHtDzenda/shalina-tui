using System.Net;
using System.Runtime.InteropServices;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Core.Api.Maps
{
  public enum TileDirection
  {
    NorthWest,
    North,
    NorthEast,
    West,
    Center,
    East,
    SouthWest,
    South,
    SouthEast,
  }
  public struct Tile
  {
    public string filePath;
    public string fileUrl;
  }
  public class MapsApi
  {
    public MapProviders[] mapProviders = new MapProviders[]
    {
      new MapProviders
      {
        name = MapProviders.MapPropiversName.OpenStreetMap,
        tileUrl = "https://b.tile.openstreetmap.org/",
        urlSufix = "",
        cacheDirSuffix = "osm"
      },
      new MapProviders
      {
        name = MapProviders.MapPropiversName.Thunderforest,
        tileUrl = "https://a.tile.thunderforest.com/transport-dark/",
        urlSufix = "?apikey=6e5478c8a4f54c779f85573c0e399391",//not mine :)
        cacheDirSuffix = "thf"
      },
    };

    public MapProviders currentMapProvider { get; set; }
    public Image<Rgb24> ConcatImages(Tile[] tiles, byte count = 3)
    {
      const Int16 pixelCount = 256;
      var finalImage = new Image<Rgb24>(pixelCount * count, pixelCount * count);
      for (int i = 0; i < tiles.Length; i++)
      {
        using (Image<Rgb24> img = Image.Load<Rgb24>(tiles[i].filePath))
        {
          int xPos = (i % count) * pixelCount;
          int yPos = (i / count) * pixelCount;

          finalImage.Mutate(ctx => ctx.DrawImage(img, new Point(xPos, yPos), 1f));
        }
      }

      return finalImage;
    }
    public MapsApi(MapProviders.MapPropiversName mapPropiversName)
    {
      currentMapProvider = mapProviders.FirstOrDefault(x => x.name == mapPropiversName) ?? mapProviders.First();
    }
    public Tile[] GetNeighbourTiles(double latitude, double longitude, int zoom)
    {
      int tileX = (int)((longitude + 180.0) / 360.0 * (1 << zoom));
      int tileY = (int)((1.0 - Math.Log(Math.Tan(latitude * Math.PI / 180.0) + 1.0 / Math.Cos(latitude * Math.PI / 180.0)) / Math.PI) / 2.0 * (1 << zoom));
      Tile[] tiles = new Tile[9];
      tiles[(int)TileDirection.Center] = new Tile
      {
        filePath = $"{currentMapProvider.cacheDirSuffix}-{zoom}-{tileX}-{tileY}.png",
        fileUrl = $"{currentMapProvider.tileUrl}{zoom}/{tileX}/{tileY}.png"
      };
      tiles[(int)TileDirection.South] = new Tile
      {
        filePath = $"{currentMapProvider.cacheDirSuffix}-{zoom}-{tileX}-{tileY + 1}.png",
        fileUrl = $"{currentMapProvider.tileUrl}{zoom}/{tileX}/{tileY + 1}.png"
      };
      tiles[(int)TileDirection.North] = new Tile
      {
        filePath = $"{currentMapProvider.cacheDirSuffix}-{zoom}-{tileX}-{tileY - 1}.png",
        fileUrl = $"{currentMapProvider.tileUrl}{zoom}/{tileX}/{tileY - 1}.png"
      };
      tiles[(int)TileDirection.West] = new Tile
      {
        filePath = $"{currentMapProvider.cacheDirSuffix}-{zoom}-{tileX - 1}-{tileY}.png",
        fileUrl = $"{currentMapProvider.tileUrl}{zoom}/{tileX - 1}/{tileY}.png"
      };
      tiles[(int)TileDirection.East] = new Tile
      {
        filePath = $"{currentMapProvider.cacheDirSuffix}-{zoom}-{tileX + 1}-{tileY}.png",
        fileUrl = $"{currentMapProvider.tileUrl}{zoom}/{tileX + 1}/{tileY}.png"
      };
      tiles[(int)TileDirection.NorthWest] = new Tile
      {
        filePath = $"{currentMapProvider.cacheDirSuffix}-{zoom}-{tileX - 1}-{tileY - 1}.png",
        fileUrl = $"{currentMapProvider.tileUrl}{zoom}/{tileX - 1}/{tileY - 1}.png"
      };
      tiles[(int)TileDirection.NorthEast] = new Tile
      {
        filePath = $"{currentMapProvider.cacheDirSuffix}-{zoom}-{tileX + 1}-{tileY - 1}.png",
        fileUrl = $"{currentMapProvider.tileUrl}{zoom}/{tileX + 1}/{tileY - 1}.png"
      };
      tiles[(int)TileDirection.SouthWest] = new Tile
      {
        filePath = $"{currentMapProvider.cacheDirSuffix}-{zoom}-{tileX - 1}-{tileY + 1}.png",
        fileUrl = $"{currentMapProvider.tileUrl}{zoom}/{tileX - 1}/{tileY + 1}.png"
      };
      tiles[(int)TileDirection.SouthEast] = new Tile
      {
        filePath = $"{currentMapProvider.cacheDirSuffix}-{zoom}-{tileX + 1}-{tileY + 1}.png",
        fileUrl = $"{currentMapProvider.tileUrl}{zoom}/{tileX + 1}/{tileY + 1}.png"
      };
      for (int i = 0; i < tiles.Length; i++)
      {
        string path = CheckForFileCache(tiles[i].filePath, tiles[i].fileUrl);
        tiles[i].filePath = path;
      }
      return tiles;
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
        client.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/100.0.4896.127 Safari/537.36");
        client.Headers.Add("Accept-Language", "en-US,en;q=0.9");
        client.Headers.Add("Accept-Encoding", "gzip, deflate, br");
        client.DownloadFile(url + currentMapProvider.urlSufix, filePath);
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
