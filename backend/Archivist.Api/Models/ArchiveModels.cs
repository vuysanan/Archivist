namespace Archivist.Api.Models
{
    public class TrackDetails
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Artist { get; set; }
        public string AlbumArt { get; set; }
        public int Year { get; set; }
        public int Bpm { get; set; }
        public Credits Credits { get; set; }
        public List<Sample> Samples { get; set; }
    }

    public class Credits
    {
        public string LeadProducer { get; set; }
        public List<string> CoProducers { get; set; }
        public List<string> Writers { get; set; }
        public string MixingEngineer { get; set; }
    }

    public class Sample
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Artist { get; set; }
        public string Year { get; set; }
        public string Type { get; set; } // e.g., "Direct Sample", "Interpolation", "Vocal Extraction"
    }
}