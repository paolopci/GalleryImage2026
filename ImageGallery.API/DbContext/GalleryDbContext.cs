using ImageGallery.Model;
using Microsoft.EntityFrameworkCore;

namespace ImageGallery.API.DbContext;

public class GalleryDbContext(DbContextOptions<GalleryDbContext> options) : Microsoft.EntityFrameworkCore.DbContext(options)
{
    private static readonly string[] AllowedImageExtensions = [".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp"];

    public DbSet<Image> Images => Set<Image>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.Entity<Image>(entity =>
        {
            entity.ToTable("Images");
            entity.HasKey(image => image.Id);
            entity.Property(image => image.Title)
                .IsRequired()
                .HasMaxLength(200);
            entity.Property(image => image.FileName)
                .IsRequired()
                .HasMaxLength(260);
        });
    }

    public async Task SeedImagesFromFolderAsync(string webRootPath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(webRootPath);

        var imagesFolder = Path.Combine(webRootPath, "Images");
        if (!Directory.Exists(imagesFolder))
        {
            return;
        }

        var existingFileNames = await Images
            .AsNoTracking()
            .Select(image => image.FileName)
            .ToListAsync(cancellationToken);

        var knownFileNames = new HashSet<string>(existingFileNames, StringComparer.OrdinalIgnoreCase);

        var newImages = Directory.EnumerateFiles(imagesFolder)
            .Where(filePath => AllowedImageExtensions.Contains(Path.GetExtension(filePath), StringComparer.OrdinalIgnoreCase))
            .Select(Path.GetFileName)
            .OfType<string>()
            .Where(fileName => !string.IsNullOrWhiteSpace(fileName) && !knownFileNames.Contains(fileName))
            .Select(fileName => new Image
            {
                Id = Guid.NewGuid(),
                FileName = fileName,
                Title = Path.GetFileNameWithoutExtension(fileName)
            })
            .ToList();

        if (newImages.Count == 0)
        {
            return;
        }

        await Images.AddRangeAsync(newImages, cancellationToken);
        await SaveChangesAsync(cancellationToken);
    }
}
