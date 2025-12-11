using Microsoft.AspNetCore.Components.Forms;
using Microsoft.EntityFrameworkCore;
using Ongaku.Data;
using Ongaku.Models;

namespace Ongaku.Services {
    public class ArtistService {
        private readonly IDbContextFactory<OngakuContext> _contextFactory;
        private readonly IWebHostEnvironment _environment;

        public Action? OnArtistEdit;

        public ArtistService(IWebHostEnvironment env, IDbContextFactory<OngakuContext> context)
        {
            _contextFactory = context;
            _environment = env;
        }

        public async Task<Artist?> GetArtistByName(string name)
        {
            using var _context = _contextFactory.CreateDbContext();

            if (string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            return await _context.Artists
                .AsNoTracking()
                .FirstOrDefaultAsync(a => EF.Functions.ILike(a.Name, name));
        }

        public async Task<Artist?> GetArtistById(string id)
        {
            using var _context = _contextFactory.CreateDbContext();
            return await _context.Artists.Include(a => a.Tracks).Where(a => a.Id == Convert.ToInt32(id)).FirstOrDefaultAsync();
        }

        public async Task<Artist> AddArtistAsync(string name)
        {
            using var _context = _contextFactory.CreateDbContext();

            var existingArtist = await _context.Artists
                .FirstOrDefaultAsync(a => EF.Functions.ILike(a.Name, name));

            if (existingArtist != null)
            {
                return existingArtist;
            }

            Artist artist = new()
            {
                Name = name,
                Avatar = "assets/teto_cover.png"
            };

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
            return await _context.Artists.Include(a => a.Tracks).ToListAsync();
        }

        public async Task EditArtistAvatar(IBrowserFile file, Artist artist)
        {
            using var _context = _contextFactory.CreateDbContext();
            _context.Attach(artist);

            var coversFolder = Path.Combine(_environment.WebRootPath, "avatars");
            Directory.CreateDirectory(coversFolder);

            var avatarName = $"{Guid.NewGuid()}{Path.GetExtension(file.Name)}";
            var absAvatarPath = Path.Combine(coversFolder, avatarName);

            using var stream = file.OpenReadStream(long.MaxValue);

            using var fs = new FileStream(absAvatarPath, FileMode.Create, FileAccess.Write);

            await stream.CopyToAsync(fs, bufferSize: 81920);


            var coverPath = $"avatars/{avatarName}";

            var oldAvatar = artist.Avatar;
            if (oldAvatar != null && oldAvatar.Split('/')[0] == "avatars")
            {
                var oldPath = Path.Combine(_environment.WebRootPath, oldAvatar);
                if (File.Exists(oldPath))
                {
                    File.Delete(oldPath);
                }
            }

            artist.Avatar = coverPath;

            await _context.SaveChangesAsync();

            OnArtistEdit?.Invoke();
        }
    }
}
