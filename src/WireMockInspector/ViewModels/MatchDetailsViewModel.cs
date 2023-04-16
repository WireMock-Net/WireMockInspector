using System.Collections.Generic;

namespace WireMockInspector.ViewModels;

public class MatchDetailsViewModel:ViewModelBase
{
    public string RuleName { get; set; }
    public bool? Matched { get; set; }
    public bool NoExpectations { get; set; }

    public ActualValue ActualValue { get; set; }
    public string Expectations { get; set; }
}


public abstract class ActualValue:ViewModelBase
{
    
}

public class SimpleActualValue:ActualValue
{
    public string Value { get; set; }
}

public class MarkdownActualValue:ActualValue
{
    public string Value { get; set; }
}

public class KeyValueListActualValue:ActualValue
{
    public IReadOnlyList<KeyValuePair<string,string>> Items { get; set; }
}



