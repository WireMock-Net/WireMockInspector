using System;
using Avalonia;
using Avalonia.Media;
using AvaloniaEdit;
using AvaloniaEdit.Document;
using AvaloniaEdit.TextMate;
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


    private  void SetMarkdown(ViewModels.MarkdownCode md)
    {
        if (md is not null)
        {
            if (this.Document is not null)
            {
                this.Document.Text = md.rawValue;
            }
            else
            {
                this.Document = new TextDocument(md.rawValue);
            }
            if (_currentLang != md.lang)
            {
                _currentLang = md.lang;
                CodeBlockViewer _textEditor = this;

                if (_registryOptions.GetLanguageByExtension("." + md.lang) is { } languageByExtension)
                {
                    _textMateInstallation.SetGrammar(_registryOptions.GetScopeByLanguageId(languageByExtension.Id));
                }    
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