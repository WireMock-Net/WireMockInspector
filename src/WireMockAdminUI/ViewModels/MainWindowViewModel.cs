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
            });
            LoadRequestsCommand.ThrownExceptions.Subscribe(exception =>
            {

            });
            AdminUrl = "http://localhost:1080";

            LoadMatchedMappingCommand = ReactiveCommand.CreateFromTask(async (RequestViewModel req, CancellationToken c) =>
            {
                var expectations = await GetExpectations(req, c);
                return GetMappingDetails(req, expectations);
            });

            LoadMatchedMappingCommand
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(details => RelatedMapping = details);

            this.WhenAnyValue(x => x.SelectedRequest)
                .OfType<RequestViewModel>()
                .InvokeCommand(LoadMatchedMappingCommand);
        }

        private static MappingDetails GetMappingDetails(RequestViewModel req, MappingModel expectations)
        {
            return new MappingDetails
            {
                RequestParts = new List<MatchDetailsViewModel>
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
                }
            };
        }

        private async Task<MappingModel> GetExpectations(RequestViewModel req, CancellationToken c)
        {
            if (req.MappingId.HasValue)
            {
                var api = RestClient.For<IWireMockAdminApi>(AdminUrl);
                return await api.GetMappingAsync(req.MappingId.Value, c);
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

        private static bool IsMatched(RequestViewModel req, string rule)
        {
            return req.Matches.Where(x=>x.RuleName == rule).All(x=>x.Matched);
        }

        public ReactiveCommand<Unit, IList<LogEntryModel>>  LoadRequestsCommand { get; }
        public ReactiveCommand<RequestViewModel, MappingDetails> LoadMatchedMappingCommand { get; }


        public MappingDetails? RelatedMapping
        {
            get => _relatedMapping;
            set => this.RaiseAndSetIfChanged(ref _relatedMapping, value);
        }

        private MappingDetails? _relatedMapping;
    }

    public class MappingDetails:ViewModelBase
    {
        public List<MatchDetailsViewModel> RequestParts { get; set; }
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