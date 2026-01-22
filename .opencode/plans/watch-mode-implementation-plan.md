# Watch Mode Implementation Plan

## Overview

Add a new "Watch" feature to WireMockInspector that continuously monitors the WireMock server, accumulating data over time with additive merging logic. This preserves historical data (especially mappings) that may be deleted from the server.

## Current Architecture Summary

**Key Components:**
- `MainWindowViewModel.cs` - Contains `LoadRequestsCommand` that fetches data from WireMock Admin API
- `MainWindow.axaml` - UI with Admin URL textbox and Load button
- Data stored in `ObservableCollection<T>` properties: `Requests`, `Mappings`, `Scenarios`, `Settings`
- Uses `RestClient.For<IWireMockAdminApi>(AdminUrl)` to communicate with WireMock
- Data processing happens in the subscription to `LoadRequestsCommand`

**Data Identifiers (for merging):**
- Requests: Identified by `LogEntryModel.Guid` (unique per request)
- Mappings: Identified by `MappingModel.Guid` (unique per mapping)
- Scenarios: Identified by `ScenarioStateModel.Name` (unique per scenario)
- Settings: Completely replaced (no merging needed)

---

## Design Requirements

Based on user feedback:

1. **Watch Button is a Toggle** - clicking starts/stops watching
2. **Add Clear Button** - separate button to clear accumulated state
3. **Show Status Indicator** - visual feedback for connection state (Connecting, Connected, Reconnecting)
4. **200ms Polling Interval** - very responsive polling

### Merging Behavior

| Data Type | Merge Strategy |
|-----------|----------------|
| Settings | Complete replacement |
| Requests | Additive only - add new requests, never remove existing |
| Mappings | Add new, update existing (by GUID), never remove old |
| Scenarios | Add new, update existing (by name), never remove old |

---

## Implementation Details

### 1. Add Watch State Properties

**File:** `src/WireMockInspector/ViewModels/MainWindowViewModel.cs`

```csharp
// Watch mode state
private bool _isWatching;
public bool IsWatching
{
    get => _isWatching;
    set => this.RaiseAndSetIfChanged(ref _isWatching, value);
}

// Connection status for UI feedback
private WatchConnectionStatus _watchStatus;
public WatchConnectionStatus WatchStatus
{
    get => _watchStatus;
    set => this.RaiseAndSetIfChanged(ref _watchStatus, value);
}

private IDisposable? _watchSubscription;
private readonly TimeSpan _watchPollingInterval = TimeSpan.FromMilliseconds(200);
```

Add enum:
```csharp
public enum WatchConnectionStatus
{
    Disconnected,   // Not watching
    Connecting,     // Attempting to connect (never connected yet)
    Connected,      // Successfully connected
    Reconnecting    // Was connected, lost connection, trying to reconnect
}
```

---

### 2. Create Commands

**File:** `src/WireMockInspector/ViewModels/MainWindowViewModel.cs`

```csharp
// Toggle watch mode (start/pause)
public ReactiveCommand<Unit, Unit> WatchCommand { get; }

// Clear all accumulated data
public ReactiveCommand<Unit, Unit> ClearDataCommand { get; }
```

---

### 3. Implement WatchCommand (Toggle)

**File:** `src/WireMockInspector/ViewModels/MainWindowViewModel.cs`

