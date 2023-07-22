using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using DynamicData;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ReactiveUI;
using RestEase;
using WireMock.Admin.Mappings;
using WireMock.Admin.Requests;
using WireMock.Admin.Scenarios;
using WireMock.Admin.Settings;
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
        public ObservableCollection<Scenario> Scenarios { get;  } = new();

        public Scenario SelectedScenario
        {
            get => _selectedScenario;
            set => this.RaiseAndSetIfChanged(ref _selectedScenario, value);
        }

        private Scenario _selectedScenario;

        
        private readonly ObservableAsPropertyHelper<IEnumerable<Scenario>> _filteredScenarios;
        public IEnumerable<Scenario> FilteredScenarios => _filteredScenarios.Value;

        public string ScenarioTermFilter
        {
            get => _scenarioTermFilter;
            set => this.RaiseAndSetIfChanged(ref _scenarioTermFilter, value);
        }

        public int ScenarioTypeFilter
        {
            get => _scenarioTypeFilter;
            set => this.RaiseAndSetIfChanged(ref _scenarioTypeFilter, value);
        }

        private int _scenarioTypeFilter;




        private string _scenarioTermFilter;

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



        private SearchHelper _requestSearcher = new ();

        private readonly ObservableAsPropertyHelper<NewVersionInfoViewModel> _newVersion;
        public NewVersionInfoViewModel NewVersion => _newVersion.Value;


        private GithubUpdater _githubUpdater = new GithubUpdater("cezarypiatek/WireMockInspector");
        private WireMockInspectorSettingsManager _settingsManager = new();

        public ObservableCollection<SettingsWrapper> Settings { get; set; } = new();

        public class Node
        {
            public string Id { get; set; }
            public double X { get; set; }
            public double Y { get; set; }
            public double Width { get; set; }
            public double Height { get; set; }
        }

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

            SaveServerSettings = ReactiveCommand.CreateFromTask(async () =>
            {
                if (serverSettings.ProxyAndRecordSettings.ReplaceSettings is { OldValue: null })
                {
                    serverSettings.ProxyAndRecordSettings.ReplaceSettings = null;
                }

                try
                {
                    var api = RestClient.For<IWireMockAdminApi>(AdminUrl);
                    await api.PutSettingsAsync(serverSettings);
                }
                finally
                {
                    serverSettings.ProxyAndRecordSettings.ReplaceSettings ??= new();
                }
                
            });

            LoadRequestsCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                var api = RestClient.For<IWireMockAdminApi>(AdminUrl);
                var requestsTask = api.GetRequestsAsync();
                var mappingsTask = api.GetMappingsAsync();
                var settingsTask = api.GetSettingsAsync();
                var scenariosTask = api.GetScenariosAsync();
               
                await Task.WhenAll(requestsTask, mappingsTask, settingsTask, scenariosTask).ConfigureAwait(false);

                foreach (var request in requestsTask.Result)
                {
                    request.Response.StatusCode ??= 200;
                }
                
                return (requests: requestsTask.Result, mappings: mappingsTask.Result, settings: settingsTask.Result, scenarios: scenariosTask.Result);
            }); 

            LoadRequestsCommand
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(x =>
                {
                    
                    Settings.Clear();
                    serverSettings = x.settings;
                    serverSettings.ProxyAndRecordSettings ??= new ProxyAndRecordSettingsModel();
                    serverSettings.ProxyAndRecordSettings.WebProxySettings ??= new WebProxySettingsModel();
                    serverSettings.ProxyAndRecordSettings.ReplaceSettings ??= new ProxyUrlReplaceSettingsModel();
                    Settings.AddRange(MapToSettingsWrappers(serverSettings));


                    var requests = x.requests.Select(MapRequestData).OfType<RequestViewModel>().OrderByDescending(x => x.Timestamp).ToList();
                    SelectedRequest = null;
                    Requests.Clear();
                    Requests.AddRange(requests);
                    RequestSearchTerm = string.Empty;
                    _requestSearcher.Load(requests);
                    Mappings.Clear();

                    var hitCalculator = new MappingHitCalculator(x.requests);
                    var scenarios = x.scenarios.GroupBy(x => x.Name).ToDictionary(x => x.Key, x => x.FirstOrDefault());
                    
                    var enrichedScenarios = x.mappings.Where(m => string.IsNullOrWhiteSpace(m.Scenario) == false)
                        .GroupBy(n => n.Scenario)
                        .ToDictionary(m => m.Key, mappingsFromScenario =>
                            {
                                scenarios.TryGetValue(mappingsFromScenario.Key, out var state);
                                var estimatedScenarioStateDate = CalculateEstimatedScenarioStateDate(state, mappingsFromScenario, hitCalculator);


                                var transitions = mappingsFromScenario.Select(m =>
                                {
                                    var hit = hitCalculator.HasPerfectHitAfter(m.Guid, estimatedScenarioStateDate);
                                    var fromNode = m.WhenStateIs is null
                                        ? new ScenarioEdgeNode(Guid.NewGuid().ToString(), state?.Started == false || (state is {Started: true, NextState: null, Counter: > 0, Finished: false} && hit), hit)
                                        : new ScenarioNode(m.WhenStateIs);
                                    
                                    var toNode = m.SetStateTo is null
                                        ? new ScenarioEdgeNode(Guid.NewGuid().ToString(), state?.Finished == true && hit, false)
                                        : new ScenarioNode(m.SetStateTo);
                                    
                                    return new ScenarioTransition
                                    (
                                        Id: m.Guid.ToString(),
                                        From: fromNode,
                                        To: toNode,
                                        Hit: hit
                                    )
                                    {
                                        LastHit = hitCalculator.GetFirstPerfectHitDateAfter(m.Guid, estimatedScenarioStateDate ?? DateTime.MinValue),
                                        Description = $"[{m.WhenStateIs}] -> [{m.SetStateTo}]",
                                        MappingDefinition =AsMarkdownCode("json", JsonConvert.SerializeObject(m, Formatting.Indented)).AsMarkdownSyntax(),
                                        TriggeredBy = hitCalculator.GetPerfectHitCountAfter(m.Guid, estimatedScenarioStateDate)
                                            .OrderBy(x=>x.Request.DateTime)
                                            .Select(l=> new ScenarioTransitionLogEntry
                                            {
                                                Timestamp = l.Request.DateTime,
                                                Description = $"{l.Request.Method} {l.Request.Path}",
                                                RequestDefinition = AsMarkdownCode("json", JsonConvert.SerializeObject(l.Request, Formatting.Indented)),
                                                ResponseDefinition = AsMarkdownCode("json", JsonConvert.SerializeObject(l.Response, Formatting.Indented)),
                                            }  )
                                            .ToList()
                                    };
                                }).OrderBy(x=> x.LastHit??DateTime.MaxValue).ToList();
                                return new Scenario
                                ( 
                                    CurrentTransitionId: "", 
                                    Transitions: transitions, mappingsFromScenario.Key, 
                                    CurrentState:  state switch
                                    {
                                        {Started: false} => "[Not Started]",
                                        {Finished: true} => "[Finished]",
                                        {Started: true, NextState: null, Counter: > 0} => "[Started]",
                                        not null => state.NextState,
                                        _ => ""
                                    },
                                    ThisMappingTransition: ""
                                )
                                {
                                    
                                    Status = (state?.Started, state?.Finished) switch
                                    {
                                        (_, true) => ScenarioStatus.Finished,    
                                        (true, false) => ScenarioStatus.Started,
                                        _ => ScenarioStatus.NotStarted
                                    }
                                }; 
                            });
                    Scenarios.Clear();
                    Scenarios.AddRange(enrichedScenarios.Values);
                    SelectedScenario = null;
                    
                    var mappings = x.mappings.Select(model =>
                    {
                        var partialHitCount = hitCalculator.GetPartialHitCount(model.Guid);
                        var perfectHitCount = hitCalculator.GetPerfectHitCount(model.Guid);
                        var mappingId = model.Guid?.ToString();
                        return new MappingViewModel()
                        {
                            Raw = model,
                            Id = mappingId,
                            Title = model.Title,
                            DescriptiveName = GetDescriptiveName(model),
                            Description = model.Title != model.Description? model.Description: null,
                            UpdatedOn = model.UpdatedAt,
                            Content = AsMarkdownCode("json", JsonConvert.SerializeObject(model, Formatting.Indented)).AsMarkdownSyntax(),
                            PartialHitCount = partialHitCount,
                            PerfectHitCount = perfectHitCount,
                            HitType = (perfectHitCount, partialHitCount) switch
                            {
                                ( > 0, _) => MappingHitType.PerfectMatch,
                                (_, >0) => MappingHitType.OnlyPartialMatch,
                                _ => MappingHitType.Unmatched
                            },
                            Scenario = enrichedScenarios.TryGetValue(model.Scenario, out var scenario)? scenario with {CurrentTransitionId = mappingId, ThisMappingTransition = $"[{model.WhenStateIs}] -> [{model.SetStateTo}]"}: null
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
                        try
                        {
                            var foundIds = _requestSearcher.Search(x.term);
                            if (foundIds.Any())
                            {
                                result = result.Where(x => foundIds.Contains(x.Raw.Guid.ToString()));
                            }
                            else
                            {
                                result = result.Where(el => el.Path.Contains(x.term, StringComparison.InvariantCultureIgnoreCase));
                            }

                        }
                        catch (Exception e)
                        {
                            result = result.Where(el => el.Path.Contains(x.term, StringComparison.InvariantCultureIgnoreCase));
                        }
                    }

                    return x.type switch
                    {
                        1 => result.Where(x => x.IsMatched),
                        2 => result.Where(x => x.IsMatched == false),
                        3 => result.Where(x => x.Title?.Contains("Proxy Mapping on") == true),
                        _ => result,
                    };
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
                        4 => result.Where(x => x.Title?.Contains("Proxy Mapping for") == true),
                        _ => result,
                    } ;
                })
                .ObserveOn(RxApp.MainThreadScheduler)
                .ToProperty(this, x => x.FilteredMappings, out _filteredMappings);
            
            this.WhenAnyValue(x => x.ScenarioTermFilter, x=>x.ScenarioTypeFilter, (term, type) => (term,  type))
                .Throttle(TimeSpan.FromMilliseconds(200))
                .DistinctUntilChanged()
                .Select(x =>
                {
                    IEnumerable<Scenario> result = Scenarios;
                    if (string.IsNullOrWhiteSpace(x.term) == false)
                    {
                        result = result.Where(el => el.ScenarioName.Contains(x.term, StringComparison.InvariantCultureIgnoreCase) == true);
                    }

                    return x.type switch
                    {
                        1 => result.Where(x=>x.Status == ScenarioStatus.NotStarted),
                        2 => result.Where(x => x.Status == ScenarioStatus.Started),
                        3 => result.Where(x => x.Status == ScenarioStatus.Finished),
                        _ => result,
                    } ;
                })
                .ObserveOn(RxApp.MainThreadScheduler)
                .ToProperty(this, x => x.FilteredScenarios, out _filteredScenarios);

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
                    SelectedMapping.Code = AsMarkdownCode("cs", code).AsMarkdownSyntax();
                });
        }

        private static DateTime? CalculateEstimatedScenarioStateDate(ScenarioStateModel? state, IGrouping<string?, MappingModel> mappingsFromScenario, MappingHitCalculator hitCalculator)
        {
            DateTime? estimatedScenarioStateDate = null;
            DateTime? lastHitExitPointDate = null;
            if (state is {Started: true})
            {
                var lastHits =  mappingsFromScenario.SelectMany(x=> hitCalculator.GetLPerfectHitDates(x.Guid).Select(d=>
                        new {id = x.Guid, date = d})).OrderByDescending(x=>x.date)
                    .ToList();
               var entryPoints = mappingsFromScenario.Where(x => x.WhenStateIs == null);
            
               if (entryPoints.MaxBy(x => hitCalculator.GetLastPerfectHitDate(x.Guid)) is { } latestEntryPoint)
               {
                   var lastEntryDate = hitCalculator.GetLastPerfectHitDate(latestEntryPoint.Guid);
                   var lastHitsEnumerator = lastHits.AsEnumerable();
                   var lastBeforeLastEntry = new {id = (Guid?) null, date = DateTime.MinValue};
                   var counterForEntryRequests = 0;
                   foreach (var lt in lastHitsEnumerator)
                   {
                       if (lt.date >= lastEntryDate && lt.id != latestEntryPoint.Guid)
                       {
                           continue;
                       }

                       if (lt.id == latestEntryPoint.Guid)
                       {
                           counterForEntryRequests++;
                           lastBeforeLastEntry = lt;
                           //INFO: In case when scenario was previously reset on the first step. This won't work correctly when current state is after entry point.
                           //       It would be better if the state could have ScenarioStartDate.
                           if (state.NextState == null && state.Finished == false)
                           {
                               if (counterForEntryRequests == state.Counter)
                               {
                                   break;
                               }
                           }
                       }
                       else
                       {
                           break;
                       }
                   }
                   
                   if (lastBeforeLastEntry.id != null)
                   {
                       estimatedScenarioStateDate = hitCalculator.GetFirstPerfectHitDateAfter(latestEntryPoint.Guid, lastBeforeLastEntry.date);
                   }
                   else
                   {
                       estimatedScenarioStateDate = hitCalculator.GetFirstPerfectHitDateAfter(latestEntryPoint.Guid, DateTime.MinValue);
                   }
               }
            }

            return estimatedScenarioStateDate;
        }

        private string GetDescriptiveName(MappingModel model)
        {
            var sb = new StringBuilder();
           
            if (model.Request.Methods is {Length: > 0} methods)
            {
                sb.Append(string.Join(" | ", methods));
                
            }
            else
            {
                sb.Append("<any method>");
            }
            sb.Append(": ");
            
            var pathDefined = false;
            
            {
                if (model.Request.Url is JObject jo)
                {
                    if (jo.TryGetValue("Matchers", out var matchers) && matchers is JArray arr)
                    {
                        var patterns = arr.OfType<JObject>().Select(x => x.TryGetValue("Pattern", out var p) ? p.ToString() : null)
                            .OfType<string>();
                        sb.Append(string.Join(" | ", patterns));
                        pathDefined = true;
                    }
                }
            }
            {
                if (model.Request.Path is JObject jo)
                {
                    if (jo.TryGetValue("Matchers", out var matchers) && matchers is JArray arr)
                    {
                        var patterns = arr.OfType<JObject>().Select(x => x.TryGetValue("Pattern", out var p) ? p.ToString() : null)
                            .OfType<string>();
                        sb.Append(string.Join(" | ", patterns));
                        pathDefined = true;
                    }
                }
            }
            if (pathDefined == false)
            {
                sb.Append("<any path>");
            }
            return sb.ToString();
        }

        public ReactiveCommand<Unit, Unit> SaveServerSettings { get; set; }

        private static IEnumerable<SettingsWrapper> MapToSettingsWrappers(object serverSettings, string? namePrefix = null)
        {
            foreach (var propertyInfo in serverSettings.GetType().GetProperties().OrderBy(x => x.Name))
            {
                var property = propertyInfo;
                if ((property.PropertyType.IsPrimitive ==false) && property.PropertyType.IsEnum == false && property.PropertyType.Namespace?.StartsWith("System") == false && property.GetValue(serverSettings) is {} obj)
                {
                    foreach (var wrapper in MapToSettingsWrappers(obj, namePrefix: $"{namePrefix}.{property.Name}".Trim('.')))
                    {
                        yield return wrapper;
                    }
                    continue;
                }

                yield return new SettingsWrapper(serverSettings, property, namePrefix);
            }

            
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
                    Title = r switch
                    {
                        {RequestMatchResult.IsPerfectMatch: true} => r.MappingTitle,
                        {PartialRequestMatchResult: { }} => r.PartialMappingTitle,
                        _ => null
                    },
                    Path = r.Request.Url.Substring(AdminUrl.Length),
                    Timestamp = r.Request.DateTime,
                    IsMatched = matchModel?.IsPerfectMatch ?? false,
                    Method = r.Request.Method,
                    StatusCode = r.Response.StatusCode?.ToString() ?? "200" ,
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
            var statusCode = req.Raw.Response?.StatusCode?.ToString()?? string.Empty;
            var statusCodeFormatted = $"{statusCode} ({HttpStatusCodeToDescriptionConverter.TranslateStatusCode(statusCode)})";
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
                                "Bytes" => new Markdown("plaintext", req.Raw.Request.BodyAsBytes?.ToString()?? string.Empty),
                                "File" => new Markdown("plaintext","[FileContent]"),
                                _ => new Markdown("plaintext", "")
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
                            Value = statusCodeFormatted
                        },
                        Expectations = ExpectationsAsJson(expectations.Response?.StatusCode?.ToString())
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
                            Value =  GetActualForRequestBody(req)
                        },
                        Expectations = expectations.Response switch
                        {
                            {Body: {} bodyResponse} => WrapBodyInMarkdown(bodyResponse), 
                            {BodyAsJson: {} bodyAsJson} => new Markdown("json", bodyAsJson.ToString()!),
                            {BodyAsBytes: {} bodyAsBytes} =>  new Markdown("plaintext", bodyAsBytes.ToString()?? string.Empty),
                            {BodyAsFile: {} bodyAsFile} =>  new Markdown("plaintext",bodyAsFile),
                            _ => new Markdown("plaintext",string.Empty)
                        }
                    }
                }
            };
        }

        private static Markdown GetActualForRequestBody(RequestViewModel req)
        {
            return req.Raw.Response?.DetectedBodyType.ToString() switch
            {
                "Json" => new Markdown("json",req.Raw.Response.BodyAsJson?.ToString() ?? string.Empty),
                "Bytes" => new Markdown("plaintext", req.Raw.Response.BodyAsBytes?.ToString()?? string.Empty),
                "File" => new Markdown("plaintext",req.Raw.Response.BodyAsFile?.ToString() ?? string.Empty),
                _ => WrapBodyInMarkdown( req.Raw.Response?.Body?? string.Empty),
            };
        }

        private static Markdown WrapBodyInMarkdown(string bodyResponse)
        {
            var cleanBody = bodyResponse.Trim();
            if (cleanBody.StartsWith("[") || cleanBody.StartsWith("{"))
            {
                return new Markdown("json", bodyResponse);

            }
            if (cleanBody.StartsWith("<"))
            {
                return new Markdown("xml", bodyResponse);

            }
            return new Markdown("plaintext", bodyResponse);
        }

        public static Markdown AsMarkdownCode(string lang, string code) => new Markdown(lang, code);

        public string RequestSearchTerm
        {
            get => _requestSearchTerm;
            set => this.RaiseAndSetIfChanged(ref _requestSearchTerm, value);
        }

        private string _requestSearchTerm;

        

        private static Markdown ExpectationsAsJson(object? data)
        {
            if (data == null)
            {
                return  new Markdown("plaintext", string.Empty);
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

        public ReactiveCommand<Unit, (IList<LogEntryModel> requests, IList<MappingModel> mappings, SettingsModel settings, IList<ScenarioStateModel> scenarios)> LoadRequestsCommand { get; }

        private readonly ObservableAsPropertyHelper<MappingDetails> _relatedMapping;
        private SettingsModel serverSettings;
        public MappingDetails RelatedMapping => _relatedMapping.Value;
    }

    public class ScenarioTransitionLogEntry
    {
        public DateTime Timestamp { get; set; }
        public string Description { get; set; }
        public Markdown RequestDefinition { get; set; }
        public Markdown ResponseDefinition { get; set; }
    }

    public record ScenarioNode(string Id);
    public record ScenarioEdgeNode(string Id, bool Current, bool Hit):ScenarioNode(Id);

    public record ScenarioTransition(string Id, ScenarioNode From, ScenarioNode To, bool Hit)
    {
        public string Description { get; set; }
        public string MappingDefinition { get; set; }
        public List<ScenarioTransitionLogEntry> TriggeredBy { get; set; }
        public DateTime? LastHit { get; set; }
    };

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
        public string StatusCode { get; set; }
        public List<MatchInfo> Matches { get; set; }
        public Guid? MappingId { get; set; }
        public LogEntryModel Raw { get; set; }
        public string? Title { get; set; }
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
        public string DescriptiveName { get; set; }
        public DateTime? UpdatedOn { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
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

        public Scenario Scenario { get; set; }
        
    }

    public record Scenario(string CurrentTransitionId, IReadOnlyList<ScenarioTransition> Transitions,
        string ScenarioName, string? CurrentState, string ThisMappingTransition)
    {
        public ScenarioStatus Status { get; set; }
    };

    public enum ScenarioStatus
    {
        NotStarted,
        Started,
        Finished
    }

    public enum MappingHitType
    {
        Unmatched,
        OnlyPartialMatch,
        PerfectMatch
    }
}