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

namespace WireMockAdminUI.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        public string AdminUrl
        {
            get => _adminUrl;
            set => this.RaiseAndSetIfChanged(ref _adminUrl, value);
        }

        private string _adminUrl;

        public ObservableCollection<RequestViewModel> Requests { get; } = new();

        public RequestViewModel? SelectedRequest
        {
            get => _selectedRequest;
            set => this.RaiseAndSetIfChanged(ref _selectedRequest, value);
        }

        private RequestViewModel? _selectedRequest;

        public MainWindowViewModel()
        {
            LoadRequestsCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                var api = RestClient.For<IWireMockAdminApi>(AdminUrl);
                return await api.GetRequestsAsync();
            }); 

            LoadRequestsCommand
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(x =>
                {
                    Requests.Clear();
                    Requests.AddRange(x.Select(r =>
                    {
                        var matchModel = r.PartialRequestMatchResult ?? r.RequestMatchResult;
                        var mappingGuid = r.PartialMappingGuid ?? r.MappingGuid;
                        return new RequestViewModel
                        {
                            Raw = r,
                            Path = r.Request.Url.Substring(AdminUrl.Length),
                            Timestamp = r.Request.DateTime,
                            IsMatched = matchModel?.IsPerfectMatch ?? false,
                            Method = r.Request.Method,
                            StatusCode = r.Response.StatusCode is int val ? val : 0,
                            MappingId = mappingGuid,
                            Matches = matchModel?.MatchDetails.OfType<JObject>().Select(x =>
                            {
                                var v = x.ToObject<MatchJOBject>();
                                return new MatchInfo()
                                {
                                    Matched = v.Score > 0,
                                    RuleName = v.Name
                                };
                            }).ToList() ?? new List<MatchInfo>()
                        };
                    }).OrderByDescending(x=>x.Timestamp));
                    RequestSearchTerm = string.Empty;
                });
            LoadRequestsCommand.ThrownExceptions.Subscribe(exception =>
            {

            });
            AdminUrl = "http://localhost:1080";
            
            this.WhenAnyValue(x => x.SelectedRequest)
                .OfType<RequestViewModel>()
                .SelectMany(async req =>
                {
                    var expectations = await GetExpectations(req, default);
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
        }

        public int RequestTypeFilter
        {
            get => _requestTypeFilter;
            set => this.RaiseAndSetIfChanged(ref _requestTypeFilter, value);
        }

        private int _requestTypeFilter;
        
        private readonly ObservableAsPropertyHelper<IEnumerable<RequestViewModel>> _filteredRequests;
        public IEnumerable<RequestViewModel> FilteredRequests => _filteredRequests.Value;

        private static MappingDetails GetMappingDetails(RequestViewModel req, MappingModel expectations)
        {
            var isPerfectMatch = req.Raw.RequestMatchResult?.IsPerfectMatch == true;
            return new MappingDetails
            {
                RequestParts = new MatchDetailsList
                {
                    new()
                    {
                        RuleName = "ClientIP",
                        Matched = IsMatched(req, "ClientIPMatcher"),
                        ActualValue = new SimpleActualValue()
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
                        ActualValue = new SimpleActualValue()
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
                        ActualValue = new SimpleActualValue()
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
                        ActualValue = new SimpleActualValue()
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
                        ActualValue = new KeyValueListActualValue()
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
                        ActualValue = new KeyValueListActualValue()
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
                        ActualValue = new KeyValueListActualValue()
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
                        ActualValue = new MarkdownActualValue()
                        {
                            Value = req.Raw.Request.DetectedBodyType switch
                            {
                                "String" or "FormUrlEncoded" => req.Raw.Request.Body?? string.Empty,
                                "Json" => $"```json\r\n{req.Raw.Request.BodyAsJson?.ToString() ?? string.Empty}\r\n```",
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
                        ActualValue = new SimpleActualValue()
                        {
                            Value = req.Raw.Response?.StatusCode?.ToString()?? string.Empty
                        },
                        Expectations = expectations.Response?.StatusCode?.ToString()?? string.Empty
                    },
                    new ()
                    {
                        RuleName = "Headers",
                        Matched = isPerfectMatch,
                        ActualValue =  new KeyValueListActualValue()
                        {
                            Items = req.Raw.Response?.Headers?.OrderBy(x=>x.Key).SelectMany(x=> x.Value.Select(v => new KeyValuePair<string,string>(x.Key, v))).ToList() ?? new List<KeyValuePair<string, string>>()
                        },
                        Expectations = ExpectationsAsJson(expectations.Response?.Headers)
                    },
                    new ()
                    {
                        RuleName = "Body",
                        Matched = isPerfectMatch,
                        ActualValue = new MarkdownActualValue()
                        {
                            Value =  req.Raw.Response?.DetectedBodyType.ToString() switch
                            {
                                "String" or "FormUrlEncoded" => req.Raw.Response.Body?? string.Empty,
                                "Json" => $"```json\r\n{req.Raw.Response.BodyAsJson?.ToString() ?? string.Empty}\r\n```",
                                "Bytes" => req.Raw.Response.BodyAsBytes?.ToString()?? string.Empty,
                                "File" => "[FileContent]",
                                _ => ""
                            }
                        },
                        Expectations = expectations.Response switch
                        {
                            {Body: {} bodyResponse} => WrapBodyInMarkdown(bodyResponse), 
                            {BodyAsJson: {} bodyAsJson} => $"```json\r\n{bodyAsJson.ToString() ?? string.Empty}\r\n```",
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
                return $"```json\r\n{bodyResponse}\r\n```";

            }
            if (cleanBody.StartsWith("<"))
            {
                return $"```xml\r\n{bodyResponse}\r\n```";

            }

            return bodyResponse;
        }

        public string RequestSearchTerm
        {
            get => _requestSearchTerm;
            set => this.RaiseAndSetIfChanged(ref _requestSearchTerm, value);
        }

        private string _requestSearchTerm;

        private async Task<MappingModel> GetExpectations(RequestViewModel req, CancellationToken c)
        {
            if (req.MappingId.HasValue)
            {
                try
                {
                    var api = RestClient.For<IWireMockAdminApi>(AdminUrl);
                    return await api.GetMappingAsync(req.MappingId.Value, c);
                }
                catch (Exception e)
                {
                    return new MappingModel();

                }
            }
            return new MappingModel();
        }

        private static string ExpectationsAsJson(object? data)
        {
            if (data == null)
            {
                return string.Empty;
            }

            return $"```json\r\n{JsonConvert.SerializeObject(data, Formatting.Indented)}\r\n```";
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

        public ReactiveCommand<Unit, IList<LogEntryModel>>  LoadRequestsCommand { get; }

        private readonly ObservableAsPropertyHelper<MappingDetails> _relatedMapping;
        public MappingDetails RelatedMapping => _relatedMapping.Value;
    }

    public class MappingDetails:ViewModelBase
    {
        public MatchDetailsList RequestParts { get; set; }
        public MatchDetailsList ResponseParts { get; set; }
    }

    public class MatchDetailsList: List<MatchDetailsViewModel>
    {
        
    }
    public class RequestViewModel:ViewModelBase
    {
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
}