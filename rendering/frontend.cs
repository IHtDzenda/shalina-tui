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
    static string query = "";
    static CancellationTokenSource cts = new CancellationTokenSource();
    static Config config = Config.Load();

    public static string RenderTooltip()
    {
      if (config.isConfigOpen)
      {
        return EditConfig();
      }
      else if (config.isSidebarOpen)
      {
        return RenderSearch();
      }
      return "";
    }
    public static string RenderSearch()
    {
      string search = !config.isSearching && query.Length == 0 ? "[gray](press F to focus search)[/]" : config.isSearching ? $"[red]{query}[/]" : $"[gray]{query}";
      return $"Search and filter connections \n-> {search}";
    }
    public static string RenderSidebar(Config config)
    {
      return "Move using arrow keys [red]↑↓ ←→[/] zoom using [red]+/-[/] keys\nTo search press [red]F[/], For settings press [red]C[/]\nTo toggle panel press [red]H[/]  To exit press [red]Q[/]";
    }
    public static string EditConfig()
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
          string text = config.isEditingConfig ? $"[red]{config.newConfigValue}[/]" + cursor : $"#{hexColor}";
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
          switch (config.layout)
          {
            case Config.Layout.Map:
              HandleMapKeyPress(key);
              break;
            case Config.Layout.Config:
              HandleSidebarKeyPress(key);
              break;
            case Config.Layout.Search:
              HandleSidebarKeyPress(key);
              break;
          }
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
          bool isSidebarSelected = config.isSearching || config.isConfigOpen || config.isEditingConfig;
          var layout = config.isSidebarOpen ? new Layout("Root")
                    .SplitColumns(
                        new Layout("Left"),
                         new Layout("Right")
                        .SplitRows(
                            new Layout("Top"),
                            new Layout("Bottom")
                        )) : new Layout("Root")
                        .SplitColumns(
                            new Layout("Left"));

          layout["Left"].Update(
              new Panel(
                Align.Center(
                  Renderer.RenderMap(config, true),
                  VerticalAlignment.Middle))
              .Border(BoxBorder.None)).MinimumSize((int)(((config.resolution.width + 1) * 2)));
          if (config.isSidebarOpen)
          {
            layout["Top"].Update(
                new Panel(
                  Align.Center(
                    new Markup(RenderTooltip()),
                    VerticalAlignment.Top)).BorderColor(isSidebarSelected ? Color.Red : Color.Default)
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
    private static void HandleSidebarKeyPress(ConsoleKeyInfo key)
    {
      double step = 32 / (double)(2 << config.zoom);
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
        case ConsoleKey.Q:
          cts.Cancel();
          Environment.Exit(0);
          break;
        case ConsoleKey.F:
          if (config.isEditingConfig || !config.isSidebarOpen)
            break;
          config.isSearching = !config.isSearching;
          break;
        case ConsoleKey.C:
          config.isConfigOpen = !config.isConfigOpen;
          break;
        case ConsoleKey.Enter:
          if (config.isConfigOpen && config.isEditingConfig)
          {
            config.colorScheme[config.colorScheme.ElementAt(config.cursorConfigIndex).Key] = Util.ParseHexColor(config.newConfigValue);
            config.isEditingConfig = false;
            config.Save();
          }
          else if (config.isConfigOpen && !config.isEditingConfig)
          {
            config.isEditingConfig = true;
            config.newConfigValue = config.colorScheme.ElementAt(config.cursorConfigIndex).Value.ToHex();
          }

          break;
        case ConsoleKey.Escape:

          if (config.isEditingConfig)
          {
            config.isEditingConfig = false;
            break;
          }
          else if (config.isConfigOpen)
          {
            config.isConfigOpen = false;
          }
          else if (config.isSearching)
          {
            config.isSearching = false;
            query = "";
          }
          config.layout = Config.Layout.Map;
          break;
        case ConsoleKey.Backspace:
          if (query.Length > 0 && config.isSearching)
          {
            query = query.Substring(0, query.Length - 1);
            config.query = new UserQuery(query);
          }
          else if (config.isEditingConfig && config.newConfigValue.Length > 0)
          {
            config.newConfigValue = config.newConfigValue.Substring(0, config.newConfigValue.Length - 1);
          }
          break;
        default:
          if (config.isSearching)
          {
            query = query + key.KeyChar.ToString();
            config.query = new UserQuery(query);
          }
          else if (config.isEditingConfig)
          {
            config.newConfigValue = config.newConfigValue + key.KeyChar.ToString().ToUpper();
          }
          break;
      }
    }
    static void HandleMapKeyPress(ConsoleKeyInfo key)
    {
       double step = 32 /(double)( 2 << config.zoom);

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
        case ConsoleKey.F:
          config.layout = config.layout == Config.Layout.Map ? Config.Layout.Search : Config.Layout.Map;
          config.isSearching = !config.isSearching;
          break;
        case ConsoleKey.C:
          config.layout = config.layout == Config.Layout.Map ? Config.Layout.Config : Config.Layout.Map;
          config.isConfigOpen = !config.isConfigOpen;
          break;
        case ConsoleKey.H:
          config.isSidebarOpen = !config.isSidebarOpen;
          if (!config.isSidebarOpen)
          {
            query = query + key.KeyChar.ToString();
            config.query = new UserQuery(query);
            config.resolution = ((short)((AnsiConsole.Profile.Width - 1) / 2), (short)AnsiConsole.Profile.Height);
          }
          else
          {
            config.resolution = ((short)(AnsiConsole.Profile.Width / 3), (short)AnsiConsole.Profile.Height);
          }
          rerender = true;
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
