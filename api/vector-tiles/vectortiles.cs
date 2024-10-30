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
    public static string DownloadTile(int tileX, int tileY, int zoom)
    {
      if (zoom > 14)
      {
        zoom = 14;
      }
      string url = $"https://a.tile.thunderforest.com/thunderforest.transport-v2/{zoom}/{tileX}/{tileY}.vector.pbf?apikey={apiKey}";
      string filePath = Util.CheckForCacheDir() + $"vt-{zoom}-{tileX}-{tileY}.pbf";
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
        if (Util.IsGzip(responseBody))
        {
          responseBody = Util.DecompressGzip(responseBody);
        }
        File.WriteAllBytes(filePath, responseBody);
      }
      return filePath;
    }
    public static VectorTile GetTile(int tileX, int tileY, int zoom)
    {
      string filePath = DownloadTile(tileX, tileY, zoom);
      return LoadTile(filePath);
    }
  }
}
