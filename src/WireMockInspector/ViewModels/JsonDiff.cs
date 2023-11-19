using System;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace WireMockInspector.ViewModels;

public class AlphabeticallySortedJsonDiffFormatter 
{
    public static readonly AlphabeticallySortedJsonDiffFormatter Instance = new AlphabeticallySortedJsonDiffFormatter();
    
    public string Format(JToken? diff)
    {
        if (diff == null)
        {
            return string.Empty;
        }

        if (diff is JObject jObject)
        {
            if(jObject.DeepClone() is JObject sorted)
            {
                Sort(sorted);
                return sorted.ToString();
            }

            return jObject.ToString();
        }
        return diff.ToString();
    }

    private static void Sort(JObject jObj)
    {
        var props = jObj.Properties().ToList();
        foreach (var prop in props)
        {
            prop.Remove();
        }

        foreach (var prop in props.OrderBy(p => p.Name))
        {
            jObj.Add(prop);
            if (prop.Value is JObject)
                Sort((JObject)prop.Value);
        }
    }
}

public static class JsonHelper
{
    public static string ToComparableForm(string json)
    {
        try
        {
            return AlphabeticallySortedJsonDiffFormatter.Instance.Format(JToken.Parse(json));
        }
        catch (Exception e)
        {
            return json;
        }
    }
}