```csharp
WatchCommand = ReactiveCommand.Create(() =>
{
    if (IsWatching)
    {
        StopWatching();
    }
    else
    {
        StartWatching();
    }
});

private void StartWatching()
{
    IsWatching = true;
    WatchStatus = WatchConnectionStatus.Connecting;
    
    _watchSubscription = Observable
        .Timer(TimeSpan.Zero, _watchPollingInterval)
        .SelectMany(_ => Observable.FromAsync(TryFetchDataAsync))
        .ObserveOn(RxApp.MainThreadScheduler)
        .Subscribe(
            result =>
            {
                if (result is null)
                {
                    // Connection failed
                    WatchStatus = DataLoaded 
                        ? WatchConnectionStatus.Reconnecting 
                        : WatchConnectionStatus.Connecting;
                    return;
                }
                
                WatchStatus = WatchConnectionStatus.Connected;
                var (requests, mappings, settings, scenarios) = result.Value;
                
                if (!DataLoaded)
                {
                    ProcessInitialData(requests, mappings, settings, scenarios);
                }
                else
                {
                    ProcessMergedData(requests, mappings, settings, scenarios);
                }
            },
            ex =>
            {
                WatchStatus = WatchConnectionStatus.Reconnecting;
            });
}

private void StopWatching()
{
    _watchSubscription?.Dispose();
    _watchSubscription = null;
    IsWatching = false;
    WatchStatus = WatchConnectionStatus.Disconnected;
}
```

---

### 4. Implement ClearDataCommand

**File:** `src/WireMockInspector/ViewModels/MainWindowViewModel.cs`

```csharp
ClearDataCommand = ReactiveCommand.Create(() =>
{
    // Clear all collections
    Requests.Clear();
    Mappings.Clear();
    Scenarios.Clear();
    Settings.Clear();
    
    // Reset selected items
    SelectedRequest = null;
    SelectedMapping = null;
    SelectedScenario = null;
    
    // Reset search terms
    RequestSearchTerm = string.Empty;
    MappingSearchTerm = string.Empty;
    ScenarioTermFilter = string.Empty;
    
    // Clear search index
    _requestSearcher.Load(new List<RequestViewModel>());
    
    // Hide data tabs
    DataLoaded = false;
    
    // If watching, reset status to Connecting
    if (IsWatching)
    {
        WatchStatus = WatchConnectionStatus.Connecting;
    }
});
```

---

### 5. Data Fetching Helper

**File:** `src/WireMockInspector/ViewModels/MainWindowViewModel.cs`

```csharp
private async Task<(IList<LogEntryModel> requests, IList<MappingModel> mappings, 
    SettingsModel settings, IList<ScenarioStateModel> scenarios)?> TryFetchDataAsync()
{
    try
    {
        var api = RestClient.For<IWireMockAdminApi>(AdminUrl);
        var requestsTask = api.GetRequestsAsync();
        var mappingsTask = api.GetMappingsAsync();
        var settingsTask = api.GetSettingsAsync();
        var scenariosTask = api.GetScenariosAsync();
        
        await Task.WhenAll(requestsTask, mappingsTask, settingsTask, scenariosTask)
            .ConfigureAwait(false);
        
        foreach (var request in requestsTask.Result)
        {
            request.Response.StatusCode ??= 200;
        }
        
        return (requestsTask.Result, mappingsTask.Result, 
                settingsTask.Result, scenariosTask.Result);
    }
    catch
    {
        return null;
    }
}
```

---

### 6. Refactor: Extract ProcessInitialData

**File:** `src/WireMockInspector/ViewModels/MainWindowViewModel.cs`

Move the existing code from `LoadRequestsCommand.Subscribe` into a new method:

