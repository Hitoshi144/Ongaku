namespace Ongaku.Models {
    public class PlaylistTrack {
        public int PlaylistId { get; set; }
        public Playlist? Playlist { get; set; }

        public int TrackId { get; set; }
        public required Track Track { get; set; }
    }
}
