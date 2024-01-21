using System.Collections.Generic;
using System.Runtime.Serialization;
using ReactiveUI;

namespace WireMockInspector.ViewModels;

[DataContract]
public class MappingCodeGeneratorConfigViewModel : ViewModelBase
{
    // Request attributes
    private bool _includeClientIP = true;
    
    [DataMember]
    public bool IncludeClientIP
    {
        get => _includeClientIP;
        set => this.RaiseAndSetIfChanged(ref _includeClientIP, value);
    }

    private bool _includePath = true;
    
    [DataMember]
    public bool IncludePath
    {
        get => _includePath;
        set => this.RaiseAndSetIfChanged(ref _includePath, value);
    }
    
    private bool _includeUrl = true;
    
    [DataMember]
    public bool IncludeUrl
    {
        get => _includeUrl;
        set => this.RaiseAndSetIfChanged(ref _includeUrl, value);
    }

    private bool _includeQuery = true;
    
    [DataMember]
    public bool IncludeQuery
    {
        get => _includeQuery;
        set => this.RaiseAndSetIfChanged(ref _includeQuery, value);
    }

    private bool _includeMethod = true;
    
    [DataMember]
    public bool IncludeMethod
    {
        get => _includeMethod;
        set => this.RaiseAndSetIfChanged(ref _includeMethod, value);
    }

    private bool _includeHeaders = true;
    
    [DataMember]
    public bool IncludeHeaders
    {
        get => _includeHeaders;
        set => this.RaiseAndSetIfChanged(ref _includeHeaders, value);
    }

    private bool _includeCookies = true;
    
    [DataMember]
    public bool IncludeCookies
    {
        get => _includeCookies;
        set => this.RaiseAndSetIfChanged(ref _includeCookies, value);
    }

    private bool _includeBody = true;
    
    [DataMember]
    public bool IncludeBody
    {
        get => _includeBody;
        set => this.RaiseAndSetIfChanged(ref _includeBody, value);
    }

    // Response attributes
    private bool _includeStatusCode = true;
    
    [DataMember]
    public bool IncludeStatusCode
    {
        get => _includeStatusCode;
        set => this.RaiseAndSetIfChanged(ref _includeStatusCode, value);
    }

    private bool _includeHeadersResponse = true;
    
    [DataMember]
    public bool IncludeHeadersResponse
    {
        get => _includeHeadersResponse;
        set => this.RaiseAndSetIfChanged(ref _includeHeadersResponse, value);
    }

    private bool _includeBodyResponse = true;
    
    [DataMember]
    public bool IncludeBodyResponse
    {
        get => _includeBodyResponse;
        set => this.RaiseAndSetIfChanged(ref _includeBodyResponse, value);
    }
    
    
    public List<string> Templates { get; set; }

    private string _selectedTemplate;

    public string SelectedTemplate
    {
        get => _selectedTemplate;
        set =>  this.RaiseAndSetIfChanged(ref _selectedTemplate, value);
    }
}