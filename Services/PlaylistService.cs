using Microsoft.EntityFrameworkCore;
using Ongaku.Data;
using Ongaku.Models;

namespace Ongaku.Services {
    public class PlaylistService {
        private readonly IDbContextFactory<OngakuContext> _contextFactory;
        private readonly IWebHostEnvironment _environment;

        public Action<Track, int>? OnTrackAdded;
        public Action<Track, int>? OnTrackDeleted;

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
            return await _context.Playlists.Include(p => p.PlaylistTracks).ThenInclude(pt => pt.Track).ThenInclude(t => t!.Artist).AsQueryable().ToListAsync();
        }

        public async Task<Playlist?> GetPlaylist(string id)
        {
            using var _context = _contextFactory.CreateDbContext();
            return await _context.Playlists.Include(p => p.PlaylistTracks).ThenInclude(pt => pt.Track).ThenInclude(t => t!.Artist).FirstOrDefaultAsync(pt => pt.Id == Convert.ToInt32(id));
        }

        public async Task DeletePlaylist(Playlist playlist)
        {
            using var _context = _contextFactory.CreateDbContext();
            List<PlaylistTrack> playlistTracks = await _context.PlaylistTracks.Where(pt => pt.PlaylistId == playlist.Id).ToListAsync();
            if (playlistTracks.Any())
            {
                _context.PlaylistTracks.RemoveRange(playlistTracks);
            }
            _context.Playlists.Remove(playlist);

            await _context.SaveChangesAsync();
        }

        public async Task DeleteTrack(Playlist playlist, Track track)
        {
            using var _context = _contextFactory.CreateDbContext();
            PlaylistTrack? _exists = await _context.PlaylistTracks.FirstOrDefaultAsync(pt => pt.TrackId == track.Id && pt.PlaylistId == playlist.Id);
            if (_exists != null)
            {
                _context.PlaylistTracks.Remove(_exists);
                await _context.SaveChangesAsync();

                OnTrackDeleted?.Invoke(track, playlist.Id);
            }
        }

        public async Task AddTrack(Playlist playlist, Track track)
        {
            using var _context = _contextFactory.CreateDbContext();
            _context.Attach(playlist);

            PlaylistTrack playlistTrack = new()
            {
                TrackId = track.Id,
                PlaylistId = playlist.Id,
            };
            playlist.PlaylistTracks.Add(playlistTrack);
            await _context.SaveChangesAsync();
            OnTrackAdded?.Invoke(track, playlist.Id);
        }

        public async Task EditName(Playlist playlist, string name)
        {
            using var _context = _contextFactory.CreateDbContext();
            _context.Attach(playlist);
            playlist.Name = name;
            await _context.SaveChangesAsync();
        }
    }
}
