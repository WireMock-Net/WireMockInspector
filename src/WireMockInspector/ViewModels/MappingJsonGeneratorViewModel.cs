using System.Reactive.Linq;
using System.Windows.Input;
using Avalonia.Controls.ApplicationLifetimes;
using DynamicData.Binding;
using ReactiveUI;
using WireMock.Admin.Requests;
using WireMockInspector.CodeGenerators.Json;

namespace WireMockInspector.ViewModels;

public class MappingJsonGeneratorViewModel : ViewModelBase
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

    public MappingJsonGeneratorViewModel()
    {
        CopyActualValue = ReactiveCommand.Create(async () =>
        {
            if (Avalonia.Application.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                await desktop.MainWindow.Clipboard.SetTextAsync(_outputCode.Value.rawValue);
            }
        });

        Config.WhenAnyPropertyChanged()
            .Where(x => x is not null)
            .Select(x =>
            {
                var code = MappingJsonGenerator.Generate(Request, Response, x);
                return new MarkdownCode("json", code);
            }).ToProperty(this, x => x.OutputCode, out _outputCode);
    }
}