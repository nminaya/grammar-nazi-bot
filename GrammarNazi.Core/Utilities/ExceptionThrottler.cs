
namespace GrammarNazi.Core.Utilities;

public class ExceptionThrottler
{
    private static readonly object _lock = new();
    private static readonly Dictionary<string, ThrottleState> _states = new(StringComparer.Ordinal);
    private const int MaxEntries = 250;

    /// <summary>
    /// Determines whether an exception with the given key should be reported based on window and burst threshold.
    /// Note: Suppression is best-effort once the internal states dictionary reaches maximum capacity (250 entries).
    /// </summary>
    public static bool ShouldReport(string key, TimeSpan window, int threshold = 1)
    {
        lock (_lock)
        {
            var now = DateTime.UtcNow;

            // Evict expired states or oldest entries if capacity reached
            if (_states.Count >= MaxEntries && !_states.ContainsKey(key))
            {
                var keysToRemove = _states
                    .Where(kvp => kvp.Value.RecentOccurrences.All(o => now - o > kvp.Value.Window))
                    .Select(kvp => kvp.Key)
                    .ToList();

                if (keysToRemove.Count == 0)
                {
                    keysToRemove = _states
                        .OrderBy(kvp => kvp.Value.RecentOccurrences.LastOrDefault())
                        .Take(_states.Count - (int)(MaxEntries * 0.8))
                        .Select(kvp => kvp.Key)
                        .ToList();
                }

                foreach (var k in keysToRemove)
                {
                    _states.Remove(k);
                }
            }

            if (!_states.TryGetValue(key, out var state))
            {
                state = new ThrottleState(window);
                _states[key] = state;
            }
            else
            {
                state.Window = window;
            }

            if (threshold <= 1)
            {
                state.RecentOccurrences.RemoveAll(x => now - x > state.Window);
                if (state.RecentOccurrences.Count == 0)
                {
                    state.RecentOccurrences.Add(now);
                    return true;
                }
                return false;
            }
            else
            {
                state.RecentOccurrences.Add(now);
                state.RecentOccurrences.RemoveAll(x => now - x > state.Window);

                if (state.RecentOccurrences.Count >= threshold)
                {
                    state.RecentOccurrences.Clear();
                    return true;
                }
                return false;
            }
        }
    }

    public static void ResetForTesting()
    {
        lock (_lock)
        {
            _states.Clear();
        }
    }

    private class ThrottleState(TimeSpan window)
    {
        public TimeSpan Window { get; set; } = window;
        public List<DateTime> RecentOccurrences { get; } = [];
    }
}
