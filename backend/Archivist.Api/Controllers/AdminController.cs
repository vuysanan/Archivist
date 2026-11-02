using System.Security.Cryptography;
using System.Text;
using Archivist.Api.Models;
using Archivist.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Archivist.Api.Controllers
{
    // Review queue for community suggestions. Every request must send the secret admin key.
    [ApiController]
    [Route("api/admin/suggestions")]
    public class AdminController : ControllerBase
    {
        private readonly ISuggestionStore _store;
        private readonly IConfiguration _config;

        public AdminController(ISuggestionStore store, IConfiguration config)
        {
            _store = store;
            _config = config;
        }

        // No key configured = admin is switched off (and looks like it doesn't exist).
        private IActionResult? Authorize()
        {
            var expected = _config["Admin:Key"];
            if (string.IsNullOrEmpty(expected)) return NotFound();

            var given = Request.Headers["X-Admin-Key"].ToString();
            var ok = CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(given), Encoding.UTF8.GetBytes(expected));
            return ok ? null : Unauthorized();
        }

        [HttpGet]
        public async Task<IActionResult> List([FromQuery] SuggestionStatus status = SuggestionStatus.Pending)
        {
            if (Authorize() is { } denied) return denied;

            var items = await _store.ListAsync(status);
            return Ok(items.Select(s => new
            {
                s.Id,
                s.TrackTitle,
                s.TrackArtist,
                s.SampleTitle,
                s.SampleArtist,
                s.SampleYear,
                s.Type,
                s.CreatedAt
            }));
        }

        [HttpPost("{id:guid}/approve")]
        public Task<IActionResult> Approve(Guid id) => Decide(id, SuggestionStatus.Approved);

        [HttpPost("{id:guid}/reject")]
        public Task<IActionResult> Reject(Guid id) => Decide(id, SuggestionStatus.Rejected);

        private async Task<IActionResult> Decide(Guid id, SuggestionStatus status)
        {
            if (Authorize() is { } denied) return denied;
            return await _store.SetStatusAsync(id, status) ? NoContent() : NotFound();
        }
    }
}