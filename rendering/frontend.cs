using Core.Rendering.Search;
using SixLabors.ImageSharp.PixelFormats;
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

    public static string RenderTooltip()
    {
      if (config.layout == Config.Layout.Search)
      {
        return RenderSearch();
      }
      else if (config.layout == Config.Layout.Config)
      {
        return RenderConfig();
      }
      return "";
    }
    public static string RenderSearch()
    {
      string search = config.userQuery + " ";
      if (config.userQuery.Length == 0 && !config.sidebarSelected)
        search = "[gray](press tab to focus search)[/]";
      else if (config.sidebarSelected)
      {
        search = "[red]" + search.Substring(0, config.cursorConfigIndex) + "[red on white]" + search[config.cursorConfigIndex] + "[/]" + search.Substring(config.cursorConfigIndex + 1) + "[/]";
      }
      else
        search = $"[gray]{config.userQuery}[/]";

      return $"Search and filter connections \n-> {search}";
    }
    public static string RenderSidebar(Config config)
    {
      return "Move using arrow keys [red]↑↓ ←→[/] zoom using [red]+/-[/] keys\nTo toggle sidebar selection press [red]TAB[/]\nPress [red]ESC[/] to deselect current menu\nTo toggle panel press [red]H[/] To exit press [red]Q[/]";
    }
    public static string RenderConfig()
    {
      int index = config.cursorConfigIndex;
      string[] cfg = new string[config.colorScheme.Count];
      for (int i = 0; i < config.colorScheme.Count; i++)
      {
        string hexColor = config.colorScheme.ElementAt(i).Value.ToHex();
        string colorDot = $"[default on #{hexColor}]  [/]";
        if (i == index)
        {
          string cursor = "█";
          string text = config.sidebarSelected ? $"[red]{config.newConfigValue}[/]" + cursor : $"#{hexColor}";
          cfg[i] = $"[red]{config.colorScheme.ElementAt(i).Key}[/] - {text} - {colorDot}";
        }
        else
        {
          cfg[i] = $"{config.colorScheme.ElementAt(i).Key} - #{hexColor} - {colorDot}";
        }
      }
      return String.Join("\n", cfg);
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
          ConsoleKeyInfo key = Console.ReadKey(intercept: true);
          if (config.sidebarSelected)
          {
            switch (config.layout)
            {
              case Config.Layout.Config:
                HandleConfigKeyPress(key);
                break;
              case Config.Layout.Search:
                HandleSearchKeyPress(key);
                break;
            }
          }
          else
          {
            HandleMapKeyPress(key);
          }
          rerender = true; // Trigger immediate rerender
        }
        await Task.Delay(10);
      }
    }


    static async Task RenderLoop(CancellationToken token)
    {
      try
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
            var layout = config.layout == Config.Layout.Map ? new Layout("Root")
                          .SplitColumns(
                              new Layout("Left")) : new Layout("Root")
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
                .Border(BoxBorder.None)).MinimumSize((int)(((config.resolution.width + 1) * 2)));
            if (config.layout != Config.Layout.Map)
            {
              layout["Top"].Update(
                  new Panel(
                    Align.Center(
                      new Markup(RenderTooltip()),
                      VerticalAlignment.Top)).BorderColor(config.sidebarSelected ? Color.Red : Color.Default)
                  .Expand());
              layout["Bottom"].Update(
                  new Panel(
                    Align.Center(
                      new Markup($"Running at {1000 / stopwatch.ElapsedMilliseconds} FPS \n {RenderSidebar(config)}"),
                      VerticalAlignment.Bottom))
                  .Expand());
            }
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
      catch (Exception e)
      {
        Console.WriteLine(e);
      }
    }
    private static void HandleConfigKeyPress(ConsoleKeyInfo key)
    {
      switch (key.Key)
      {
        case ConsoleKey.UpArrow:
          config.cursorConfigIndex--;
          if (config.cursorConfigIndex < 0)
          {
            config.cursorConfigIndex = config.colorScheme.Count - 1;
          }
          break;
        case ConsoleKey.DownArrow:
          config.cursorConfigIndex++;
          if (config.cursorConfigIndex >= config.colorScheme.Count)
          {
            config.cursorConfigIndex = 0;
          }
          break;
        case ConsoleKey.Tab:
          config.layout = Config.Layout.Search;
          break;
        case ConsoleKey.Enter:
          // TODO

          break;
        case ConsoleKey.Escape:
          config.sidebarSelected = false;
          break;
        case ConsoleKey.Backspace:
          if (config.newConfigValue.Length > 0)
          {
            config.newConfigValue = config.newConfigValue.Substring(0, config.cursorConfigIndex - 1) + config.newConfigValue.Substring(config.cursorConfigIndex);
            config.cursorConfigIndex--;
          }
          break;
        default:
          config.newConfigValue = config.newConfigValue + key.KeyChar.ToString().ToUpper();
          break;
      }
    }
    private static void HandleSearchKeyPress(ConsoleKeyInfo key)
    {
      switch (key.Key)
      {
        case ConsoleKey.LeftArrow:
          if ( config.cursorConfigIndex > 0) {
            config.cursorConfigIndex--;
          }
          break;
        case ConsoleKey.RightArrow:
          if ( config.cursorConfigIndex < config.userQuery.Length) {
            config.cursorConfigIndex++;
          }
          break;
        case ConsoleKey.Tab:
          config.layout = Config.Layout.Config;
          break;
        case ConsoleKey.Escape:
          config.sidebarSelected = false;
          break;
        case ConsoleKey.Backspace:
          if (config.userQuery.Length > 0)
          {
            config.userQuery = config.userQuery.Substring(0, config.userQuery.Length - 1);
            config.cursorConfigIndex--;
          }
          config.query = new UserQuery(config.userQuery);
          rerender = true;
          break;
        default:
          config.userQuery = config.userQuery + key.KeyChar.ToString().ToUpper();
          config.query = new UserQuery(config.userQuery);
          rerender = true;
          config.cursorConfigIndex++;
          break;
      }
    }
    static void HandleMapKeyPress(ConsoleKeyInfo key)
    {
      double step = 32 / (double)(2 << config.zoom);

      switch (key.Key)
      {
        case ConsoleKey.UpArrow:
          config.latitude += step;
          break;
        case ConsoleKey.DownArrow:
          config.latitude -= step;
          break;
        case ConsoleKey.LeftArrow:
          config.longitude -= step;
          break;
        case ConsoleKey.RightArrow:
          config.longitude += step;
          break;
        case ConsoleKey.Q:
          cts.Cancel();
          Environment.Exit(0);
          break;
        case ConsoleKey.Tab:
          if (config.layout != Config.Layout.Map)
          {
            config.sidebarSelected = true;
          }
          break;
        case ConsoleKey.H:
          if (config.layout != Config.Layout.Map)
          {
            config.layout = Config.Layout.Map;
            config.resolution = ((short)((AnsiConsole.Profile.Width - 1) / 2), (short)AnsiConsole.Profile.Height);
          }
          else
          {
            config.layout = Config.Layout.Search;
            config.resolution = ((short)(AnsiConsole.Profile.Width / 3), (short)AnsiConsole.Profile.Height);
          }
          config.sidebarSelected = false;
          rerender = true;
          break;
        case ConsoleKey.Add:
          config.zoom++;
          break;
        case ConsoleKey.Subtract:
          config.zoom--;
          break;

        default:
          break;
      }
    }

  }
}
