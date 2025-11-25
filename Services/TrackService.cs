using Microsoft.EntityFrameworkCore;
using MudBlazor;
using Ongaku.Data;
using Ongaku.Enums;
using Ongaku.Models;

namespace Ongaku.Services {
    public class TrackService {
        private readonly OngakuContext _context;

        public TrackService(OngakuContext context)
        {
            _context = context;
        }

        public async Task<List<Track>> GetAllTracksAsync(
            TrackSortBy sortBy = TrackSortBy.CreatedAt,
            SortOrder orderBy = SortOrder.Descending
            )
        {
            var query = _context.Tracks.Include(t => t.Artist).AsQueryable();

            query = sortBy switch
            {
                TrackSortBy.Title => orderBy == SortOrder.Ascending
                ? query.OrderBy(t => t.Title)
                : query.OrderByDescending(t => t.Title),

                TrackSortBy.Duration => orderBy == SortOrder.Ascending
                ? query.OrderBy(t => t.Duration)
                : query.OrderByDescending(t => t.Duration),

                TrackSortBy.Artist => orderBy == SortOrder.Ascending
                ? query.OrderBy(t => t.Artist)
                : query.OrderByDescending(t => t.Artist),

                TrackSortBy.CreatedAt => orderBy == SortOrder.Ascending
                ? query.OrderBy(t => t.CreatedAt)
                : query.OrderByDescending(t => t.CreatedAt),

                _ => query
            };

            return await query.ToListAsync();
        }

        public async Task<List<Track>> GetTracksByArtistAsync(
            int artistId
            )
        {
            return await _context.Tracks.Where(t => t.ArtistId == artistId).Include(t => t.Artist).ToListAsync();
        }

        public async Task AddTrackAsync(Track track)
        {
            _context.Tracks.Add(track);
            await _context.SaveChangesAsync();
        }

        public async Task<List<Track>> GetTracksByTitleAsync(string req)
        {
            return await _context.Tracks.Where(t => EF.Functions.ILike(t.Title, $"%{req}%")).ToListAsync();
        }

        public async Task<TimeSpan> GetMaxDurationAsync()
        {
            return await _context.Tracks.Select(t => t.Duration).DefaultIfEmpty(TimeSpan.Zero).MaxAsync();
        }

        public async Task<TimeSpan> GetMinDurationAsync()
        {
            return await _context.Tracks.Select(t => t.Duration).DefaultIfEmpty(TimeSpan.Zero).MinAsync();
        }
    }
}
