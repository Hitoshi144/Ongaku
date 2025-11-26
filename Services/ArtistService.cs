using Microsoft.EntityFrameworkCore;
using Ongaku.Data;
using Ongaku.Models;

namespace Ongaku.Services {
    public class ArtistService {
        private readonly OngakuContext _context;
        private readonly IWebHostEnvironment _environment;

        public ArtistService(IWebHostEnvironment env, OngakuContext context)
        {
            _context = context;
            _environment = env;
        }

        public async Task<Artist?> GetArtistByName(string name)
        {
            return await _context.Artists.Where(t => EF.Functions.ILike(t.Name, $"%{name}%")).FirstOrDefaultAsync();
        }

        public async Task<Artist> AddArtistAsync(string name)
        {
            Artist artist = new() { Name = name };

            _context.Artists.Add(artist);
            await _context.SaveChangesAsync();
            return artist;
        }
    }
}
