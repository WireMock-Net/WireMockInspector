using System;
using Avalonia;
using Avalonia.Media;
using Avalonia.Styling;
using AvaloniaEdit;
using AvaloniaEdit.Document;
using AvaloniaEdit.TextMate;
using TextMateSharp.Grammars;
using WireMockInspector.ViewModels;

namespace WireMockInspector.Views;

public class CodeBlockViewer:TextEditor, IStyleable
{
    Type IStyleable.StyleKey => typeof(TextEditor);
    public CodeBlockViewer()
    {
        this.Initialized += (sender, args) =>
        {
            //First of all you need to have a reference for your TextEditor for it to be used inside AvaloniaEdit.TextMate project.
            var _textEditor = this;
            _textEditor.Background = new SolidColorBrush(Color.FromRgb(30, 30, 30));
            _textEditor.TextArea.TextView.Margin = new Thickness(10);
            _textEditor.ShowLineNumbers = true;
            _textEditor.IsReadOnly = true;
            _textEditor.FontFamily = "Cascadia Code,Consolas,Menlo,Monospace";
        };
        this._registryOptions = new RegistryOptions(ThemeName.DarkPlus);
        this._textMateInstallation = this.InstallTextMate(_registryOptions);
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

                _textMateInstallation.SetGrammar(_registryOptions.GetScopeByLanguageId(_registryOptions.GetLanguageByExtension("."+md.lang).Id));    
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