namespace Pigmemento.Api.Dtos;

public class InferResponseDto
{
    public ProbabilitiesDto Probs { get; set; } = default!;
    public string CamPngUrl { get; set; } = default!;
}

public class ProbabilitiesDto
{
    public double Benign { get; set; }
    public double Malignant { get; set; }
}