using Mapbox.VectorTile;

namespace Core.Api.VectorTiles
{
  class VectorTiles
  {
    private const string apiKey = "6e5478c8a4f54c779f85573c0e399391";//not mine :)
    public static void PrintTile(VectorTile vt)
    {
      foreach (var lyrName in vt.LayerNames())
      {
        VectorTileLayer lyr = vt.GetLayer(lyrName);
        for (int i = 0; i < lyr.FeatureCount(); i++)
        {
          Console.WriteLine($"=============");
          Console.WriteLine($"Name: {lyr.Name}");
          VectorTileFeature feat = lyr.GetFeature(i);
          var properties = feat.GetProperties();
          foreach (var prop in properties)
          {
            Console.WriteLine("key:{0} value:{1}", prop.Key, prop.Value);
          }
          foreach (var part in feat.Geometry<int>())
          {
            foreach (var geom in part)
            {
              Console.WriteLine("geom.X:{0} geom.Y:{1}", geom.X, geom.Y);
            }
          }
        }
      }
    }
    public static VectorTile LoadTile(string filePath)
    {
      byte[] data = File.ReadAllBytes(filePath);
      return new VectorTile(data);
    }
    public static string GetTile(double latitude, double longitude, int zoom)
    {
      int tileX = (int)((longitude + 180.0) / 360.0 * (1 << zoom));
      int tileY = (int)((1.0 - Math.Log(Math.Tan(latitude * Math.PI / 180.0) + 1.0 / Math.Cos(latitude * Math.PI / 180.0)) / Math.PI) / 2.0 * (1 << zoom));
      string url = $"https://a.tile.thunderforest.com/thunderforest.transport-v2/{zoom}/{tileX}/{tileY}.vector.pbf?apikey={apiKey}";
      string filePath = Util.Util.CheckForCacheDir() + $"vt-{zoom}-{tileX}-{tileY}.pbf";
      if (File.Exists(filePath))
      {
        Console.WriteLine($"Using cached file {filePath}");
        return filePath;
      }
      using (HttpClient client = new HttpClient())
      {
        Console.WriteLine($"Downloading {url} to {filePath}");
        HttpResponseMessage response = client.GetAsync(url).Result;
        if (!response.IsSuccessStatusCode)
        {
          throw new Exception("Request failed!");
        }
        byte[] responseBody = response.Content.ReadAsByteArrayAsync().Result;
        if (responseBody.Length == 0)
        {
          throw new Exception("Response body is null!, try using a smaller zoom level.");
        }
        if (Util.Util.IsGzip(responseBody))
        {
          responseBody = Util.Util.DecompressGzip(responseBody);
        }
        File.WriteAllBytes(filePath, responseBody);
      }
      return filePath;

    }
  }
}
