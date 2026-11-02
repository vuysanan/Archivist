using System.ComponentModel.DataAnnotations;

namespace Archivist.Api.Models
{
    public enum SuggestionStatus { Pending, Approved, Rejected }

    // One community suggestion, kept as Pending until an admin approves it.
    public class SampleSuggestion
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string TrackKey { get; set; } = "";   // identifies the track the sample belongs to
        public string SampleKey { get; set; } = "";  // identifies the suggested sample (used to spot duplicates)
        public string TrackTitle { get; set; } = "";
        public string TrackArtist { get; set; } = "";
        public string SampleTitle { get; set; } = "";
        public string SampleArtist { get; set; } = "";
        public string SampleYear { get; set; } = "";
        public string Type { get; set; } = "Sample";
        public SuggestionStatus Status { get; set; } = SuggestionStatus.Pending;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    // What the "Suggest a sample" form sends.
    public class SuggestionRequest
    {
        [Required, StringLength(150)] public string TrackTitle { get; set; } = "";
        [Required, StringLength(150)] public string TrackArtist { get; set; } = "";
        [Required, StringLength(150)] public string SampleTitle { get; set; } = "";
        [Required, StringLength(150)] public string SampleArtist { get; set; } = "";
        [RegularExpression(@"^(\d{4})?$")] public string? SampleYear { get; set; }
        [RegularExpression("^(Sample|Interpolation)$")] public string Type { get; set; } = "Sample";

        // A hidden "honeypot" box. People never see it, so a filled-in value means a bot.
        public string? Website { get; set; }
    }
}