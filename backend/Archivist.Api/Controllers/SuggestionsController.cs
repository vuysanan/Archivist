using Archivist.Api.Models;
using Archivist.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Archivist.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SuggestionsController : ControllerBase
    {
        private readonly ISuggestionStore _store;

        public SuggestionsController(ISuggestionStore store)
        {
            _store = store;
        }

        // Anyone can suggest a sample. It is saved as "Pending" and stays hidden until an admin approves it.
        [HttpPost]
        [EnableRateLimiting("suggest")]
        public async Task<IActionResult> Create(SuggestionRequest request)
        {
            // People leave the hidden "website" box empty. If it's filled in, act as if it worked but save nothing.
            if (!string.IsNullOrWhiteSpace(request.Website)) return Accepted();

            var trackKey = TextUtil.Key(request.TrackArtist, request.TrackTitle);
            var sampleKey = TextUtil.Key(request.SampleArtist, request.SampleTitle);

            // Already suggested (or already approved)? No need to store it twice.
            if (await _store.ExistsAsync(trackKey, sampleKey)) return Accepted();

            await _store.AddAsync(new SampleSuggestion
            {
                TrackKey = trackKey,
                SampleKey = sampleKey,
                TrackTitle = request.TrackTitle.Trim(),
                TrackArtist = request.TrackArtist.Trim(),
                SampleTitle = request.SampleTitle.Trim(),
                SampleArtist = request.SampleArtist.Trim(),
                SampleYear = request.SampleYear ?? "",
                Type = request.Type
            });

            return Accepted();
        }
    }
}