using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pigmemento.Api.Data;
using Pigmemento.Api.Dtos;
using Pigmemento.Api.Services;

namespace Pigmemento.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class InferenceController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IInferenceClient _inferenceClient;
    private readonly IHttpClientFactory _httpClientFactory;

    public InferenceController(
        AppDbContext db,
        IInferenceClient inferenceClient,
        IHttpClientFactory httpClientFactory)
    {
        _db = db;
        _inferenceClient = inferenceClient;
        _httpClientFactory = httpClientFactory;
    }

    [HttpPost]
    [Authorize]
    [RequestSizeLimit(10_000_000)] // 10 MB
    public async Task<ActionResult<InferResponseDto>> Infer(
        IFormFile file, 
        CancellationToken ct)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("Image file is required");
        }

        await using var stream = file.OpenReadStream();

        var result = await _inferenceClient.InferAsync(stream, file.FileName, ct);
        
        return Ok(result);
    }

    [HttpPost("cases/{caseId:guid}")]
    [Authorize]
    public async Task<ActionResult<InferResponseDto>> InferForCase(
        Guid caseId,
        CancellationToken ct)
    {
        // 1) Load case from DB
        var @case = await _db.Cases
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == caseId, ct);

        if (@case is null)
            return NotFound();

        if (string.IsNullOrWhiteSpace(@case.ImageUrl))
            return BadRequest("Case has no ImageUrl");
        
        // 2) Download image bytes from ImageUrl
        var httpClient = _httpClientFactory.CreateClient("image-downloader");

        await using var imageStream = await httpClient.GetStreamAsync(@case.ImageUrl, ct);
        
        // 3) Forward stream to Python ML service
        var result = await _inferenceClient.InferAsync(imageStream, "case.jpg", ct);
        return Ok(result);
    }
}