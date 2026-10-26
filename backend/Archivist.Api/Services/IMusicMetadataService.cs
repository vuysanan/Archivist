using Archivist.Api.Models;

namespace Archivist.Api.Services
{
    public interface IMusicMetadataService
    {
        // Return null when nothing matches, so the controller can answer 404.
        Task<TrackDetails?> GetTrackMetadataAsync(string query);
        Task<AlbumDetails?> GetAlbumMetadataAsync(string query);
    }
}