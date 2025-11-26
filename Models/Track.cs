namespace Ongaku.Models {
    public class Track {
        public int Id { get; set; }
        public required string Title { get; set; }
        public required int ArtistId { get; set; }
        public required Artist Artist { get; set; }
        public required string FilePath { get; set; }
        public string? CoverPath { get; set; }
        public TimeSpan Duration { get; set; }
        public ICollection<PlaylistTrack>? PlaylistTracks { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
