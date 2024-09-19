using Core.Api.Maps;
using Spectre.Console;
using Core.Rendering;

namespace Core.Debug
{
  public class Debug
  {
    public static void PrintLayout(Tile[] tiles)
    {
      int maxWidth = 120;
      var layout = new Layout("Root")
        .SplitColumns(
            new Layout("left")
            .SplitRows(
              new Layout("NorthWest"),
              new Layout("West"),
              new Layout("SouthWest")),
            new Layout("middle")
            .SplitRows(
              new Layout("North"),
              new Layout("Center"),
              new Layout("South")),
            new Layout("Right")
            .SplitRows(
              new Layout("NorthEast"),
              new Layout("East"),
              new Layout("SouthEast")));

      layout["Center"].Update(new CanvasImageWithText(tiles[(int)TileDirection.Center].filePath).MaxWidth(maxWidth));
      layout["North"].Update(new CanvasImageWithText(tiles[(int)TileDirection.North].filePath).MaxWidth(maxWidth));
      layout["South"].Update(new CanvasImageWithText(tiles[(int)TileDirection.South].filePath).MaxWidth(maxWidth));
      layout["West"].Update(new CanvasImageWithText(tiles[(int)TileDirection.West].filePath).MaxWidth(maxWidth));
      layout["East"].Update(new CanvasImageWithText(tiles[(int)TileDirection.East].filePath).MaxWidth(maxWidth));
      layout["NorthWest"].Update(new CanvasImageWithText(tiles[(int)TileDirection.NorthWest].filePath).MaxWidth(maxWidth));
      layout["NorthEast"].Update(new CanvasImageWithText(tiles[(int)TileDirection.NorthEast].filePath).MaxWidth(maxWidth));
      layout["SouthWest"].Update(new CanvasImageWithText(tiles[(int)TileDirection.SouthWest].filePath).MaxWidth(maxWidth));
      layout["SouthEast"].Update(new CanvasImageWithText(tiles[(int)TileDirection.SouthEast].filePath).MaxWidth(maxWidth));
      AnsiConsole.Write(layout);
    }
  }
}
