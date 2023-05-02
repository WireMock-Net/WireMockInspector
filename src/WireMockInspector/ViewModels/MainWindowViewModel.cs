using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using DynamicData;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ReactiveUI;
using RestEase;
using WireMock.Admin.Mappings;
using WireMock.Admin.Requests;
using WireMock.Client;
using WireMock.Types;

namespace WireMockInspector.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {

        public bool DataLoaded
        {
            get => _dataLoaded;
            set => this.RaiseAndSetIfChanged(ref _dataLoaded, value);
        }

        private bool _dataLoaded;




        public string AdminUrl
        {
            get => _adminUrl;
            set => this.RaiseAndSetIfChanged(ref _adminUrl, value);
        }

        private string _adminUrl;

        public ObservableCollection<MappingViewModel> Mappings { get;  } = new();

        public ObservableCollection<RequestViewModel> Requests { get; } = new();

        public RequestViewModel? SelectedRequest
        {
            get => _selectedRequest;
            set => this.RaiseAndSetIfChanged(ref _selectedRequest, value);
        }

        private RequestViewModel? _selectedRequest;

        public MappingViewModel SelectedMapping
        {
            get => _selectedMapping;
            set => this.RaiseAndSetIfChanged(ref _selectedMapping, value);
        }

        private MappingViewModel _selectedMapping;



        

        private readonly ObservableAsPropertyHelper<NewVersionInfoViewModel> _newVersion;
        public NewVersionInfoViewModel NewVersion => _newVersion.Value;


        private GithubUpdater _githubUpdater = new GithubUpdater("cezarypiatek/WireMockInspector");
        private WireMockInspectorSettingsManager _settingsManager = new();

