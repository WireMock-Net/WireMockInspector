using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ReactiveUI;

namespace WireMockInspector.ViewModels;

public class ExpectationMatcher
{
    
    public IReadOnlyList<KeyValuePair<string,string>> Attributes { get; set; }
    public List<string> Tags { get; set; }
    public List<MarkdownCode> Patterns { get; set; }
}

public abstract class ExpectationsModel
{
   
}

public class SimpleKeyValueExpectations: ExpectationsModel
{
    public IReadOnlyList<KeyValuePair<string,string>> Items { get; set; }
    
}

class SimpleStringExpectations : ExpectationsModel
{
    public string Value { get; set; }
}

public class MissingExpectations:ExpectationsModel
{
    public static readonly MissingExpectations Instance = new MissingExpectations();
}

public class RawExpectations:ExpectationsModel
{
    public MarkdownCode Definition { get; set; }
}

public class RichExpectations:ExpectationsModel
{
    public MarkdownCode Definition { get; set; }
    public string? Operator { get; set; }
    public List<ExpectationMatcher> Matchers { get; set; }
}    

public class MatchDetailsViewModel:ViewModelBase
{
    private ActualValue _actualValue;
    
    public string RuleName { get; set; }
    public bool? Matched { get; set; }
    public bool NoExpectations { get; set; }

    public ActualValue ActualValue
    {
        get => _actualValue;
        set => this.RaiseAndSetIfChanged(ref _actualValue, value);
    }

    public ExpectationsModel Expectations
    {
        get => _expectations;
        set => this.RaiseAndSetIfChanged(ref _expectations, value);
    }

    private ExpectationsModel _expectations;




    public ICommand ReformatActualValue { get; set; }
    public ICommand CopyActualValue { get; set; }
    

    public MatchDetailsViewModel()
    {
        CopyActualValue = ReactiveCommand.Create(async () =>
        {
            if (Avalonia.Application.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                if (ActualValue is MarkdownActualValue {Value: {rawValue: var raw}})
                {
                    await desktop.MainWindow.Clipboard.SetTextAsync(raw);
                }
                else if (ActualValue is SimpleActualValue {Value: var simpleValue} )
                {
                    await desktop.MainWindow.Clipboard.SetTextAsync(simpleValue);
                }
                else if(ActualValue is KeyValueListActualValue{SelectedActualValueGridItem: {} selectedRow})
                {
                    await desktop.MainWindow.Clipboard.SetTextAsync($"{selectedRow.Key}:{selectedRow.Value}");
                }    
            }
            
            
        });
        
       
        
        ReformatActualValue = ReactiveCommand.Create(() =>
        {
            if (this.ActualValue is MarkdownActualValue {Value: {} rawValue} markdown)
            {
                ActualValue = new MarkdownActualValue()
                {
                    Value = rawValue.TryToReformat()
                };
            }
        }, 
            this.WhenAnyValue(x=>x.ActualValue).Select(x =>
            {
                return x is MarkdownActualValue {Value: { } va} && va.IsJsonMarkdown();
            }));
        
    }

    
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
    public MarkdownCode Value { get; set; }
    public string MarkdownValue { get; set; }
}

public record MarkdownCode(string lang, string rawValue)
{
    public string AsMarkdownSyntax()
    {
        if (string.IsNullOrWhiteSpace(rawValue) == false)
        {
            return $"```{lang}\r\n{rawValue}\r\n```";    
        }

        return string.Empty;
    }

    public override string ToString() => AsMarkdownSyntax();
    
    public  MarkdownCode TryToReformat()
    {
        if (IsJsonMarkdown())
        {

            try
            {
              
                var formatted = JToken.Parse(this.rawValue).ToString(Formatting.Indented);
                return MainWindowViewModel.AsMarkdownCode("json", formatted);
            }
            catch (Exception e)
            {
                
            }
        }

        return this;
    }

    public bool IsJsonMarkdown()
    {
        return this.lang == "json";
    }
};

public class KeyValueListActualValue:ActualValue
{
    public KeyValuePair<string, string> SelectedActualValueGridItem { get; set; } = new KeyValuePair<string, string>();
    public IReadOnlyList<KeyValuePair<string,string>> Items { get; set; }
}



