using Application.Interfaces;
using Domain.Enums;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using NpgsqlTypes;

namespace Infrastructure.Repositories;

/// <inheritdoc />
public class DocumentNumbers : IDocumentNumbers
{
    private readonly AppDbContext _context;

    public DocumentNumbers(AppDbContext context)
    {
        _context = context;
    }

    public async Task<int> NextAsync(
        DocumentKind kind, string financialYear, CancellationToken cancellationToken)
    {
        // One statement, on purpose.
        //
        // INSERT ... ON CONFLICT DO UPDATE is atomic: the first document of a year creates the row,
        // every one after it increments the same row, and either way the row is locked until the
        // caller's transaction ends. A second request wanting the same series waits a moment rather
        // than reading a number that is about to be taken.
        //
        // Raw SQL rather than read-modify-write through the change tracker, because that would be
        // the very race this replaces.
        var sql = """
            INSERT INTO document_counters ("Id", "Kind", "FinancialYear", "LastNumber")
            VALUES (gen_random_uuid(), @kind, @year, 1)
            ON CONFLICT ("Kind", "FinancialYear")
            DO UPDATE SET "LastNumber" = document_counters."LastNumber" + 1
            RETURNING "LastNumber";
            """;

        var connection = (NpgsqlConnection)_context.Database.GetDbConnection();

        // The context's own transaction when there is one, so this shares the caller's unit of work
        // instead of committing a number the caller might never use.
        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        await using var command = new NpgsqlCommand(sql, connection);
        command.Transaction = (NpgsqlTransaction?)_context.Database.CurrentTransaction?.GetDbTransaction();
        command.Parameters.Add(new NpgsqlParameter("kind", NpgsqlDbType.Text) { Value = kind.ToString() });
        command.Parameters.Add(new NpgsqlParameter("year", NpgsqlDbType.Text) { Value = financialYear });

        return (int)(await command.ExecuteScalarAsync(cancellationToken))!;
    }
}
