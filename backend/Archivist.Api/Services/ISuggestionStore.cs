using Archivist.Api.Models;

namespace Archivist.Api.Services
{
    public interface ISuggestionStore
    {
        Task AddAsync(SampleSuggestion suggestion);
        Task<bool> ExistsAsync(string trackKey, string sampleKey);
        Task<List<SampleSuggestion>> ListAsync(SuggestionStatus status);
        Task<List<SampleSuggestion>> GetApprovedAsync(string trackKey);
        Task<bool> SetStatusAsync(Guid id, SuggestionStatus status);
    }
}