using Microsoft.AspNetCore.Mvc;
using Pigmemento.Api.Core.Services;
using System.ComponentModel.DataAnnotations;

namespace Pigmemento.Api.Controllers;

[ApiController]
[Route("media")]
public class MediaController : ControllerBase
{
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/jpg", "image/png", "image/webp"
    };

    private readonly StorageService _storage;

    public MediaController(StorageService storage)
    {
        _storage = storage;
    }

    /// <summary>
    /// Upload an image to R2. Returns the storage key and a readable URL (signed or public).
    /// </summary>
    [HttpPost]
    [RequestSizeLimit(20_000_000)] // 20 MB
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<object>> Upload([FromForm, Required] IFormFile file,
        [FromQuery] string? folder = "cases")
    {
        if (file is null || file.Length == 0)
            return BadRequest("No file provided.");

        // Basic content-type validation (defense-in-depth)
        var contentType = file.ContentType?.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(contentType) || !AllowedContentTypes.Contains(contentType))
            return BadRequest($"Unsupported Content-Type. Allowed: {string.Join(", ", AllowedContentTypes)}");

        var ext = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(ext))
        {
            // fallback by content-type
            ext = contentType switch
            {
                "image/jpeg" or "image/jpg" => ".jpg",
                "image/png" => ".png",
                "image/webp" => ".webp",
                _ => ".bin"
            };
        }

        var key = $"{folder?.Trim('/')}/{DateTime.UtcNow:yyyy/MM}/{Guid.NewGuid()}{ext.ToLowerInvariant()}";

        // OPTIONAL: small max length guard
        if (file.Length > 20_000_000) // 20 MB
            return BadRequest("File too large.");

        await using var stream = file.OpenReadStream();
        var url = await _storage.UploadAsync(key, stream, contentType);

        return Ok(new
        {
            key,
            url,
            contentType,
            size = file.Length
        });
    }

    /// <summary>
    /// Get a fresh readable URL (signed if bucket is private).
    /// </summary>
    [HttpGet("signed")]
    public ActionResult<object> GetSigned([FromQuery, Required] string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return BadRequest("key is required.");
        var url = _storage.GetReadableUrl(key);
        return Ok(new { key, url });
    }

    /// <summary>
    /// Delete an object from storage.
    /// </summary>
    [HttpDelete]
    public async Task<IActionResult> Delete([FromQuery, Required] string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return BadRequest("key is required.");
        await _storage.DeleteAsync(key);
        return NoContent();
    }
}