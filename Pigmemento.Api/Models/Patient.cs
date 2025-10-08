using Microsoft.EntityFrameworkCore;

namespace Pigmemento.Api.Models;

[Owned]
public class Patient
{
    public int? Age { get; set; }
    public string? Sex { get; set; }
    public string? Site { get; set; }
    public string? FitzpatrickType { get; set; }
}
