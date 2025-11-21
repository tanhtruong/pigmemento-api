using Pigmemento.Api.Dtos;

namespace Pigmemento.Api.Services;

public interface IInferenceClient
{
    Task<InferResponseDto> InferAsync(Stream image, string fileName, CancellationToken ct);
}