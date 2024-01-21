using System.Collections.Generic;
using System.Reactive.Linq;
using DynamicData.Binding;
using ReactiveUI;
using WireMock.Admin.Requests;
using WireMockInspector.CodeGenerators;

namespace WireMockInspector.ViewModels;

public class MappingCodeGeneratorViewModel : ViewModelBase
{
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
   
        Config.WhenAnyPropertyChanged()
            .Where(x=> x is not null)
            .Select(x =>
            {
                var code = MappingCodeGenerator.GenerateCSharpCode(Request, Response, x);
                return new MarkdownCode("cs", code);
            }).ToProperty(this, x => x.OutputCode, out _outputCode);
    }
}