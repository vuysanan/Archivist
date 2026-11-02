using Archivist.Api.Models;

namespace Archivist.Api.Services
{
    // Used when no database is configured (local development and tests). Data is lost on restart.
    public class InMemorySuggestionStore : ISuggestionStore
    {
        private readonly List<SampleSuggestion> _items = new();
        private readonly object _lock = new();

        public Task AddAsync(SampleSuggestion suggestion)
        {
            lock (_lock) _items.Add(suggestion);
            return Task.CompletedTask;
        }

        public Task<bool> ExistsAsync(string trackKey, string sampleKey)
        {
            lock (_lock)
                return Task.FromResult(_items.Any(i => i.TrackKey == trackKey && i.SampleKey == sampleKey
                                                       && i.Status != SuggestionStatus.Rejected));
        }

        public Task<List<SampleSuggestion>> ListAsync(SuggestionStatus status)
        {
            lock (_lock)
                return Task.FromResult(_items.Where(i => i.Status == status).OrderBy(i => i.CreatedAt).ToList());
        }

        public Task<List<SampleSuggestion>> GetApprovedAsync(string trackKey)
        {
            lock (_lock)
                return Task.FromResult(_items.Where(i => i.TrackKey == trackKey && i.Status == SuggestionStatus.Approved).ToList());
        }

        public Task<bool> SetStatusAsync(Guid id, SuggestionStatus status)
        {
            lock (_lock)
            {
                var item = _items.FirstOrDefault(i => i.Id == id);
                if (item is null) return Task.FromResult(false);
                item.Status = status;
                return Task.FromResult(true);
            }
        }
    }
}