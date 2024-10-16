using Mapbox.VectorTile;

namespace Core.Api.VectorTiles
{
  class VectorTiles
  {
    private const string apiKey = "6e5478c8a4f54c779f85573c0e399391";//not mine :)
    public static VectorTile LoadTile(string filePath)
    {
      byte[] data = File.ReadAllBytes(filePath);
      return new VectorTile(data);
    }
    public static string GetTile(double latitude, double longitude, int zoom)
    {
      //url is limited to 15 zoom levels
      if (zoom >= 15)
      {
        zoom = 14;
      }
      int tileX = (int)((longitude + 180.0) / 360.0 * (1 << zoom));
      int tileY = (int)((1.0 - Math.Log(Math.Tan(latitude * Math.PI / 180.0) + 1.0 / Math.Cos(latitude * Math.PI / 180.0)) / Math.PI) / 2.0 * (1 << zoom));
      string url = $"https://a.tile.thunderforest.com/thunderforest.transport-v2/{zoom}/{tileX}/{tileY}.vector.pbf?apikey={apiKey}";
      string filePath = Util.Util.CheckForCacheDir() + $"vt-{zoom}-{tileX}-{tileY}.pbf";
      if (File.Exists(filePath))
      {
        return filePath;
      }
      using (HttpClient client = new HttpClient())
      {
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
