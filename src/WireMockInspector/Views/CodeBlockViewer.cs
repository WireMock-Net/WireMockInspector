using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Media;
using AvaloniaEdit;
using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;
using AvaloniaEdit.TextMate;
using DiffPlex.DiffBuilder.Model;
using TextMateSharp.Grammars;
using WireMockInspector.ViewModels;

namespace WireMockInspector.Views;

public class CodeBlockViewer : TextEditor 
{
    protected override Type StyleKeyOverride { get; } =  typeof(TextEditor);

    public CodeBlockViewer()
    {
        this._registryOptions = new RegistryOptions(ThemeName.DarkPlus);
        this._textMateInstallation = this.InstallTextMate(_registryOptions);
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        this.Background = new SolidColorBrush(Color.FromRgb(30, 30, 30));
        this.TextArea.TextView.Margin = new Thickness(10, 0);
        this.ShowLineNumbers = true;
        this.IsReadOnly = true;
        this.FontFamily = "Cascadia Code,Consolas,Menlo,Monospace";
    }

    static CodeBlockViewer()
    {
        CodeProperty.Changed.Subscribe(OnCodeChanged);
    }

    private static void OnCodeChanged(AvaloniaPropertyChangedEventArgs e)
    {
        (e.Sender as CodeBlockViewer)?.OnCodeChanged((ViewModels.MarkdownCode)e.OldValue, (ViewModels.MarkdownCode)e.NewValue);
    }

    private void OnCodeChanged(MarkdownCode newValue, MarkdownCode NewValue)
    {
        SetMarkdown(NewValue);
    }


    private void SetMarkdown(ViewModels.MarkdownCode md)
    {
        if (md is not null)
        {
            
            if (_currentLang != md.lang)
            {
                _currentLang = md.lang;

                if (_registryOptions.GetLanguageByExtension("." + md.lang) is { } languageByExtension)
                {
                    _textMateInstallation.SetGrammar(_registryOptions.GetScopeByLanguageId(languageByExtension.Id));
                }


                if (this.TextArea.TextView.LineTransformers.OfType<DiffLineBackgroundRenderer>().FirstOrDefault() is { } existing)
                {
                    this.TextArea.TextView.LineTransformers.Remove(existing);
                }
                this.TextArea.TextView.LineTransformers.Add(new DiffLineBackgroundRenderer(md.oldTextLines));
            }
            
            if (this.Document is not null || _currentLang != md.lang)
            {
                this.Document.Text = md.rawValue;
            }
            else
            {
                this.Document = new TextDocument(md.rawValue);
            }
           
        }
        else
        {
            this.Document = new TextDocument("");
        }
            
    }

    private string? _currentLang;
    public ViewModels.MarkdownCode Code
    {
        get { return GetValue(CodeProperty); }
        set { SetValue(CodeProperty, value); }
    }

    public static readonly StyledProperty<ViewModels.MarkdownCode> CodeProperty = AvaloniaProperty.Register<CodeBlockViewer, ViewModels.MarkdownCode>(nameof(Code));
    private readonly TextMate.Installation _textMateInstallation;
    private readonly RegistryOptions _registryOptions;
}



public class DiffLineBackgroundRenderer :  GenericLineTransformer
{
    private readonly List<DiffPiece>? _mdOldTextLines;


    protected override void TransformLine(DocumentLine line, ITextRunConstructionContext context)
    {
        if (_mdOldTextLines is { } li)
        {
            var index = line.LineNumber -1;
            if (index is > -1 && index < li.Count)
            {
                var brush = li[index].Type switch
                {
                    ChangeType.Deleted => new SolidColorBrush(Colors.Red, 0.5),
                    ChangeType.Inserted => new SolidColorBrush(Colors.Green, 0.5),
                    ChangeType.Imaginary => new SolidColorBrush(Colors.Gray, 0.5),
                    ChangeType.Modified => new SolidColorBrush(Colors.Orange, 0.5),
                    _ => null
                };
                if (brush != null)
                {
                    this.SetTextStyle(line,0, line.Length, null,  brush, context.GlobalTextRunProperties.Typeface.Style, context.GlobalTextRunProperties.Typeface.Weight, false);        
                }    
            }
            
        }
        
        
    }

    public DiffLineBackgroundRenderer(List<DiffPiece>? mdOldTextLines) : base((_)=>{})
    {
        _mdOldTextLines = mdOldTextLines;
    }
}

