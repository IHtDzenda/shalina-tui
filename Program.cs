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
        HandleKeyPress(Console.ReadKey(intercept: true).Key);  // Read the key without displaying it
        rerender = true;  // Trigger immediate rerender
      }
      await Task.Delay(100);  // Check for key press every 100ms
    }
  }   
  static void HandleKeyPress(ConsoleKey key)
    {
        // Adjust config based on arrow key pressed
        switch (key)
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
            default:
                // Handle other keys if needed
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
      AnsiConsole.Write(Renderer.RenderMap(config, false));

      stopwatch.Stop();
      AnsiConsole.WriteLine($"Time elapsed: {stopwatch.ElapsedMilliseconds}ms");

      // Adjust sleep time to compensate for rendering duration
      int delay = 5000 - (int)stopwatch.ElapsedMilliseconds;
      if (delay > 0)
      {
        // Check if rerender was requested before the delay ends
        for (int i = 0; i < delay / 100; i++)
        {
          if (rerender)
          {
            rerender = false;
            break;
          }
          Thread.Sleep(100);  // Sleep in small increments
        }
      }
    }
  }
}