```csharp
private void ProcessInitialData(IList<LogEntryModel> requests, IList<MappingModel> mappings, 
    SettingsModel settings, IList<ScenarioStateModel> scenarios)
{
    // === BEGIN: Existing code from LoadRequestsCommand.Subscribe (lines 183-304) ===
    
    Settings.Clear();
    serverSettings = settings;
    serverSettings.ProxyAndRecordSettings ??= new ProxyAndRecordSettingsModel();
    serverSettings.ProxyAndRecordSettings.WebProxySettings ??= new WebProxySettingsModel();
    serverSettings.ProxyAndRecordSettings.ReplaceSettings ??= new ProxyUrlReplaceSettingsModel();
    Settings.AddRange(MapToSettingsWrappers(serverSettings));

    var requestVMs = requests.Select(MapRequestData).OfType<RequestViewModel>()
        .OrderByDescending(x => x.Timestamp).ToList();
    SelectedRequest = null;
    Requests.Clear();
    Requests.AddRange(requestVMs);
    RequestSearchTerm = string.Empty;
    _requestSearcher.Load(requestVMs);
    Mappings.Clear();

    var hitCalculator = new MappingHitCalculator(requests);
    var scenarioStates = scenarios.GroupBy(x => x.Name)
        .ToDictionary(x => x.Key, x => x.FirstOrDefault());
    
    var enrichedScenarios = mappings.Where(m => string.IsNullOrWhiteSpace(m.Scenario) == false)
        .GroupBy(n => n.Scenario)
        .ToDictionary(m => m.Key, mappingsFromScenario =>
        {
            // ... existing scenario calculation logic ...
        });
    
    Scenarios.Clear();
    Scenarios.AddRange(enrichedScenarios.Values);
    SelectedScenario = null;
    
    var mappingVMs = mappings.Select(model =>
    {
        // ... existing mapping creation logic ...
    }).OfType<MappingViewModel>().OrderByDescending(x => x.UpdatedOn);
    
    Mappings.AddRange(mappingVMs);
    MappingSearchTerm = string.Empty;
    
    // === END: Existing code ===
    
    DataLoaded = true;
}
```

Then update `LoadRequestsCommand.Subscribe` to call this method:

```csharp
LoadRequestsCommand
    .ObserveOn(RxApp.MainThreadScheduler)
    .Subscribe(x =>
    {
        ProcessInitialData(x.requests, x.mappings, x.settings, x.scenarios);
    });
```

---

### 7. Implement ProcessMergedData

**File:** `src/WireMockInspector/ViewModels/MainWindowViewModel.cs`

```csharp
private void ProcessMergedData(IList<LogEntryModel> newRequests, IList<MappingModel> newMappings, 
    SettingsModel newSettings, IList<ScenarioStateModel> newScenarios)
{
    // 1. Settings: Complete replacement
    serverSettings = newSettings;
    serverSettings.ProxyAndRecordSettings ??= new ProxyAndRecordSettingsModel();
    serverSettings.ProxyAndRecordSettings.WebProxySettings ??= new WebProxySettingsModel();
    serverSettings.ProxyAndRecordSettings.ReplaceSettings ??= new ProxyUrlReplaceSettingsModel();
    Settings.Clear();
    Settings.AddRange(MapToSettingsWrappers(serverSettings));

    // 2. Merge requests (additive - only add new ones)
    var existingRequestGuids = Requests.Select(r => r.Raw.Guid).ToHashSet();
    var requestsToAdd = newRequests
        .Where(r => !existingRequestGuids.Contains(r.Guid))
        .Select(MapRequestData)
        .OfType<RequestViewModel>()
        .ToList();
    
    if (requestsToAdd.Count > 0)
    {
        foreach (var req in requestsToAdd)
        {
            Requests.Add(req);
        }
        
        // Re-sort by timestamp (descending)
        var sorted = Requests.OrderByDescending(x => x.Timestamp).ToList();
        Requests.Clear();
        Requests.AddRange(sorted);
        
        // Rebuild search index
        _requestSearcher.Load(Requests.ToList());
    }
    
    // 3. Calculate hit statistics with ALL accumulated requests
    var allRawRequests = Requests.Select(r => r.Raw).ToList();
    var hitCalculator = new MappingHitCalculator(allRawRequests);
    var scenarioStates = newScenarios.GroupBy(x => x.Name)
        .ToDictionary(x => x.Key, x => x.FirstOrDefault());
    
    // 4. Calculate enriched scenarios (for mapping Scenario property)
    var enrichedScenarios = CalculateEnrichedScenarios(newMappings, scenarioStates, hitCalculator);
    
    // 5. Merge mappings (add new, update existing, keep old)
    MergeMappings(newMappings, allRawRequests, hitCalculator, enrichedScenarios);
    
    // 6. Merge scenarios (add new, update existing, keep old)
    MergeScenarios(enrichedScenarios);
}
```

