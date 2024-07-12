using System.Reactive.Linq;
using System.Windows.Input;
using Avalonia.Controls.ApplicationLifetimes;
using DynamicData.Binding;
using ReactiveUI;
using WireMock.Admin.Requests;
using WireMockInspector.CodeGenerators.Code;

namespace WireMockInspector.ViewModels;

public class MappingCodeGeneratorViewModel : ViewModelBase
{
    public ICommand CopyActualValue { get; set; }
    private LogRequestModel _request;
    public LogRequestModel Request
    {
        get => _request;
        set => this.RaiseAndSetIfChanged(ref _request, value);
    }

    private LogResponseModel _response;
    public LogResponseModel Response
    {
        get => _response;
        set => this.RaiseAndSetIfChanged(ref _response, value);
    }

    private MappingCodeGeneratorConfigViewModel _config;

    public MappingCodeGeneratorConfigViewModel Config
    {
        get;
        private set;
    } = new MappingCodeGeneratorConfigViewModel();

    private readonly ObservableAsPropertyHelper<MarkdownCode> _outputCode;
    public MarkdownCode OutputCode => _outputCode.Value;
  
    public MappingCodeGeneratorViewModel()
    {
        CopyActualValue = ReactiveCommand.Create(async () =>
        {
            if (Avalonia.Application.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                await desktop.MainWindow.Clipboard.SetTextAsync(_outputCode.Value.rawValue);
            }
        });

        Config.WhenAnyPropertyChanged()
            .Where(x=> x is not null)
            .Select(x =>
            {
                var code = MappingCodeGenerator.Generate(Request, Response, x);
                return new MarkdownCode("csharp", code);
            }).ToProperty(this, x => x.OutputCode, out _outputCode);
    }
}