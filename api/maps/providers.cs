namespace Core.Api.Maps
{
  public class MapProviders
  {
    public enum MapPropiversName
    {
      OpenStreetMap,
      Thunderforest
    }
    public MapPropiversName name { get; set; }
    public string tileUrl { get; set; }
    public string cacheDirSuffix { get; set; }
    public string urlSufix { get; set; }
  }
}