        public MainWindowViewModel()
        {
            Observable.FromAsync(async () =>
            {
                var (isNewVersionAvailable, version) = await _githubUpdater.CheckIsNewerVersionAvailable();
                return new NewVersionInfoViewModel(_githubUpdater.RepositorySku)
                {
                    IsVisible = isNewVersionAvailable,
                    Version = version
                };
            })
            .ObserveOn(RxApp.MainThreadScheduler)
            .ToProperty(this, x => x.NewVersion, out _newVersion);

            LoadRequestsCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                var api = RestClient.For<IWireMockAdminApi>(AdminUrl);
                var requestsTask = api.GetRequestsAsync();
                var mappingsTask = api.GetMappingsAsync();
                await Task.WhenAll(requestsTask, mappingsTask).ConfigureAwait(false);
                return (requests: requestsTask.Result, mappings: mappingsTask.Result);
            }); 

            LoadRequestsCommand
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(x =>
                {
                    var requests = x.requests.Select(MapRequestData).OfType<RequestViewModel>().OrderByDescending(x => x.Timestamp);
                    SelectedRequest = null;
                    Requests.Clear();
                    Requests.AddRange(requests);
                    RequestSearchTerm = string.Empty;
                    
                    Mappings.Clear();

                    var hitCalculator = new MappingHitCalculator(x.requests);

                    var mappings = x.mappings.Select(model =>
                    {
                        var partialHitCount = hitCalculator.GetPartialHitCount(model.Guid);
                        var perfectHitCount = hitCalculator.GetPerfectHitCount(model.Guid);
                        return new MappingViewModel()
                        {
                            Raw = model,
                            Id = model.Guid?.ToString(),
                            Title = model.Title,
                            Description = model.Description,
                            UpdatedOn = model.UpdatedAt,
                            Content = AsMarkdownCode("json", JsonConvert.SerializeObject(model, Formatting.Indented)),
                            PartialHitCount = partialHitCount,
                            PerfectHitCount = perfectHitCount,
                            HitType = (perfectHitCount, partialHitCount) switch
                            {
                                ( > 0, _) => MappingHitType.PerfectMatch,
                                (_, >0) => MappingHitType.OnlyPartialMatch,
                                _ => MappingHitType.Unmatched
                            }
                        };
                    }).OfType<MappingViewModel>().OrderBy(x=>x.UpdatedOn);
                    Mappings.AddRange(mappings);
                    MappingSearchTerm = string.Empty;
                    DataLoaded = true;
                });

            LoadRequestsCommand.ThrownExceptions.ObserveOn(RxApp.MainThreadScheduler).Subscribe(ex =>
            {
                DataLoaded = false;
            });

            LoadRequestsCommand
                .ObserveOn(RxApp.TaskpoolScheduler)
                .Subscribe( async _ =>
                {
                    await _settingsManager.UpdateSettings(settings =>
                    {
                        settings.AdminUrl = AdminUrl;
                    }).ConfigureAwait(false);
                });

            Observable.FromAsync(async () => await _settingsManager.LoadSettings().ConfigureAwait(false))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(settings =>
                {
                    if (string.IsNullOrWhiteSpace(AdminUrl))
                    {
                        AdminUrl = settings.AdminUrl;
                    }
                });

            this.WhenAnyValue(x => x.SelectedRequest)
                .OfType<RequestViewModel>()
                .Select( req =>
                {
                    var expectations = Mappings.FirstOrDefault(x => x.Raw.Guid == req.MappingId)?.Raw ?? new MappingModel(); ;
                    return GetMappingDetails(req, expectations);
                })
                .ObserveOn(RxApp.MainThreadScheduler)
                .ToProperty(this, x => x.RelatedMapping, out _relatedMapping);

            this.WhenAnyValue(x => x.RequestSearchTerm, x=>x.Requests,  x=>x.RequestTypeFilter, (term, requests, type) => (term, requests, type))
                .Throttle(TimeSpan.FromMilliseconds(200))
                .DistinctUntilChanged()
                .Select(x =>
                {
                    IEnumerable<RequestViewModel> result = x.requests;
                    if (string.IsNullOrWhiteSpace(x.term) == false)
                    {
                        result = result.Where(el =>
                            el.Path.Contains(x.term, StringComparison.InvariantCultureIgnoreCase));
                    }

                    return x.type switch
                    {
                        1 => result.Where(x=>x.IsMatched),
                        2 => result.Where(x => x.IsMatched == false),
                        _ => result,
                    } ;
                })
                .ObserveOn(RxApp.MainThreadScheduler)
                .ToProperty(this, x => x.FilteredRequests, out _filteredRequests);  
            
            this.WhenAnyValue(x => x.MappingSearchTerm, x=>x.MappingTypeFilter, (term, type) => (term,  type))
                .Throttle(TimeSpan.FromMilliseconds(200))
                .DistinctUntilChanged()
                .Select(x =>
                {
                    IEnumerable<MappingViewModel> result = Mappings;
                    if (string.IsNullOrWhiteSpace(x.term) == false)
                    {
                        result = result.Where(el => 
                            el.Id?.Contains(x.term, StringComparison.InvariantCultureIgnoreCase) == true ||
                            el.Title?.Contains(x.term, StringComparison.InvariantCultureIgnoreCase) == true ||
                            el.Description?.Contains(x.term, StringComparison.InvariantCultureIgnoreCase) == true
                            );
                    }

                    return x.type switch
                    {
                        1 => result.Where(x=>x.PerfectHitCount > 0),
                        2 => result.Where(x => x.PartialHitCount > 0),
                        3 => result.Where(x => x.HitType == MappingHitType.Unmatched),
                        _ => result,
                    } ;
                })
                .ObserveOn(RxApp.MainThreadScheduler)
                .ToProperty(this, x => x.FilteredMappings, out _filteredMappings);

            this.WhenAnyValue(x => x.SelectedMapping)
                .Where(x=> x?.Raw?.Guid != null)
                .SelectMany(async sm =>
                {
                    try
                    {
                        var api = RestClient.For<IWireMockAdminApi>(AdminUrl);
                        return await api.GetMappingCodeAsync(sm.Raw.Guid!.Value, MappingConverterType.Server);
                    }
                    catch (Exception e)
                    {
                        return "Code definition unavailable";
                    }
                })
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(code =>
                {
                    SelectedMapping.Code = AsMarkdownCode("cs", code);
                });
        }

        private RequestViewModel? MapRequestData(LogEntryModel r)
        {
            try
            {
                var matchModel = r.PartialRequestMatchResult ?? r.RequestMatchResult;
                var mappingGuid = r.PartialMappingGuid ?? r.MappingGuid;
                return new RequestViewModel
                {
                    Raw = r,
                    MatchingStatus = r switch
                    {
                        {RequestMatchResult.IsPerfectMatch: true} => MatchingStatus.PerfectMatch,
                        {PartialRequestMatchResult: { }} => MatchingStatus.PartialMatch,
                        _ => MatchingStatus.Unmatched
                    },
                    Path = r.Request.Url.Substring(AdminUrl.Length),
                    Timestamp = r.Request.DateTime,
                    IsMatched = matchModel?.IsPerfectMatch ?? false,
                    Method = r.Request.Method,
                    StatusCode = r.Response.StatusCode is int val ? val : 0,
                    MappingId = mappingGuid,
                    Matches = matchModel?.MatchDetails.OfType<JObject>().Select(x =>
                    {
                        var v = x.ToObject<MatchJOBject>();
                        return new MatchInfo
                        {
                            Matched = v.Score > 0,
                            RuleName = v.Name
                        };
                    }).ToList() ?? new List<MatchInfo>()
                };
            }
            catch (Exception e)
            {
                return null;
            }
        }


        public int MappingTypeFilter
        {
            get => _mappingTypeFilter;
            set => this.RaiseAndSetIfChanged(ref _mappingTypeFilter, value);
        }

        private int _mappingTypeFilter;

        public string MappingSearchTerm
        {
            get => _mappingSearchTerm;
            set => this.RaiseAndSetIfChanged(ref _mappingSearchTerm, value);
        }

        private string _mappingSearchTerm;

        public int RequestTypeFilter
        {
            get => _requestTypeFilter;
            set => this.RaiseAndSetIfChanged(ref _requestTypeFilter, value);
        }

        private int _requestTypeFilter;
        
        private readonly ObservableAsPropertyHelper<IEnumerable<RequestViewModel>> _filteredRequests;
        public IEnumerable<RequestViewModel> FilteredRequests => _filteredRequests.Value;
        
        private readonly ObservableAsPropertyHelper<IEnumerable<MappingViewModel>> _filteredMappings;
        public IEnumerable<MappingViewModel> FilteredMappings => _filteredMappings.Value;

        private static MappingDetails GetMappingDetails(RequestViewModel req, MappingModel expectations)
        {
            var isPerfectMatch = req.Raw.RequestMatchResult?.IsPerfectMatch == true;
            return new MappingDetails
            {
                MatchingStatus = req.MatchingStatus,
                MappingId = req.MappingId?.ToString()??string.Empty,
                MappingAvailability = req switch
                {
                    {MappingId: null} => MappingAvailability.NotProvided,
                    {MappingId: {}} when expectations.Request == null => MappingAvailability.Missing,
                    _ => MappingAvailability.Found
                },
                RequestParts = new MatchDetailsList
                {
                    new()
                    {
                        RuleName = "ClientIP",
                        Matched = IsMatched(req, "ClientIPMatcher"),
                        ActualValue = new SimpleActualValue
                        {
                            Value = req.Raw.Request.ClientIP
                        },
                        Expectations = ExpectationsAsJson(expectations.Request?.ClientIP),
                        NoExpectations = expectations.Request?.ClientIP is null
                    },
                    new()
                    {
                        RuleName = "Method",
                        Matched = IsMatched(req, "MethodMatcher"),
                        ActualValue = new SimpleActualValue
                        {
                            Value = req.Raw.Request.Method
                        },
                        Expectations = ExpectationsAsJson(expectations.Request?.Methods),
                        NoExpectations = expectations.Request?.Methods is null
                    },
                    new()
                    {
                        RuleName = "Url",
                        Matched = IsMatched(req, "UrlMatcher"),
                        ActualValue = new SimpleActualValue
                        {
                            Value = req.Raw.Request.Url
                        },
                        Expectations = ExpectationsAsJson(expectations.Request?.Url),
                        NoExpectations = expectations.Request?.Url is null
                    },
                    new()
                    {
                        RuleName = "Path",
                        Matched = IsMatched(req, "PathMatcher"),
                        ActualValue = new SimpleActualValue
                        {
                            Value = req.Raw.Request.Path
                        },
                        Expectations = ExpectationsAsJson(expectations.Request?.Path),
                        NoExpectations = expectations.Request?.Path is null
                    },
                    new()
                    {
                        RuleName = "Headers",
                        Matched = IsMatched(req, "HeaderMatcher"),
                        ActualValue = new KeyValueListActualValue
                        {
                            Items = req.Raw.Request.Headers?.OrderBy(x=>x.Key).SelectMany(x=> x.Value.Select(v => new KeyValuePair<string,string>(x.Key, v))).ToList() ?? new List<KeyValuePair<string, string>>()
                        },
                        Expectations = ExpectationsAsJson(expectations.Request?.Headers),
                        NoExpectations = expectations.Request?.Headers is null
                    },
                    new()
                    {
                        RuleName = "Cookies",
                        Matched = IsMatched(req, "CookieMatcher"),
                        ActualValue = new KeyValueListActualValue
                        {
                            Items = req.Raw.Request.Cookies?.OrderBy(x=>x.Key).Select(x=>x).ToList() ?? new List<KeyValuePair<string, string>>()
                        },
                        Expectations = ExpectationsAsJson(expectations.Request?.Cookies),
                        NoExpectations = expectations.Request?.Cookies is null
                    },
                    new()
                    {
                        RuleName = "Query params",
                        Matched = IsMatched(req, "ParamMatcher"),
                        ActualValue = new KeyValueListActualValue
                        {
                            Items = req.Raw.Request.Query?.OrderBy(x=>x.Key).SelectMany(x=> x.Value.Select(v => new KeyValuePair<string,string>(x.Key, v))).ToList() ?? new List<KeyValuePair<string, string>>(),
                        },
                        Expectations = ExpectationsAsJson(expectations.Request?.Params),
                        NoExpectations = expectations.Request?.Params is null
                    },
                    new()
                    {
                        RuleName = "Body",
                        Matched = IsMatched(req, "BodyMatcher"),
                        ActualValue = new MarkdownActualValue
                        {
                            Value = req.Raw.Request.DetectedBodyType switch
                            {
                                "String" or "FormUrlEncoded" => WrapBodyInMarkdown(req.Raw.Request.Body?? string.Empty),
                                "Json" => AsMarkdownCode("json", req.Raw.Request.BodyAsJson?.ToString() ?? string.Empty),
                                "Bytes" => req.Raw.Request.BodyAsBytes?.ToString()?? string.Empty,
                                "File" => "[FileContent]",
                                _ => ""
                            }
                        },
                        Expectations = ExpectationsAsJson(expectations.Request?.Body),
                        NoExpectations = expectations.Request?.Body is null
                    }
                },
                ResponseParts = new MatchDetailsList
                {
                    new ()
                    {
                        RuleName = "Status code",
                        Matched = isPerfectMatch,
                        ActualValue = new SimpleActualValue
                        {
                            Value = req.Raw.Response?.StatusCode?.ToString()?? string.Empty
                        },
                        Expectations = expectations.Response?.StatusCode?.ToString()?? string.Empty
                    },
                    new ()
                    {
                        RuleName = "Headers",
                        Matched = isPerfectMatch,
                        ActualValue =  new KeyValueListActualValue
                        {
                            Items = req.Raw.Response?.Headers?.OrderBy(x=>x.Key).SelectMany(x=> x.Value.Select(v => new KeyValuePair<string,string>(x.Key, v))).ToList() ?? new List<KeyValuePair<string, string>>()
                        },
                        Expectations = ExpectationsAsJson(expectations.Response?.Headers)
                    },
                    new ()
                    {
                        RuleName = "Body",
                        Matched = isPerfectMatch,
                        ActualValue = new MarkdownActualValue
                        {
                            Value =  req.Raw.Response?.DetectedBodyType.ToString() switch
                            {
                                "String" or "FormUrlEncoded" => WrapBodyInMarkdown( req.Raw.Response.Body?? string.Empty),
                                "Json" => AsMarkdownCode("json",req.Raw.Response.BodyAsJson?.ToString() ?? string.Empty),
                                "Bytes" => req.Raw.Response.BodyAsBytes?.ToString()?? string.Empty,
                                "File" => "[FileContent]",
                                _ => ""
                            }
                        },
                        Expectations = expectations.Response switch
                        {
                            {Body: {} bodyResponse} => WrapBodyInMarkdown(bodyResponse), 
                            {BodyAsJson: {} bodyAsJson} => AsMarkdownCode("json", bodyAsJson.ToString()!),
                            {BodyAsBytes: {} bodyAsBytes} => bodyAsBytes.ToString()?? string.Empty,
                            {BodyAsFile: {} bodyAsFile} => bodyAsFile,
                            _ => ""
                        }
                    }
                }
            };
        }

        private static string WrapBodyInMarkdown(string bodyResponse)
        {
            var cleanBody = bodyResponse.Trim();
            if (cleanBody.StartsWith("[") || cleanBody.StartsWith("{"))
            {
                return AsMarkdownCode("json", bodyResponse);

            }
            if (cleanBody.StartsWith("<"))
            {
                return AsMarkdownCode("xml", bodyResponse);

            }
            return bodyResponse;
        }

        private static string AsMarkdownCode(string lang, string code) => $"```{lang}\r\n{code}\r\n```";

        public string RequestSearchTerm
        {
            get => _requestSearchTerm;
            set => this.RaiseAndSetIfChanged(ref _requestSearchTerm, value);
        }

        private string _requestSearchTerm;

        

        private static string ExpectationsAsJson(object? data)
        {
            if (data == null)
            {
                return string.Empty;
            }

            return AsMarkdownCode("json", JsonConvert.SerializeObject(data, Formatting.Indented));
        }

        private static bool? IsMatched(RequestViewModel req, string rule)
        {
            var candidates = req.Matches.Where(x=>x.RuleName == rule).ToArray();
            if (candidates.Length == 0)
            {
                return null;
            }
            return candidates.All(x=>x.Matched);
        }

        public ReactiveCommand<Unit, (IList<LogEntryModel> requests, IList<MappingModel> mappings)>  LoadRequestsCommand { get; }

        private readonly ObservableAsPropertyHelper<MappingDetails> _relatedMapping;
        public MappingDetails RelatedMapping => _relatedMapping.Value;
    }

    public class MappingDetails:ViewModelBase
    {
        public MatchingStatus MatchingStatus { get; set; }
        public MappingAvailability MappingAvailability { get; set; }
        public string MappingId { get; set; }
        public MatchDetailsList RequestParts { get; set; }
        public MatchDetailsList ResponseParts { get; set; }
    }

    public class MatchDetailsList: List<MatchDetailsViewModel>
    {
        
    }
    public class RequestViewModel:ViewModelBase
    {
        public MatchingStatus MatchingStatus { get; set; }
        public string Method { get; set; }
        public string Path { get; set; }
        public DateTime Timestamp { get; set; }
        public bool IsMatched { get; set; }
        public int StatusCode { get; set; }
        public List<MatchInfo> Matches { get; set; }
        public Guid? MappingId { get; set; }
        public LogEntryModel Raw { get; set; }
    }

    public class MatchJOBject
    {
        public string Name { get; set; }
        public double Score { get; set; }
    }

    public class MatchInfo
    {
        public string RuleName { get; set; }
        public bool Matched { get; set; }
    }


    public enum MatchingStatus
    {
        Unmatched,
        PartialMatch,
        PerfectMatch
    }

    public enum MappingAvailability
    {
        NotProvided,
        Missing,
        Found
    }

    public class MappingViewModel: ViewModelBase
    {
        public MappingModel Raw { get; set; }
        public string Id { get; set; }
        public DateTime? UpdatedOn { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Content { get; set; }

        public int PerfectHitCount { get; set; }
        public int PartialHitCount { get; set; }
        public MappingHitType HitType { get; set; }

        public string Code
        {
            get => _code;
            set => this.RaiseAndSetIfChanged(ref _code, value);
        }

        private string _code;
        
    }


    public enum MappingHitType
    {
        Unmatched,
        OnlyPartialMatch,
        PerfectMatch
    }
}