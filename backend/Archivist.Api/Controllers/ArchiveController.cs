using Microsoft.AspNetCore.Mvc;
using Archivist.Api.Models;
using Archivist.Api.Services;

namespace Archivist.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ArchiveController : ControllerBase
    {
        private readonly IMusicMetadataService _metadataService;

        public ArchiveController(IMusicMetadataService metadataService)
        {
            _metadataService = metadataService;
        }

        [HttpGet("search")]
        public async Task<ActionResult<TrackDetails>> SearchTrack([FromQuery] string q)
        {
            if (string.IsNullOrWhiteSpace(q))
                return BadRequest("Search query cannot be empty.");

            var result = await _metadataService.GetTrackMetadataAsync(q);
            return result is null ? NotFound() : Ok(result);
        }

        [HttpGet("album")]
        public async Task<ActionResult<AlbumDetails>> SearchAlbum([FromQuery] string q)
        {
            if (string.IsNullOrWhiteSpace(q))
                return BadRequest("Search query cannot be empty.");

            var result = await _metadataService.GetAlbumMetadataAsync(q);
            return result is null ? NotFound() : Ok(result);
        }

        // A fresh 30-second preview link for the play button on the Sample Tree.
        [HttpGet("preview")]
        public async Task<IActionResult> Preview([FromQuery] string title, [FromQuery] string? artist = null)
        {
            if (string.IsNullOrWhiteSpace(title))
                return BadRequest("Title cannot be empty.");

            var url = await _metadataService.GetPreviewUrlAsync(title, artist ?? "");
            return url is null ? NotFound() : Ok(new { url });
        }
    }
}