---

### 8. Implement CalculateEnrichedScenarios

Extract the scenario calculation logic into a reusable method:

**File:** `src/WireMockInspector/ViewModels/MainWindowViewModel.cs`

```csharp
private Dictionary<string, Scenario> CalculateEnrichedScenarios(
    IList<MappingModel> mappings,
    Dictionary<string, ScenarioStateModel?> scenarioStates,
    MappingHitCalculator hitCalculator)
{
    return mappings
        .Where(m => string.IsNullOrWhiteSpace(m.Scenario) == false)
        .GroupBy(n => n.Scenario)
        .ToDictionary(m => m.Key!, mappingsFromScenario =>
        {
            scenarioStates.TryGetValue(mappingsFromScenario.Key!, out var state);
            var estimatedScenarioStateDate = CalculateEstimatedScenarioStateDate(
                state, mappingsFromScenario, hitCalculator);

            var transitions = mappingsFromScenario.Select(m =>
            {
                var hit = hitCalculator.HasPerfectHitAfter(m.Guid, estimatedScenarioStateDate);
                var fromNode = m.WhenStateIs is null
                    ? new ScenarioEdgeNode(Guid.NewGuid().ToString(), 
                        state?.Started == false || 
                        (state is {Started: true, NextState: null, Counter: > 0, Finished: false} && hit), hit)
                    : new ScenarioNode(m.WhenStateIs);
                
                var toNode = m.SetStateTo is null
                    ? new ScenarioEdgeNode(Guid.NewGuid().ToString(), state?.Finished == true && hit, false)
                    : new ScenarioNode(m.SetStateTo);
                
                return new ScenarioTransition(
                    Id: m.Guid.ToString()!,
                    From: fromNode,
                    To: toNode,
                    Hit: hit
                )
                {
                    LastHit = hitCalculator.GetFirstPerfectHitDateAfter(m.Guid, 
                        estimatedScenarioStateDate ?? DateTime.MinValue),
                    Description = $"[{m.WhenStateIs}] -> [{m.SetStateTo}]",
                    MappingDefinition = new MarkdownCode("json", 
                        JsonConvert.SerializeObject(m, Formatting.Indented)),
                    TriggeredBy = new RequestLogViewModel()
                    {
                        MapToLogEntries(hitCalculator.GetPerfectHitCountAfter(m.Guid, estimatedScenarioStateDate))
                    }
                };
            }).OrderBy(x => x.LastHit ?? DateTime.MaxValue).ToList();
            
            return new Scenario(
                CurrentTransitionId: "", 
                Transitions: transitions, 
                mappingsFromScenario.Key!, 
                CurrentState: state switch
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
}
```

---

### 9. Implement MergeMappings

**File:** `src/WireMockInspector/ViewModels/MainWindowViewModel.cs`

