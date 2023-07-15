using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Windows.Input;
using Avalonia;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ReactiveUI;

namespace WireMockInspector.ViewModels;

public class MatchDetailsViewModel:ViewModelBase
{
    private ActualValue _actualValue;
    private Markdown _expectations;
    public string RuleName { get; set; }
    public bool? Matched { get; set; }
    public bool NoExpectations { get; set; }

    public ActualValue ActualValue
    {
        get => _actualValue;
        set => this.RaiseAndSetIfChanged(ref _actualValue, value);
    }

    
    
    public Markdown Expectations
    {
        get => _expectations;
        set =>  this.RaiseAndSetIfChanged(ref _expectations,  value);
    }

    public ICommand ReformatActualValue { get; set; }
    public ICommand CopyActualValue { get; set; }
    
    public ICommand ReformatExpectations{ get; set; }
    public ICommand CopyExpectations{ get; set; }

    public MatchDetailsViewModel()
    {
        CopyActualValue = ReactiveCommand.Create(async () =>
        {
            if (ActualValue is MarkdownActualValue {Value: {rawValue: var raw}})
            {
                await Application.Current!.Clipboard.SetTextAsync(raw);
            }
            else if (ActualValue is SimpleActualValue {Value: var simpleValue} )
            {
                await Application.Current!.Clipboard.SetTextAsync(simpleValue);
            }
            else if(ActualValue is KeyValueListActualValue{SelectedActualValueGridItem: {} selectedRow})
            {
                await Application.Current!.Clipboard.SetTextAsync($"{selectedRow.Key}:{selectedRow.Value}");
            }
        });
        
        CopyExpectations = ReactiveCommand.Create(async () =>
        {
            if (Expectations is Markdown{rawValue: var value})
            {
                await Application.Current!.Clipboard.SetTextAsync(value);
            }
        });
        
        ReformatActualValue = ReactiveCommand.Create(() =>
        {
            if (this.ActualValue is MarkdownActualValue {Value: {} rawValue} markdown)
            {
                ActualValue = new MarkdownActualValue()
                {
                    Value = TryToReformat(rawValue)
                };
            }
        }, 
            this.WhenAnyValue(x=>x.ActualValue).Select(x =>
            {
                return x is MarkdownActualValue {Value: { } va} && IsJsonMarkdown(va);
            }));
        ReformatExpectations= ReactiveCommand.Create(() =>
        {
            if (string.IsNullOrWhiteSpace(Expectations.rawValue) == false)
            {
                Expectations = TryToReformat(Expectations);
            }
        }, this.WhenAnyValue(x=>x.Expectations).Select(IsJsonMarkdown));
    }

    private static Markdown TryToReformat(Markdown markdown)
    {
        if (IsJsonMarkdown(markdown))
        {

            try
            {
              
                var formatted = JToken.Parse(markdown.rawValue).ToString(Formatting.Indented);
                return MainWindowViewModel.AsMarkdownCode("json", formatted);
            }
            catch (Exception e)
            {
                
            }
        }

        return markdown;
    }

    private static bool IsJsonMarkdown(Markdown rawValue)
    {
        return rawValue?.lang == "json";
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
    public Markdown Value { get; set; }
    public string MarkdownValue { get; set; }
}

public record Markdown(string lang, string rawValue)
{
    public string AsMarkdownSyntax()
    {
        return $"```{lang}\r\n{rawValue}\r\n```";
    }

    public override string ToString() => AsMarkdownSyntax();
};

public class KeyValueListActualValue:ActualValue
{
    public KeyValuePair<string,string> SelectedActualValueGridItem { get; set; }
    public IReadOnlyList<KeyValuePair<string,string>> Items { get; set; }
}



