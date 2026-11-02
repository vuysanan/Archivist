using Archivist.Api.Models;
using Archivist.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace Archivist.Api.Data
{
    // Saves suggestions in the Postgres database.
    public class EfSuggestionStore : ISuggestionStore
    {
        private readonly ArchivistDbContext _db;

        public EfSuggestionStore(ArchivistDbContext db) => _db = db;

        public async Task AddAsync(SampleSuggestion suggestion)
        {
            _db.Suggestions.Add(suggestion);
            await _db.SaveChangesAsync();
        }

        public Task<bool> ExistsAsync(string trackKey, string sampleKey) =>
            _db.Suggestions.AnyAsync(x => x.TrackKey == trackKey && x.SampleKey == sampleKey
                                          && x.Status != SuggestionStatus.Rejected);

        public Task<List<SampleSuggestion>> ListAsync(SuggestionStatus status) =>
            _db.Suggestions.Where(x => x.Status == status).OrderBy(x => x.CreatedAt).ToListAsync();

        public Task<List<SampleSuggestion>> GetApprovedAsync(string trackKey) =>
            _db.Suggestions.Where(x => x.TrackKey == trackKey && x.Status == SuggestionStatus.Approved).ToListAsync();

        public async Task<bool> SetStatusAsync(Guid id, SuggestionStatus status)
        {
            var item = await _db.Suggestions.FindAsync(id);
            if (item is null) return false;
            item.Status = status;
            await _db.SaveChangesAsync();
            return true;
        }
    }
}
