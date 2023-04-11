using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using DynamicData;
using DynamicData.Binding;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ReactiveUI;
using RestEase;
using WireMock.Admin.Mappings;
using WireMock.Admin.Requests;
using WireMock.Client;

namespace WireMockAdminUI.ViewModels
{
    public static class DesignTimeData
    {
        public static MainWindowViewModel MainViewModel { get; set; } = new MainWindowViewModel()
        {
            Requests =
            {
                new RequestViewModel()
                {
                    Timestamp = DateTime.Now,
                    IsMatched = true,
                    Path = "/api/v1.0/weather?lat=10.99&lon=44.34",
                    Method = "POST",
                    StatusCode = 200
                },
                new RequestViewModel()
                {
                    Timestamp = DateTime.Now,
                    IsMatched = false,
                    Path = "/api/v1.0/weather?lat=10.99&lon=44.34",
                    Method = "GET",
                    StatusCode = 404
                },
            }
        };
    }

    public class MainWindowViewModel : ViewModelBase
    {
        public string AdminUrl
        {
            get => _adminUrl;
            set => this.RaiseAndSetIfChanged(ref _adminUrl, value);
        }

        private string _adminUrl;

        public ObservableCollection<RequestViewModel> Requests { get; } = new();

        public RequestViewModel SelectedRequest
        {
            get => _selectedRequest;
            set => this.RaiseAndSetIfChanged(ref _selectedRequest, value);
        }

