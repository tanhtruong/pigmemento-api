using CsvHelper;
using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pigmemento.Api.Data;
using Pigmemento.Api.Models;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Authorization;


namespace Pigmemento.Api.Controllers;

[ApiController]
[Route("admin")]
public class AdminImportController : ControllerBase
{
    private readonly AppDbContext _db;
    public AdminImportController(AppDbContext db) => _db = db;

    /// <summary>
    /// Import ISIC metadata CSV + uploaded.jsonl (results from /media uploads) and create Case rows.
    /// </summary>
    /// <param name="csv">ISIC metadata CSV from the ISIC CLI</param>
    /// <param name="uploadedJsonl">JSONL lines with {"key","url","contentType","size"} from /media responses</param>
    /// <param name="limit">Optional cap on rows to import</param>
    /// <param name="defaultDifficulty">easy|medium|hard</param>
    [HttpPost("import/isic")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<object>> ImportIsic(
        IFormFile csv,
        IFormFile uploadedJsonl,
        [FromQuery] int? limit = 200,
        [FromQuery] string defaultDifficulty = "medium")
    {
        if (csv is null || uploadedJsonl is null)
            return BadRequest("Both 'csv' and 'uploadedJsonl' files are required.");

        // Build URL map (ISIC_ID -> URL) from uploaded.jsonl results
        var urlMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        using (var sr = new StreamReader(uploadedJsonl.OpenReadStream()))
        {
            while (!sr.EndOfStream)
            {
                var line = await sr.ReadLineAsync();
                if (string.IsNullOrWhiteSpace(line)) continue;
                using var doc = System.Text.Json.JsonDocument.Parse(line);
                var root = doc.RootElement;

                var url = root.TryGetProperty("url", out var u) ? u.GetString() : null;
                var key = root.TryGetProperty("key", out var k) ? k.GetString() : null;

                // Derive ISIC id from key or url; expect filenames like ISIC_00xxxxx.jpg
                var filename = key ?? url;
                if (string.IsNullOrWhiteSpace(filename)) continue;
                var justName = System.IO.Path.GetFileNameWithoutExtension(filename);
                // Normalize to e.g. ISIC_0024306
                if (!string.IsNullOrWhiteSpace(justName) && url is not null)
                {
                    urlMap[justName.ToUpperInvariant()] = url;
                }
            }
        }

        // Parse ISIC CSV
        var cfg = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            DetectDelimiter = true,
            PrepareHeaderForMatch = a => a.Header.ToLowerInvariant()
        };

        int added = 0;
        using var csvReader = new StreamReader(csv.OpenReadStream());
        using var csvr = new CsvReader(csvReader, cfg);
        var recs = csvr.GetRecords<dynamic>();

        foreach (var rec in recs)
        {
            var d = (IDictionary<string, object>)rec;

            string? isicId = d.TryGetValue("isic_id", out var vId) ? vId?.ToString() : null;
            if (string.IsNullOrWhiteSpace(isicId)) continue;

            // The ISIC CLI saves images usually as ISIC_<id>.jpg
            var lookup = isicId.ToUpperInvariant();
            if (!urlMap.TryGetValue(lookup, out var imageUrl))
            {
                // Some CSVs don't include the "ISIC_" prefix in isic_id; try adding it
                if (!lookup.StartsWith("ISIC_"))
                    urlMap.TryGetValue("ISIC_" + lookup, out imageUrl);
            }
            if (string.IsNullOrWhiteSpace(imageUrl)) continue;

            var dx = d.TryGetValue("diagnosis_1", out var vDx) ? vDx?.ToString() : null;
            string label = (dx?.Equals("Malignant", StringComparison.OrdinalIgnoreCase) ?? false)
                ? "malignant" : "benign";

            int? age = null;
            if (d.TryGetValue("age_approx", out var vAge) && int.TryParse(vAge?.ToString(), out var a))
                age = a;

            var site = d.TryGetValue("anatom_site_general", out var vSite) ? vSite?.ToString() : null;

            var entity = new Case
            {
                ImageUrl = imageUrl,
                Label = label,
                Difficulty = defaultDifficulty,
                Patient = new Patient { Age = age, Site = site },
                Metadata = System.Text.Json.JsonSerializer.Serialize(new
                {
                    source = "ISIC",
                    isic_id = isicId,
                    diagnosis_1 = dx
                })
            };

            _db.Cases.Add(entity);
            added++;

            if (limit.HasValue && added >= limit.Value)
                break;
        }

        await _db.SaveChangesAsync();
        var count = await _db.Cases.CountAsync();
        return Ok(new { imported = added, totalCases = count });
    }
}