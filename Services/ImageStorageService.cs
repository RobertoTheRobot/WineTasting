using Microsoft.AspNetCore.Components.Forms;

namespace WineTasting.Services;

public class ImageStorageService
{
    private const long MaxImageBytes = 5 * 1024 * 1024;
    private static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png", ".webp", ".gif"];

    private readonly IWebHostEnvironment _env;
    private readonly ILogger<ImageStorageService> _logger;

    public ImageStorageService(IWebHostEnvironment env, ILogger<ImageStorageService> logger)
    {
        _env = env;
        _logger = logger;
    }

    public async Task<string?> SaveWineImageAsync(IBrowserFile file, CancellationToken ct = default)
    {
        var ext = Path.GetExtension(file.Name).ToLowerInvariant();
        if (!AllowedExtensions.Contains(ext))
        {
            throw new InvalidOperationException($"Unsupported image type '{ext}'. Allowed: {string.Join(", ", AllowedExtensions)}");
        }

        var fileName = $"{Guid.NewGuid():N}{ext}";
        var folder = Path.Combine(_env.WebRootPath, "uploads", "wines");
        Directory.CreateDirectory(folder);
        var fullPath = Path.Combine(folder, fileName);

        await using var input = file.OpenReadStream(MaxImageBytes, ct);
        await using var output = File.Create(fullPath);
        await input.CopyToAsync(output, ct);

        return $"/uploads/wines/{fileName}";
    }

    public void DeleteWineImage(string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath)) return;
        try
        {
            var trimmed = relativePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            var fullPath = Path.Combine(_env.WebRootPath, trimmed);
            if (File.Exists(fullPath)) File.Delete(fullPath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete wine image {Path}", relativePath);
        }
    }
}
