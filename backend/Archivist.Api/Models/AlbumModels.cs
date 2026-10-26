namespace Archivist.Api.Models
{
    public class AlbumDetails
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Artist { get; set; } = string.Empty;
        public string AlbumArt { get; set; } = string.Empty;
        public int Year { get; set; }
        public List<AlbumTrack> Tracks { get; set; } = new();
    }

    public class AlbumTrack
    {
        public int Number { get; set; }
        public string Title { get; set; } = string.Empty;
    }
}