using Spectre.Console;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Rendering
{
  public static class Frontend
  {
    static bool rerender = false;
    static CancellationTokenSource cts = new CancellationTokenSource();
    static Config config = Config.Load();

    public static string RenderMap(Config config, bool renderLive)
    {
      return "Rendering Map...";
    }
    public static string RenderSidebar(Config config)
    {
      return "Move using arrow keys [red]↑↓ ←→[/] zoom using [red]+/-[/] keys\nTo search press [red]F[/], For settings press [red]C[/]\nTo toggle panel press [red]H[/]  To exit press [red]Q[/]";
    }

    public static async Task RenderUI()
    {
      // Run the RenderLoop asynchronously
      var renderTask = RenderLoop(cts.Token);

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

    static async Task RenderLoop(CancellationToken token)
    {
      await AnsiConsole.Live(new Layout("Root")
          .SplitColumns(
              new Layout("Left"),
              new Layout("Right")
              .SplitRows(
                  new Layout("Top"),
                  new Layout("Bottom").Size(2))
          )).StartAsync(async ctx =>
      {
        while (!token.IsCancellationRequested)
        {
          Stopwatch stopwatch = new Stopwatch();
          stopwatch.Start();

          var layout = new Layout("Root")
                    .SplitColumns(
                        new Layout("Left"),
                        new Layout("Right")
                        .SplitRows(
                            new Layout("Top"),
                            new Layout("Bottom")
                        ));

          layout["Left"].Update(
              new Panel(
                Align.Center(
                  Renderer.RenderMap(config, true),
                  VerticalAlignment.Middle))
              .Border(BoxBorder.None)).MinimumSize((int)(AnsiConsole.Profile.Height * 2));
          layout["Top"].Update(
              new Panel(
                Align.Center(
                  new Text("Search for a location"),
                  VerticalAlignment.Top))
              .Expand());
          layout["Bottom"].Update(
              new Panel(
                Align.Center(
                  new Markup($"Running at {1000 / stopwatch.ElapsedMilliseconds} FPS \n {RenderSidebar(config)}"),
                  VerticalAlignment.Top))
              .Expand());

          ctx.UpdateTarget(layout);
          ctx.Refresh();

          stopwatch.Stop();

          // Short delay to control render loop frequency
          for (int i = 0; i < 100; i++)
          {
            if (rerender)
            {
              rerender = false;
              break;
            }
            if (token.IsCancellationRequested)
              return;
            await Task.Delay(10);
          }
        }
      });
    }

    static void HandleKeyPress(ConsoleKeyInfo key)
    {
      switch (key.Key)
      {
        case ConsoleKey.UpArrow:
          config.latitude += 0.001;
          break;
        case ConsoleKey.DownArrow:
          config.latitude -= 0.001;
          break;
        case ConsoleKey.LeftArrow:
          config.longitude -= 0.001;
          break;
        case ConsoleKey.RightArrow:
          config.longitude += 0.001;
          break;
        case ConsoleKey.Escape:
          cts.Cancel();
          Environment.Exit(0);
          break;
        default:
          if (key.KeyChar == '-')
          {
            config.zoom--;
          }
          else if (key.KeyChar == '+')
          {
            config.zoom++;
          }
          break;
      }
    }
  }
}
