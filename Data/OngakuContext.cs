using Ongaku.Models;
using Microsoft.EntityFrameworkCore;

namespace Ongaku.Data {
    public class OngakuContext : DbContext {
        public OngakuContext(DbContextOptions<OngakuContext> options)
        : base(options) { }

        public DbSet<Track> Tracks { get; set; }
        public DbSet<Artist> Artists { get; set; }
        public DbSet<Playlist> Playlists { get; set; }
        public DbSet<PlaylistTrack> PlaylistTracks { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<PlaylistTrack>().HasKey(pt => new { pt.PlaylistId, pt.TrackId });

            base.OnModelCreating(modelBuilder);
        }
    }
}