```csharp
private void MergeMappings(
    IList<MappingModel> newMappings,
    IList<LogEntryModel> allRawRequests,
    MappingHitCalculator hitCalculator,
    Dictionary<string, Scenario> enrichedScenarios)
{
    var existingByGuid = Mappings.ToDictionary(m => m.Raw.Guid);
    
    foreach (var model in newMappings)
    {
        var mappingVm = CreateMappingViewModel(model, allRawRequests, hitCalculator, enrichedScenarios);
        
        if (existingByGuid.TryGetValue(model.Guid, out var existing))
        {
            // Update existing mapping in place
            var index = Mappings.IndexOf(existing);
            if (index >= 0)
            {
                Mappings[index] = mappingVm;
            }
        }
        else
        {
            // Add new mapping
            Mappings.Add(mappingVm);
        }
    }
    
    // Re-sort by UpdatedOn
    var sorted = Mappings.OrderByDescending(x => x.UpdatedOn).ToList();
    Mappings.Clear();
    Mappings.AddRange(sorted);
    
    // NOTE: Mappings NOT in newMappings are intentionally KEPT
}

private MappingViewModel CreateMappingViewModel(
    MappingModel model,
    IList<LogEntryModel> allRawRequests,
    MappingHitCalculator hitCalculator,
    Dictionary<string, Scenario> enrichedScenarios)
{
    var partialHitCount = hitCalculator.GetPartialHitCount(model.Guid);
    var perfectHitCount = hitCalculator.GetPerfectHitCount(model.Guid);
    var mappingId = model.Guid?.ToString();
    
    return new MappingViewModel
    {
        Raw = model,
        Id = mappingId,
        Title = model.Title,
        ExpectedMethods = model.Request.Methods ?? Array.Empty<string>(),
        ExpectedPaths = GetExpectedPathsDescription(model),
        Description = model.Title != model.Description ? model.Description : null,
        UpdatedOn = model.UpdatedAt,
        Content = new MarkdownCode("json", JsonConvert.SerializeObject(model, Formatting.Indented)),
        PartialHitCount = partialHitCount,
        PerfectHitCount = perfectHitCount,
        PerfectMatches = new RequestLogViewModel
        {
            MapToLogEntries(allRawRequests
                .Where(x => x.MappingGuid == model.Guid)
                .OrderByDescending(x => x.Request.DateTime))
        },
        PartialMatches = new RequestLogViewModel
        {
            MapToLogEntries(allRawRequests
                .Where(x => x.PartialMappingGuid == model.Guid && x.MappingGuid != model.Guid)
                .OrderByDescending(x => x.Request.DateTime))
        },
        HitType = (perfectHitCount, partialHitCount) switch
        {
            (> 0, _) => MappingHitType.PerfectMatch,
            (_, > 0) => MappingHitType.OnlyPartialMatch,
            _ => MappingHitType.Unmatched
        },
        Scenario = model.Scenario is { } scenarioName 
            && enrichedScenarios.TryGetValue(scenarioName, out var scenario)
            ? scenario with 
            { 
                CurrentTransitionId = mappingId, 
                ThisMappingTransition = $"[{model.WhenStateIs}] -> [{model.SetStateTo}]" 
            }
            : null
    };
}
```

---

### 10. Implement MergeScenarios

**File:** `src/WireMockInspector/ViewModels/MainWindowViewModel.cs`

```csharp
private void MergeScenarios(Dictionary<string, Scenario> newScenarios)
{
    var existingByName = Scenarios.ToDictionary(s => s.ScenarioName);
    
    foreach (var (name, scenario) in newScenarios)
    {
        if (existingByName.TryGetValue(name, out var existing))
        {
            // Update existing scenario
            var index = Scenarios.IndexOf(existing);
            if (index >= 0)
            {
                Scenarios[index] = scenario;
            }
        }
        else
        {
            // Add new scenario
            Scenarios.Add(scenario);
        }
    }
    
    // NOTE: Scenarios NOT in newScenarios are intentionally KEPT
}
```

---

### 11. Create WatchStatusConverter

**File:** `src/WireMockInspector/ViewModels/WatchStatusConverter.cs` (NEW FILE)

```csharp
using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace WireMockInspector.ViewModels;

public class WatchStatusConverter : IValueConverter
{
    public static readonly WatchStatusConverter IsConnecting = new(WatchConnectionStatus.Connecting);
    public static readonly WatchStatusConverter IsConnected = new(WatchConnectionStatus.Connected);
    public static readonly WatchStatusConverter IsReconnecting = new(WatchConnectionStatus.Reconnecting);
    
    private readonly WatchConnectionStatus _targetStatus;
    
    private WatchStatusConverter(WatchConnectionStatus targetStatus)
    {
        _targetStatus = targetStatus;
    }
    
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is WatchConnectionStatus status)
        {
            return status == _targetStatus;
        }
        return false;
    }
    
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
```

