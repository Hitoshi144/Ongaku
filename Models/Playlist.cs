namespace Ongaku.Models {
    public class Playlist {
        public int Id { get; set; }
        public required string Name { get; set; }
        public ICollection<PlaylistTrack> PlaylistTracks { get; set; } = [];
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