        private RequestViewModel _selectedRequest;





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
                    Requests.AddRange(x.Select(r => new RequestViewModel
                    {
                        Raw = r,
                        Path = r.Request.Url.Substring(AdminUrl.Length),
                        Timestamp = r.Request.DateTime,
                        IsMatched = r.PartialRequestMatchResult?.IsPerfectMatch ?? false,
                        Method = r.Request.Method,
                        StatusCode = r.Response.StatusCode is int val ? val: 0,
                        MappingId = r.PartialMappingGuid,
                        Matches = r.PartialRequestMatchResult?.MatchDetails.OfType<JObject>().Select(x=>
                        {
                            var v = x.ToObject<MatchJOBject>();
                            return new MatchInfo()
                            {
                                Matched = v.Score >0,
                                RuleName = v.Name
                            };
                        }).ToList()?? new List<MatchInfo>()
                }).OrderByDescending(x=>x.Timestamp));
            });
            LoadRequestsCommand.ThrownExceptions.Subscribe(exception =>
            {

            });
            AdminUrl = "http://localhost:1080";

            LoadMatchedMappingCommand = ReactiveCommand.CreateFromTask(async (RequestViewModel req, CancellationToken c) =>
            {
                MappingModel expecations = null;
                if (req.MappingId.HasValue)
                {
                    var api = RestClient.For<IWireMockAdminApi>(AdminUrl);
                    var mappingData = await api.GetMappingAsync(req.MappingId.Value, c);
                    expecations = mappingData;
                }
                else
                {
                    expecations = new MappingModel();
                }
                
                return new MappingDetails
                {
                    RequestParts = new List<MatchDetailsViewModel>
                    {
                        new ClientIPMatchDetailsViewModel
                        {
                            Matched = IsMatched(req, "ClientIPMatcher"),
                            ActualValue = req.Raw.Request.ClientIP,
                            Expectations = AsJson(expecations.Request?.ClientIP)
                        },
                        new MethodMatchDetailsViewModel
                        {
                            Matched = IsMatched(req, "MethodMatcher"),
                            ActualValue = req.Raw.Request.Method,
                            Expectations = AsJson(expecations.Request?.Methods)
                        },
                        new UrlMatchDetailsViewModel
                        {
                            Matched = IsMatched(req, "UrlMatcher"),
                            ActualValue = req.Raw.Request.Url,
                            Expectations = AsJson(expecations.Request?.Url)
                        },
                        new PathMatchDetailsViewModel
                        {
                            Matched = IsMatched(req, "PathMatcher"),
                            ActualValue = req.Raw.Request.Path,
                            Expectations = AsJson(expecations.Request?.Path)
                        },
                        new HeaderMatchDetailsViewModel
                        {
                            Matched = IsMatched(req, "HeaderMatcher"),
                            ActualValues = req.Raw.Request.Headers?.OrderBy(x=>x.Key).SelectMany(x=> x.Value.Select(v => new KeyValuePair<string,string>(x.Key, v))).ToList() ?? new List<KeyValuePair<string, string>>(),
                            Expectations = AsJson(expecations.Request?.Headers)
                        },
                        new CookieMatchDetailsViewModel
                        {
                            Matched = IsMatched(req, "CookieMatcher"),
                            ActualValues = req.Raw.Request.Cookies?.OrderBy(x=>x.Key).Select(x=>x).ToList() ?? new List<KeyValuePair<string, string>>(),
                            Expectations = AsJson(expecations.Request?.Cookies)
                        },
                        new ParamMatchDetailsViewModel
                        {
                            Matched = IsMatched(req, "ParamMatcher"),
                            ActualValues = req.Raw.Request.Query?.OrderBy(x=>x.Key).SelectMany(x=> x.Value.Select(v => new KeyValuePair<string,string>(x.Key, v))).ToList() ?? new List<KeyValuePair<string, string>>(),
                            Expectations = AsJson(expecations.Request?.Params)
                        },
                        new BodyMatchDetailsViewModel
                        {
                            Matched = IsMatched(req, "BodyMatcher"),
                            ActualPayload = req.Raw.Request.DetectedBodyType switch
                            {
                                "String" or "FormUrlEncoded" => req.Raw.Request.Body?? string.Empty,
                                "Json" => $"```json\r\n{req.Raw.Request.BodyAsJson?.ToString() ?? string.Empty}\r\n```",
                                "Bytes" => req.Raw.Request.BodyAsBytes?.ToString()?? string.Empty,
                                "File" => "[FileContent]",
                                _ => ""
                            },
                            Expectations = AsJson(expecations.Request?.Body)
                        },
                        new ScenarioMatchDetailsViewModel()
                        {
                            Matched = IsMatched(req, "ScenarioAndStateMatcher")
                        }
                    }
                };
            });

            LoadMatchedMappingCommand
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(details => RelatedMapping = details);

            this.WhenAnyValue(x => x.SelectedRequest)
                .Where(x => x != null)               
                .InvokeCommand(LoadMatchedMappingCommand);
        }

        private static string AsJson(object? data)
        {
            return $"```json\r\n{JsonConvert.SerializeObject(data, Formatting.Indented)}\r\n```";
        }

        private static bool IsMatched(RequestViewModel req, string rule)
        {
            return req.Matches.Where(x=>x.RuleName == rule).All(x=>x.Matched);
        }

        public ReactiveCommand<Unit, IList<LogEntryModel>>  LoadRequestsCommand { get; }
        public ReactiveCommand<RequestViewModel, MappingDetails> LoadMatchedMappingCommand { get; }


        public MappingDetails RelatedMapping
        {
            get => _relatedMapping;
            set => this.RaiseAndSetIfChanged(ref _relatedMapping, value);
        }

        private MappingDetails _relatedMapping;






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




    public abstract class MatchGeneralViewModel:ViewModelBase
    {
        public abstract  string RuleName { get;}
        public bool Matched { get; set; }
    }   
    
    public abstract class MatchDetailsViewModel:ViewModelBase
    {
        public abstract string RuleName { get;}
        public bool Matched { get; set; }
        public string Expectations { get; set; }
    }

    public class BodyMatchDetailsViewModel: MatchDetailsViewModel
    {
        public string ActualPayload { get; set; }
        public string ActualContentType { get; set; }

        public override string RuleName => "Body";
    }


    public class ClientIPMatchDetailsViewModel : MatchDetailsViewModel
    {

        public string ActualValue { get; set; }
        public override string RuleName => "ClientIP";
    }

    public class CookieMatchDetailsViewModel : MatchDetailsViewModel
    {
        public List<KeyValuePair<string, string>> ActualValues { get; set; }
        public override string RuleName => "Cookie";
    }

    public class HeaderMatchDetailsViewModel : MatchDetailsViewModel
    {
        public List<KeyValuePair<string, string>> ActualValues { get; set; }
        public override string RuleName => "Headers";
    }

    public class MethodMatchDetailsViewModel : MatchDetailsViewModel
    {
        public string ActualValue { get; set; }
        public override string RuleName => "Method";
    }

    public class ParamMatchDetailsViewModel : MatchDetailsViewModel
    {
        public List<KeyValuePair<string,string>> ActualValues { get; set; }
        public override string RuleName => "Query Params";
    }

    public class PathMatchDetailsViewModel : MatchDetailsViewModel
    {
        public string ActualValue { get; set; }
        public override string RuleName => "Path";
    }

    public class ScenarioMatchDetailsViewModel : MatchDetailsViewModel
    {
        public override string RuleName => "Scenario";
    }

    public class UrlMatchDetailsViewModel : MatchDetailsViewModel
    {
        public string ActualValue { get; set; }
        public override string RuleName => "Url";
    }
}