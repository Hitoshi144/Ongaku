using Microsoft.EntityFrameworkCore;
using Ongaku.Data;
using Ongaku.Models;

namespace Ongaku.Services {
    public class PlaylistService {
        private readonly IDbContextFactory<OngakuContext> _contextFactory;
        private readonly IWebHostEnvironment _environment;

        public Action<Track, int>? OnTrackAdded;
        public Action<Track, int>? OnTrackDeleted;
        public Action<int>? OnPlaylistDeleted;
        public Action<int, string>? OnEditName;

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

                existingTracks.Reverse();

                var playlistTracks = existingTracks.Select((track, index) =>
                    new PlaylistTrack
                    {
                        PlaylistId = playlist.Id,
                        TrackId = track.Id,
                        Order = index,
                    }).ToList();

                await _context.PlaylistTracks.AddRangeAsync(playlistTracks);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<List<Playlist>> GetAllPlaylistsAsync()
        {
            using var _context = _contextFactory.CreateDbContext();
            return await _context.Playlists.Include(p => p.PlaylistTracks.OrderBy(pt => pt.Order)).ThenInclude(pt => pt.Track).ThenInclude(t => t!.Artist).AsQueryable().ToListAsync();
        }

        public async Task<Playlist?> GetPlaylist(string id)
        {
            using var _context = _contextFactory.CreateDbContext();
            return await _context.Playlists.Include(p => p.PlaylistTracks.OrderBy(pt => pt.Order)).ThenInclude(pt => pt.Track).ThenInclude(t => t!.Artist).FirstOrDefaultAsync(pt => pt.Id == Convert.ToInt32(id));
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
         
            OnPlaylistDeleted?.Invoke(playlist.Id);

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

                await NormalizeOrderOnTrackDelete(playlist);

                OnTrackDeleted?.Invoke(track, playlist.Id);
            }
        }

        public async Task AddTrack(Playlist playlist, Track track)
        {
            using var _context = _contextFactory.CreateDbContext();
            _context.Attach(playlist);

            int order = playlist.PlaylistTracks.Count();

            PlaylistTrack playlistTrack = new()
            {
                TrackId = track.Id,
                PlaylistId = playlist.Id,
                Order = order
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

            OnEditName?.Invoke(playlist.Id, name);

            await _context.SaveChangesAsync();
        }

        public async Task ChangeOrder(Playlist playlist, Track track, int newOrder)
        {
            using var _context = _contextFactory.CreateDbContext();
            _context.Attach(playlist);

            List<PlaylistTrack> playlistTracks = await _context.PlaylistTracks.Where(pt => pt.PlaylistId == playlist.Id).OrderBy(pt => pt.Order).ToListAsync();

            if (playlistTracks == null || playlistTracks.Count == 0) return;

            PlaylistTrack? changedTrack = playlistTracks.FirstOrDefault(pt => pt.TrackId == track.Id);

            if (changedTrack == null) return;

            int oldOrder = changedTrack.Order;
            if (oldOrder == newOrder) return;

            newOrder = Math.Max(0, Math.Min(newOrder, playlistTracks.Count - 1));
            foreach (var playlistTrack in playlistTracks)
            {
                if (playlistTrack.TrackId == track.Id)
                {
                    playlistTrack.Order = newOrder;
                }
                else if (oldOrder < newOrder)
                {
                    if (playlistTrack.Order > oldOrder && playlistTrack.Order <= newOrder)
                    {
                        playlistTrack.Order--;
                    }
                }
                else
                {
                    if (playlistTrack.Order >= newOrder && playlistTrack.Order < oldOrder)
                    {
                        playlistTrack.Order++;
                    }
                }
            }

            await _context.SaveChangesAsync();
        }

        public async Task NormalizeOrderOnTrackDelete(Playlist? playlist = null)
        {
            using var _context = _contextFactory.CreateDbContext();

            if (playlist != null)
            {
                var playlistTracks = await _context.PlaylistTracks
                    .Where(pt => pt.PlaylistId == playlist.Id)
                    .OrderBy(pt => pt.Order)
                    .ToListAsync();

                await NormalizeOrderForPlaylist(_context, playlistTracks);
            }
            else
            {
                var playlistIds = await _context.Playlists
                    .Select(p => p.Id)
                    .ToListAsync();

                foreach (var playlistId in playlistIds)
                {
                    var playlistTracks = await _context.PlaylistTracks
                        .Where(pt => pt.PlaylistId == playlistId)
                        .OrderBy(pt => pt.Order)
                        .ToListAsync();

                    await NormalizeOrderForPlaylist(_context, playlistTracks);
                }
            }
        }

        private async Task NormalizeOrderForPlaylist(DbContext context, List<PlaylistTrack> playlistTracks)
        {
            if (playlistTracks.Count == 0) return;

            bool needsNormalization = false;
            for (int i = 0; i < playlistTracks.Count; i++)
            {
                if (playlistTracks[i].Order != i)
                {
                    needsNormalization = true;
                    break;
                }
            }

            if (!needsNormalization) return;

            for (int i = 0; i < playlistTracks.Count; i++)
            {
                if (playlistTracks[i].Order != i)
                {
                    playlistTracks[i].Order = i;
                    context.Entry(playlistTracks[i]).Property(p => p.Order).IsModified = true;
                }
            }

            await context.SaveChangesAsync();
        }
    }
}
