using Core.Api.Maps;
using Spectre.Console;
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
    public static void LoadAndPrintConfig()
    {
      Config config = new Config();
      AnsiConsole.WriteLine("Config:");
      AnsiConsole.Markup($"[green]Latitude: {config.latitude} [/]");
      AnsiConsole.Markup($"[red]Longitude: {config.longitude} [/]");
      AnsiConsole.Markup($"[blue]Resolution: {config.resolution} [/]");
      AnsiConsole.Markup($"[white]Zoom: {config.zoom} [/]");
      AnsiConsole.Markup($"[yellow]ColorScheme: [/]");
      AnsiConsole.Markup($"[rgb({config.colorScheme.Water.R},{config.colorScheme.Water.G},{config.colorScheme.Water.B})]Water: {config.colorScheme.Water} [/]");
      AnsiConsole.Markup($"[rgb({config.colorScheme.Land.R},{config.colorScheme.Land.G},{config.colorScheme.Land.B})]Land: {config.colorScheme.Land} [/]");
      AnsiConsole.Markup($"[rgb({config.colorScheme.Grass.R},{config.colorScheme.Grass.G},{config.colorScheme.Grass.B})]Grass: {config.colorScheme.Grass} [/]");
      AnsiConsole.Markup($"[rgb({config.colorScheme.Tram.R},{config.colorScheme.Tram.G},{config.colorScheme.Tram.B})]Tram: {config.colorScheme.Tram} [/]");
      AnsiConsole.WriteLine();
      config.Save();
      config = Config.Load();
      AnsiConsole.Markup($"[green]Latitude: {config.latitude} [/]");
      AnsiConsole.Markup($"[red]Longitude: {config.longitude} [/]");
      AnsiConsole.Markup($"[blue]Resolution: {config.resolution} [/]");
      AnsiConsole.Markup($"[white]Zoom: {config.zoom} [/]");
      AnsiConsole.Markup($"[yellow]ColorScheme: [/]");
      AnsiConsole.Markup($"[rgb({config.colorScheme.Water.R},{config.colorScheme.Water.G},{config.colorScheme.Water.B})]Water: {config.colorScheme.Water} [/]");
      AnsiConsole.Markup($"[rgb({config.colorScheme.Land.R},{config.colorScheme.Land.G},{config.colorScheme.Land.B})]Land: {config.colorScheme.Land} [/]");
      AnsiConsole.Markup($"[rgb({config.colorScheme.Grass.R},{config.colorScheme.Grass.G},{config.colorScheme.Grass.B})]Grass: {config.colorScheme.Grass} [/]");
      AnsiConsole.Markup($"[rgb({config.colorScheme.Tram.R},{config.colorScheme.Tram.G},{config.colorScheme.Tram.B})]Tram: {config.colorScheme.Tram} [/]");
    }
  }
}
