using System;
using System.Collections.Generic;
using System.Linq;
using WireMock.Admin.Requests;

namespace WireMockInspector.ViewModels;

public class MappingHitCalculator
{
    private readonly Dictionary<Guid, List<LogEntryModel>> _perfectMatchMappings;
    private readonly Dictionary<Guid, List<LogEntryModel>> _partialMatchMappings;

    public MappingHitCalculator(IList<LogEntryModel> requests) 
    {
        _perfectMatchMappings = requests.Where(x => x.MappingGuid.HasValue)
            .GroupBy(x => x.MappingGuid.Value)
            .ToDictionary(x => x.Key, x => x.OrderByDescending(x=>x.Request.DateTime).ToList());
        _partialMatchMappings = requests.Where(x => x.PartialMappingGuid.HasValue && x.MappingGuid != x.PartialMappingGuid)
            .GroupBy(x => x.PartialMappingGuid.Value)
            .ToDictionary(x => x.Key, x => x.OrderByDescending(x=>x.Request.DateTime).ToList());
    }

    public int GetPerfectHitCount(Guid? mappingId)
    {
        if(mappingId.HasValue && _perfectMatchMappings.TryGetValue(mappingId.Value, out var requests))
            return requests.Count;
        return 0;
    }
    
    public bool HasPerfectHitCountAfter(Guid? mappingId, DateTime? after)
    {
        if(after.HasValue && mappingId.HasValue && _perfectMatchMappings.TryGetValue(mappingId.Value, out var requests))
            return requests.Any(x=>x.Request.DateTime >= after);
        return false;
    }
    
    public DateTime? GetLastPerfectHit(Guid? mappingId)
    {
        if(mappingId.HasValue && _perfectMatchMappings.TryGetValue(mappingId.Value, out var requests))
            return requests.FirstOrDefault()?.Request.DateTime;
        return null;
    }
            
    public int GetPartialHitCount(Guid? mappingId)
    {
        if(mappingId.HasValue && _partialMatchMappings.TryGetValue(mappingId.Value, out var requests))
            return requests.Count;
        return 0;
    }
}