using System;
using Avalonia;
using Avalonia.Media;
using Avalonia.Styling;
using AvaloniaEdit;
using AvaloniaEdit.Document;
using AvaloniaEdit.TextMate;
using TextMateSharp.Grammars;

namespace WireMockInspector.Views;

public class CodeBlockViewer:TextEditor, IStyleable
{
    Type IStyleable.StyleKey => typeof(TextEditor);
    public CodeBlockViewer()
    {
        this.Loaded += (sender, args) =>
        {

        };
            
            
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
        this.PropertyChanged+= (sender, args) =>
        {
            if (args.Property.Name == nameof(Code))
            {
                SetMarkdown(args.NewValue as ViewModels.Markdown);
            }
        };
    }

    private  void SetMarkdown(ViewModels.Markdown md)
    {
        if (md is not null)
        {
            this.Document = new TextDocument(md.rawValue);
            if (_currentLang != md.lang)
            {
                _currentLang = md.lang;
                CodeBlockViewer _textEditor = this;

                var _registryOptions = new RegistryOptions(ThemeName.DarkPlus);
                var _textMateInstallation = _textEditor.InstallTextMate(_registryOptions);
                _textMateInstallation.SetGrammar(_registryOptions.GetScopeByLanguageId(_registryOptions.GetLanguageByExtension("."+md.lang).Id));    
            }
        }
        else
        {
            this.Document = new TextDocument("");
        }
            
    }

    private string? _currentLang;
    public ViewModels.Markdown Code
    {
        get { return GetValue(CodeProperty); }
        set
        {
            SetValue(CodeProperty, value);
            SetMarkdown(value);
        }
    }

    public static readonly StyledProperty<ViewModels.Markdown> CodeProperty = AvaloniaProperty.Register<CodeBlockViewer, ViewModels.Markdown>(nameof(Code));
}