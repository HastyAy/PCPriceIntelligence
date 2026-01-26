using Domain.Entities;
using Domain.Enums;
using System.Collections.Concurrent;

namespace web.Services;

public class BuildStateService
{
    // Use concurrent dictionary for thread safety
    private static readonly ConcurrentDictionary<string, (Dictionary<ComponentType, Component> components, ParsedQuery query, DateTime timestamp)> _sessions = new();

    /// <summary>
    /// Store components found from AI search with a session ID
    /// </summary>
    public string SetSearchResults(List<Component> components, ParsedQuery query)
    {
        var sessionId = Guid.NewGuid().ToString("N");
        var componentDict = components.ToDictionary(c => c.Type, c => c);

        _sessions[sessionId] = (componentDict, query, DateTime.UtcNow);

        // Clean up old sessions (older than 1 hour)
        CleanupOldSessions();

        return sessionId;
    }

    /// <summary>
    /// Get and clear the stored components using session ID
    /// </summary>
    public (Dictionary<ComponentType, Component>? components, ParsedQuery? query) GetAndClearSearchResults(string sessionId)
    {
        if (string.IsNullOrEmpty(sessionId))
            return (null, null);

        if (_sessions.TryRemove(sessionId, out var data))
        {
            return (data.components, data.query);
        }

        return (null, null);
    }

    private void CleanupOldSessions()
    {
        var cutoff = DateTime.UtcNow.AddHours(-1);
        var oldSessions = _sessions.Where(kvp => kvp.Value.timestamp < cutoff).Select(kvp => kvp.Key).ToList();

        foreach (var sessionId in oldSessions)
        {
            _sessions.TryRemove(sessionId, out _);
        }
    }
}