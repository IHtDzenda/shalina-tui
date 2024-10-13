using Core.Api.Maps;
using Spectre.Console;
using Core.Api.VectorTiles;
namespace Core.Debug
{
  public class Debug
  {
    public static void PrintTransports()
    {
      PidData pidData = new PidData();
      Transport[] transports = pidData.getData().Result;
      AnsiConsole.WriteLine("Transports:");
      for (int i = 0; i < transports.Length; i++)
      {
        AnsiConsole.Markup($"[green]TripId: {transports[i].tripId} [/]");
        AnsiConsole.Markup($"[red]Line: {transports[i].lineName} [/]");
        AnsiConsole.Markup($"[blue]Delay: {transports[i].delay} [/]");
        AnsiConsole.Markup($"[white]Latitude: {transports[i].lat} [/]");
        AnsiConsole.Markup($"[yellow]Longitude: {transports[i].lon} [/]");
        AnsiConsole.WriteLine();
      }
    }
    public static void PrintVectorTiles(Config cfg)
    {
      string filePath = VectorTiles.GetTile(cfg.latitude, cfg.longitude, 10);
      VectorTiles.PrintTile(VectorTiles.LoadTile(filePath));
      AnsiConsole.WriteLine("Testing VectorTiles");

    }
  }

}