---

### 12. Add Pause Icon to Styles

**File:** `src/WireMockInspector/Styles.axaml`

Add after the existing icons (around line 20):

```xml
<StreamGeometry x:Key="pause_circle_regular">M12,2 C17.5228,2 22,6.47715 22,12 C22,17.5228 17.5228,22 12,22 C6.47715,22 2,17.5228 2,12 C2,6.47715 6.47715,2 12,2 Z M12,3.5 C7.30558,3.5 3.5,7.30558 3.5,12 C3.5,16.6944 7.30558,20.5 12,20.5 C16.6944,20.5 20.5,16.6944 20.5,12 C20.5,7.30558 16.6944,3.5 12,3.5 Z M10.25,8 C10.6642,8 11,8.33579 11,8.75 L11,15.25 C11,15.6642 10.6642,16 10.25,16 C9.83579,16 9.5,15.6642 9.5,15.25 L9.5,8.75 C9.5,8.33579 9.83579,8 10.25,8 Z M13.75,8 C14.1642,8 14.5,8.33579 14.5,8.75 L14.5,15.25 C14.5,15.6642 14.1642,16 13.75,16 C13.3358,16 13,15.6642 13,15.25 L13,8.75 C13,8.33579 13.3358,8 13.75,8 Z</StreamGeometry>
```

---

### 13. Update MainWindow.axaml

**File:** `src/WireMockInspector/Views/MainWindow.axaml`

Replace lines 46-54 with:

```xml
<StackPanel>
  <views:NewVersionInfo DataContext="{Binding NewVersion}" IsVisible="{Binding IsVisible}"></views:NewVersionInfo>
  <Grid ColumnDefinitions="*, Auto, Auto, Auto, Auto" >
    <TextBox MinWidth="1" Text="{Binding AdminUrl, Mode=TwoWay}" 
             Watermark="Admin API URL" Name="AdminUrlCtr" ></TextBox>
    
    <!-- Load button (one-time fetch) -->
    <Button Grid.Column="1" Command="{Binding LoadRequestsCommand}" 
            Margin="10,0,0,0" ToolTip.Tip="Load requests (one-time)">
      <PathIcon Data="{StaticResource arrow_sync_circle_regular}" 
                HorizontalAlignment="Center" VerticalAlignment="Center" ></PathIcon>
    </Button>
    
    <!-- Watch toggle button -->
    <Button Grid.Column="2" Command="{Binding WatchCommand}" 
            Margin="5,0,0,0" ToolTip.Tip="Watch server (continuous polling)">
      <Panel>
        <PathIcon Data="{StaticResource play_circle_regular}" 
                  IsVisible="{Binding !IsWatching}" 
                  HorizontalAlignment="Center" VerticalAlignment="Center" ></PathIcon>
        <PathIcon Data="{StaticResource pause_circle_regular}" 
                  Foreground="#3baced" 
                  IsVisible="{Binding IsWatching}" 
                  HorizontalAlignment="Center" VerticalAlignment="Center" ></PathIcon>
      </Panel>
    </Button>
    
    <!-- Clear data button -->
    <Button Grid.Column="3" Command="{Binding ClearDataCommand}" 
            Margin="5,0,0,0" ToolTip.Tip="Clear accumulated data">
      <PathIcon Data="{StaticResource dismiss_circle_regular}" 
                HorizontalAlignment="Center" VerticalAlignment="Center" ></PathIcon>
    </Button>
    
    <!-- Connection status indicator -->
    <StackPanel Grid.Column="4" Orientation="Horizontal" Margin="10,0,0,0" 
                VerticalAlignment="Center" IsVisible="{Binding IsWatching}">
      <!-- Connecting indicator -->
      <StackPanel Orientation="Horizontal" 
                  IsVisible="{Binding WatchStatus, 
                  Converter={x:Static viewModels:WatchStatusConverter.IsConnecting}}">
        <PathIcon Data="{StaticResource arrow_sync_circle_regular}" 
                  Foreground="Orange" Width="16" Height="16" />
        <TextBlock Text="Connecting..." Foreground="Orange" 
                   Margin="5,0,0,0" FontSize="11" />
      </StackPanel>
      
      <!-- Connected indicator -->
      <StackPanel Orientation="Horizontal"
                  IsVisible="{Binding WatchStatus, 
                  Converter={x:Static viewModels:WatchStatusConverter.IsConnected}}">
        <PathIcon Data="{StaticResource checkmark_circle_regular}" 
                  Foreground="LimeGreen" Width="16" Height="16" />
        <TextBlock Text="Connected" Foreground="LimeGreen" 
                   Margin="5,0,0,0" FontSize="11" />
      </StackPanel>
      
      <!-- Reconnecting indicator -->
      <StackPanel Orientation="Horizontal"
                  IsVisible="{Binding WatchStatus, 
                  Converter={x:Static viewModels:WatchStatusConverter.IsReconnecting}}">
        <PathIcon Data="{StaticResource question_circle_regular}" 
                  Foreground="Orange" Width="16" Height="16" />
        <TextBlock Text="Reconnecting..." Foreground="Orange" 
                   Margin="5,0,0,0" FontSize="11" />
      </StackPanel>
    </StackPanel>
  </Grid>
</StackPanel>
```

