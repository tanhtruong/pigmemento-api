using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pigmemento.Api.Data.Seed;
using Pigmemento.Api.Dtos;

namespace Pigmemento.Api.Controllers;

[ApiController]
[Route("admin/cases")]
[Authorize(Roles = "admin")]
public class AdminCasesController : ControllerBase
{
    private readonly CaseSeeder _seeder;

    public AdminCasesController(CaseSeeder seeder)
    {
        _seeder = seeder;
    }

    /// <summary>
    /// Import cases from an uploaded CSV file.
    /// Content-Type: multipart/form-data; field name: "file".
    /// </summary>
    [HttpPost("import")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(50_000_000)] // 50 MB, adjust as needed
    public async Task<ActionResult<object>> ImportCases([FromForm] CaseImportRequest request)
    {
        var file = request.File;
        
        if (file == null || file.Length == 0)
        {
            return BadRequest(new
            {
                error = "No file uploaded or file was empty. Use form field 'file'."
            });
        }

        if (!file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new
            {
                error = "Invalid file type. Expected a .csv file."
            });
        }
        
        try
        {
            await using var stream = file.OpenReadStream();
            var result = await _seeder.SeedFromCsvAsync(stream);

            return Ok(new
            {
                message = "Case import competed.",
                imported = result.Imported,
                skippedExisting = result.SkippedExisting,
                fileName = file.FileName
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                error = "Import failed.",
                detail = ex.Message
            });
        }
    }

}