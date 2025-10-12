using Microsoft.EntityFrameworkCore;
using Pigmemento.Api.Models;

namespace Pigmemento.Api.Contracts;

public static class CaseMappings
{
    // Projection for list (no label)
    public static IQueryable<CaseListItemDto> ToListItemDto(this IQueryable<Case> query)
        => query.Select(c => new CaseListItemDto(
            c.Id,
            c.ImageUrl,
            c.Difficulty,
            new PatientDto(
                c.Patient.Age,
                c.Patient.Site,
                c.Patient.Sex,
                c.Patient.FitzpatrickType
            )
        ));

    // Projection for detail (with label + teaching points)
    public static IQueryable<CaseDetailDto> ToDetailDto(this IQueryable<Case> query)
        => query.Select(c => new CaseDetailDto(
            c.Id,
            c.ImageUrl,
            c.Label,
            c.Diagnosis2,
            c.Diagnosis3,
            c.Difficulty,
            new PatientDto(
                c.Patient.Age,
                c.Patient.Site,
                c.Patient.Sex,
                c.Patient.FitzpatrickType
            ),
            c.Metadata,
            c.TeachingPoints
                .OrderBy(tp => tp.Id) // stable order
                .Select(tp => new TeachingPointDto(tp.Id, tp.CaseId, tp.Points))
                .ToList()
        ));
}