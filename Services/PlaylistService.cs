using Microsoft.EntityFrameworkCore;
using Ongaku.Data;
using Ongaku.Models;

namespace Ongaku.Services {
    public class PlaylistService {
        private readonly IDbContextFactory<OngakuContext> _contextFactory;
        private readonly IWebHostEnvironment _environment;

        public PlaylistService(IWebHostEnvironment env, IDbContextFactory<OngakuContext> context)
        {
            _contextFactory = context;
            _environment = env;
        }

        public async Task CreatePlaylistAsync(string name, List<Track>? tracks)
        {
            var playlist = new Playlist { Name = name };

            using var _context = _contextFactory.CreateDbContext();
            await _context.Playlists.AddAsync(playlist);
            await _context.SaveChangesAsync();

            if (tracks != null && tracks.Any())
            {
                var trackIds = tracks.Select(t => t.Id).ToList();

                var existingTracks = await _context.Tracks
                    .Where(t => trackIds.Contains(t.Id))
                    .ToListAsync();

                var playlistTracks = existingTracks.Select(track =>
                    new PlaylistTrack
                    {
                        PlaylistId = playlist.Id,
                        TrackId = track.Id
                    }).ToList();

                await _context.PlaylistTracks.AddRangeAsync(playlistTracks);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<List<Playlist>> GetAllPlaylistsAsync()
        {
            using var _context = _contextFactory.CreateDbContext();
            return await _context.Playlists.Include(p => p.PlaylistTracks).AsQueryable().ToListAsync();
        }
    }
}
