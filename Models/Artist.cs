namespace Ongaku.Models {
    public class Artist {
        public int Id { get; set; }
        public required string Name { get; set; }
        public ICollection<Track>? Tracks { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
