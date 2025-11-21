using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Pigmemento.Api.Dtos;

namespace Pigmemento.Api.Services;

public class InferenceOptions
{
    public string BaseUrl { get; set; } = default!;
}

public class PythonInferenceClient : IInferenceClient
{
    private readonly HttpClient _httpClient;

    public PythonInferenceClient(HttpClient httpClient, IOptions<InferenceOptions> options)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri(options.Value.BaseUrl);
    }

    public async Task<InferResponseDto> InferAsync(Stream image, string fileName, CancellationToken ct)
    {
        using var content = new MultipartFormDataContent();
        var streamContent = new StreamContent(image);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        content.Add(streamContent, "file", fileName);

        var response = await _httpClient.PostAsync("/infer", content, ct);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(ct);
        var result = JsonSerializer.Deserialize<InferResponseDto>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (result is null)
        {
            throw new InvalidOperationException("Empty inference response");
        }
        
        return result;
    }
}