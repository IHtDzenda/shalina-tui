using Core.Api.Maps;
using Core.Geometry;
using Mapbox.VectorTile.Geometry;

namespace Core.Location;

public class FindLine : LocationGetter
{
  private static GeoData? FindLineInCity(GeoDataProvider provider, Config config)
  {
    var data = provider.GetGeoDataAsync(new BoundingBox
    {
      min = new LatLng { Lat = 48, Lng = 12 },
      max = new LatLng { Lat = 52, Lng = 16 },
    },
  config).Result;

    foreach(var type in data.Values){
      foreach(var transportLine in type){
        if(transportLine.Key == config.userQuery)
          return transportLine.Value;
      }
    }

    return null;

  }
  protected override Task<Location?> InternalGetLocationAsync(Config config)
  {
    using (HttpClient client = new HttpClient())
    {
      var line = config.cityGeoData.Select(
        city => FindLineInCity(city, config)
      ).FirstOrDefault();

      return Task.FromResult<Location>(line != null ? new Location
      {
        latLng = line.boundingBox.Center(),
        boundingBox = line.boundingBox,
        rotation = 0,
        speed = null,
        altitude = null
      } : null);
    }
  }
}
