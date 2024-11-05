using Spectre.Console;
using Core.Rendering;
using Core;
using System.Diagnostics;

public class Program
{
  static bool rerender = false;
  static CancellationTokenSource cts = new CancellationTokenSource();
  static Config config = Config.Load();

  static async Task Main(string[] args)
  {
    // Start the map rendering in a background task
    var renderTask = Task.Run(() => RenderLoop(cts.Token));

    // Start listening for key inputs in the main thread
    while (true)
    {
      if (Console.KeyAvailable)
      {
        HandleKeyPress(Console.ReadKey(intercept: true)); // Read the key without displaying it
        rerender = true; // Trigger immediate rerender
      }
      await Task.Delay(10);
    }
  }
  static void HandleKeyPress(ConsoleKeyInfo key)
  {
    switch (key.Key)
    {
      case ConsoleKey.UpArrow:
        config.latitude += 0.001;  // Increase some value in the config
        break;
      case ConsoleKey.DownArrow:
        config.latitude -= 0.001;  // Decrease some value in the config
        break;
      case ConsoleKey.LeftArrow:
        config.longitude -= 0.001;  // Adjust another config value
        break;
      case ConsoleKey.RightArrow:
        config.longitude += 0.001;  // Adjust another config value
        break;
      case ConsoleKey.Escape:
        cts.Cancel();  // Stop the rendering loop
        Environment.Exit(0);  // Exit the program
        break;
      default:
        if (key.KeyChar == '-')
        {
          config.zoom--;  // Zoom out
        }
        else if (key.KeyChar == '+')
        {
          config.zoom++;  // Zoom in
        }
        break;
    }
  }

  static void RenderLoop(CancellationToken token)
  {
    while (!token.IsCancellationRequested)
    {
      Stopwatch stopwatch = new Stopwatch();
      stopwatch.Start();

      // Load configuration and render the map
      AnsiConsole.Write(Renderer.RenderMap(config, true));

      stopwatch.Stop();
      AnsiConsole.WriteLine($"Time elapsed: {stopwatch.ElapsedMilliseconds}ms");
      for (int i = 0; i < 100; i++)
      {
        if (rerender)
        {
          rerender = false;
          break;
        }
        if (token.IsCancellationRequested)
          return;
        Thread.Sleep(10);
      }
    }
  }
}
