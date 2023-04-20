using System;
using System.Collections.Generic;
using System.Linq;
using WireMock.Admin.Requests;

namespace WireMockInspector.ViewModels;

public class MappingHitCalculator
{
    private readonly Dictionary<Guid, int> _perfectMatchMappings;
    private readonly Dictionary<Guid, int> _partialMatchMappings;

    public MappingHitCalculator(IList<LogEntryModel> requests) 
    {
        _perfectMatchMappings = requests.Select(x => x.MappingGuid).Where(x => x.HasValue).GroupBy(x => x.Value).ToDictionary(x => x.Key, x => x.Count());
        _partialMatchMappings = requests.Where(x => x.PartialMappingGuid.HasValue && x.MappingGuid != x.PartialMappingGuid)
            .Select(x => x.PartialMappingGuid)
            .GroupBy(x => x.Value)
            .ToDictionary(x => x.Key, x => x.Count());
    }

    public int GetPerfectHitCount(Guid? mappingId)
    {
        if(mappingId.HasValue && _perfectMatchMappings.TryGetValue(mappingId.Value, out var count))
            return count;
        return 0;
    }
            
    public int GetPartialHitCount(Guid? mappingId)
    {
        if(mappingId.HasValue && _partialMatchMappings.TryGetValue(mappingId.Value, out var count))
            return count;
        return 0;
    }
}