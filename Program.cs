using Core.Rendering;

public class Program
{
  static async Task Main(string[] args)
  {
    var culture = (System.Globalization.CultureInfo) System.Threading.Thread.CurrentThread.CurrentCulture.Clone();
    culture.NumberFormat.NumberDecimalSeparator = ".";
    System.Threading.Thread.CurrentThread.CurrentCulture = culture;
    await Frontend.RenderUI();
  }
}
