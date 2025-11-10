using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.EntityFrameworkCore;

namespace Pigmemento.Api.Data.Seed;

public class CaseSeeder
{
    private readonly AppDbContext _db;
    public CaseSeeder(AppDbContext db) =>  _db = db;

    public async Task<CaseImportedResult> SeedFromCsvAsync(Stream csvStream)
    {
        using var reader = new StreamReader(csvStream);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HeaderValidated = null,
            MissingFieldFound = null
        });
        
        var imported = 0;
        var skippedExisting = 0;

        var records = csv.GetRecords<dynamic>();
        
        foreach (var row in records)
        {
            string? sourceId = ((string?)row.isic_id)!.Trim();
            if (string.IsNullOrEmpty(sourceId))
            {
                // Invalid row, ignore
                continue;
            }
            
            var exists = await _db.Cases
                .AsNoTracking()
                .AnyAsync(c => c.SourceId == sourceId);

            if (exists)
            {
                skippedExisting++;
                continue;
            }
            
            var entity = CaseCsvMapper.FromDynamic(row);
            _db.Cases.Add(entity);
            imported++;
        }
        
        await _db.SaveChangesAsync();

        return new CaseImportedResult(imported, skippedExisting);
    }
}

public record CaseImportedResult(int Imported, int SkippedExisting);