---

## Summary of All Changes

| File | Type | Changes |
|------|------|---------|
| `ViewModels/MainWindowViewModel.cs` | Edit | Add `IsWatching`, `WatchStatus`, `WatchConnectionStatus` enum, `WatchCommand`, `ClearDataCommand`, extract `ProcessInitialData`, add `ProcessMergedData`, `TryFetchDataAsync`, `CalculateEnrichedScenarios`, `MergeMappings`, `CreateMappingViewModel`, `MergeScenarios` |
| `Views/MainWindow.axaml` | Edit | Add Watch button, Clear button, status indicator, update grid columns |
| `Styles.axaml` | Edit | Add `pause_circle_regular` icon |
| `ViewModels/WatchStatusConverter.cs` | New | Value converter for status indicator visibility |

---

## Implementation Order

1. Add `WatchConnectionStatus` enum to `MainWindowViewModel.cs`
2. Add `IsWatching` and `WatchStatus` properties to `MainWindowViewModel.cs`
3. Create `WatchStatusConverter.cs`
4. Add `pause_circle_regular` icon to `Styles.axaml`
5. Extract existing data processing into `ProcessInitialData` method
6. Update `LoadRequestsCommand.Subscribe` to call `ProcessInitialData`
7. Implement `TryFetchDataAsync` helper method
8. Implement `CalculateEnrichedScenarios` helper method
9. Implement `CreateMappingViewModel` helper method
10. Implement `MergeMappings` method
11. Implement `MergeScenarios` method
12. Implement `ProcessMergedData` method
13. Implement `StartWatching` and `StopWatching` methods
14. Implement `WatchCommand` in constructor
15. Implement `ClearDataCommand` in constructor
16. Update `MainWindow.axaml` with new UI elements
17. Build and test

---

## Testing Scenarios

1. **Start watching with unreachable server** - Should show "Connecting..." indefinitely
2. **Start watching with reachable server** - Should load data and show "Connected"
3. **Server becomes unreachable while watching** - Should show "Reconnecting..."
4. **Server comes back online** - Should resume merging and show "Connected"
5. **New requests appear on server** - Should be added to existing list
6. **New mappings appear on server** - Should be added to existing list
7. **Mappings are deleted on server** - Should remain in the UI (preserved)
8. **Mappings are updated on server** - Should be updated in the UI
9. **Click Watch to pause** - Should stop polling, keep accumulated data
10. **Click Clear** - Should remove all data, reset to initial state
11. **Click Load while watching** - Should work as one-time fetch (no interference)
