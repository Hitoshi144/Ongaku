using Microsoft.EntityFrameworkCore;
using Ongaku.Data;
using Ongaku.Models;

namespace Ongaku.Services {
    public class ArtistService {
        private readonly IDbContextFactory<OngakuContext> _contextFactory;
        private readonly IWebHostEnvironment _environment;

        public ArtistService(IWebHostEnvironment env, IDbContextFactory<OngakuContext> context)
        {
            _contextFactory = context;
            _environment = env;
        }

        public async Task<Artist?> GetArtistByName(string name)
        {
            using var _context = _contextFactory.CreateDbContext();
            return await _context.Artists.Where(t => EF.Functions.ILike(t.Name, $"%{name}%")).FirstOrDefaultAsync();
        }

        public async Task<Artist> AddArtistAsync(string name)
        {
            using var _context = _contextFactory.CreateDbContext();
            Artist artist = new() { Name = name };

            _context.Artists.Add(artist);
            await _context.SaveChangesAsync();
            return artist;
        }

        public async Task<List<Artist>> GetAllArtistsByRequest(string req)
        {
            using var _context = _contextFactory.CreateDbContext();
            return await _context.Artists.Where(t => EF.Functions.ILike(t.Name, $"%{req}%")).ToListAsync();
        }

        public async Task<List<Artist>> GetAllArtists()
        {
            using var _context = _contextFactory.CreateDbContext();
            return await _context.Artists.ToListAsync();
        }
    }
}
