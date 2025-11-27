using Microsoft.EntityFrameworkCore;
using MudBlazor;
using Ongaku.Data;
using Ongaku.Enums;
using Ongaku.Models;

namespace Ongaku.Services {
    public class TrackService {
        private readonly OngakuContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly ArtistService _artistService;
        private readonly CoverRandomerService _coverRandomerService;

        public TrackService(IWebHostEnvironment env, OngakuContext context, ArtistService artistService, CoverRandomerService coverRandomerService)
        {
            _environment = env;
            _context = context;
            _artistService = artistService;
            _coverRandomerService = coverRandomerService;
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

        public async Task AddTrackAsync(string title, Stream stream)
        {
            var folder = Path.Combine(_environment.WebRootPath, "uploads");
            Directory.CreateDirectory( folder );

            var safeFileName = Path.GetFileName($"{title}");
            var filePath = Path.Combine( folder, safeFileName );

            if (File.Exists( filePath ) )
            {
                return;
            }

            await using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                stream.Position = 0;
                await stream.CopyToAsync( fs );
            }

            var filemetadata = TagLib.File.Create(filePath);

            Artist? existArtist = await _artistService.GetArtistByName(filemetadata.Tag.FirstPerformer);

            if (existArtist == null)
            {
                var artistName = filemetadata.Tag.FirstPerformer;

                if (string.IsNullOrEmpty(artistName))
                {
                    artistName = "Pear Teto";
                }

                existArtist = await _artistService.AddArtistAsync(artistName);
            }

            var duration = filemetadata.Properties.Duration;

            string? coverPath = null;
            if (filemetadata.Tag.Pictures != null && filemetadata.Tag.Pictures.Length > 0)
            {
                var coversFolder = Path.Combine(_environment.WebRootPath, "covers");
                Directory.CreateDirectory(coversFolder);

                var picture = filemetadata.Tag.Pictures[0];
                var bytes = picture.Data.Data;
                var mime = picture.MimeType;

                var coverName = $"{Guid.NewGuid()}.jpg";
                var absCoverPath = Path.Combine(coversFolder, coverName);

                coverPath = $"covers/{coverName}";
                await File.WriteAllBytesAsync( absCoverPath, bytes );
            }
            else
            {
                coverPath = _coverRandomerService.GetRandomCover();
            }

                var track = new Track
                {
                    Title = title,
                    Artist = existArtist,
                    ArtistId = existArtist.Id,
                    FilePath = filePath,
                    Duration = duration,
                    CoverPath = coverPath
                };

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

        public async void DeleteTrackAsync(int id)
        {
            try
            {
                Track? targetTrack = await _context.Tracks.SingleOrDefaultAsync(t => t.Id == id);
                if (targetTrack == null) return;

                File.Delete(Path.Combine(_environment.WebRootPath, targetTrack.FilePath));

                if (targetTrack.CoverPath != null && targetTrack.CoverPath.Contains("covers"))
                {
                    File.Delete(Path.Combine(_environment.WebRootPath, targetTrack.CoverPath));
                }

                _context.Tracks.Remove(targetTrack);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
    }
}
