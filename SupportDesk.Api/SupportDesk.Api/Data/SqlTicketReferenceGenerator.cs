using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace SupportDesk.Api.Data;

/// <summary>
/// Issues the next sequence number for a given year from the TicketReferenceCounters table using
/// a single atomic MERGE statement guarded by a HOLDLOCK table hint. SQL Server takes and holds an
/// update lock on the target row for the duration of the statement, so concurrent requests for the
/// same year are serialized against each other and each receives a distinct, gap-free number -
/// unlike a naive "read then increment then save" approach, which would race under concurrency.
/// </summary>
public class SqlTicketReferenceGenerator : ITicketReferenceGenerator
{
    private const string Prefix = "TCK";

    private readonly SupportDeskDbContext _db;

    public SqlTicketReferenceGenerator(SupportDeskDbContext db)
    {
        _db = db;
    }

    public async Task<string> GenerateAsync(int year, CancellationToken cancellationToken)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

        var connection = _db.Database.GetDbConnection();
        await using var command = connection.CreateCommand();
        command.Transaction = transaction.GetDbTransaction();
        command.CommandText = """
            MERGE TicketReferenceCounters WITH (HOLDLOCK) AS target
            USING (SELECT @year AS Year) AS source
            ON target.Year = source.Year
            WHEN MATCHED THEN UPDATE SET LastNumber = target.LastNumber + 1
            WHEN NOT MATCHED THEN INSERT (Year, LastNumber) VALUES (@year, 1)
            OUTPUT inserted.LastNumber;
            """;

        var yearParameter = command.CreateParameter();
        yearParameter.ParameterName = "@year";
        yearParameter.Value = year;
        command.Parameters.Add(yearParameter);

        var result = await command.ExecuteScalarAsync(cancellationToken);
        var nextNumber = Convert.ToInt32(result);

        await transaction.CommitAsync(cancellationToken);

        return $"{Prefix}-{year}-{nextNumber:D4}";
    }
}
