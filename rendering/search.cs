using System.Globalization;
using Core.Api.Maps;

namespace Core.Rendering.Search;
public class UserQuery
{
  List<(bool inversion, string value)> values = new List<(bool,string)>();
  private static readonly char[] separators = { ' ', '\t', ',', ';' };
  UserQuery(List<(bool, string)> values)
  {
    if (values == null)
    {
      throw new ArgumentNullException(nameof(values));
    }
    this.values = values;
  }
  // Parse the input string
  public UserQuery(string value)
  {
    if (value == null)
    {
      throw new ArgumentNullException(nameof(value));
    }
    value = value.Trim();
    bool inverted = false;
    string searchString = "";
    for (int i = 0; i < value.Length; i++)
    {
      if (searchString.Length == 0 && value[i] == '!')
      {
        inverted = !inverted;
        continue;
      }
      bool isSeparator = separators.Contains(value[i]);
      bool isLast = i == value.Length - 1;

      if (isLast || (isSeparator && searchString.Length > 0))
      {
        if (isLast && !isSeparator)
          searchString += value[i];
        values.Add((inverted,searchString));
        searchString = "";
        inverted = false;
        continue;
      }
      searchString += value[i];
    }
  }
  public UserQuery() {}

  public bool MatchSingle(string[] matchValues, int partialMatchStartIdx = 0)
  {
    if (matchValues == null)
      return false;

    bool isMatch = this.values.Count == 0;
    for (int i = 0; i < values.Count; i++)
    {
      if (values[i].value.Equals("all", StringComparison.InvariantCultureIgnoreCase))
        isMatch = true;
      for (int j = 0; j < matchValues.Length - partialMatchStartIdx; j++)
      {
        if (matchValues[j].Equals(values[i].value, StringComparison.InvariantCultureIgnoreCase))
        {
          isMatch = true;
          if (values[i].inversion)
            return false;
        }
      }
      for (int j = matchValues.Length - partialMatchStartIdx; j < matchValues.Length; j++)
      {
        if(matchValues[j].Length > 2)
          continue;
        if (CultureInfo.InvariantCulture.CompareInfo.IndexOf(matchValues[j], values[i].value, CompareOptions.IgnoreCase) >= 0)
        {
          isMatch = true;
          if (values[i].inversion)
            return false;
          continue;
        }
      }
    }
    return isMatch;
  }
  public bool MatchSingle((RouteType type, KeyValuePair<string, GeoData> transport) data)
  {
    if (data.transport.Value == null)
      return false;
    // Only the last 1 element is a partial match
    return MatchSingle([data.type.ToString(), data.transport.Key, data.transport.Value.routeNameLong], 1);
  }
  public bool MatchSingle((RouteType type, KeyValuePair<string, Transport> transport) data)
  {
    if (data.transport.Value == null)
      return false;
    // Only the last 1 element is a partial match
    return MatchSingle([data.type.ToString(), data.transport.Key, data.transport.Value.state.ToString(), data.transport.Value.lineName], 0);
  }
  public bool MatchSingle( Stop data)
  {
    if (data == null)
      return false;
    // Only the last 1 element is a partial match
    string[] values = data.lines.SelectMany(x => new string[] { x.type.ToString(), x.name }).ToArray()
      .Concat(new string[] { data.municipality, data.name }).ToArray();
    return MatchSingle(values, 1);
  }
}

