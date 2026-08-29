using Npgsql;
using NpgsqlTypes;
using Perimeter.Gateway.Application.Abstractions;
using Perimeter.Gateway.Application.Errors;
using Perimeter.Gateway.Domain.Models;

namespace Perimeter.Gateway.Infrastructure.CorporateData;

public sealed class CorporateDataReader : ICorporateDataReader
{
    private const string ReadAllSql = """
        SELECT
            "CustomerId",
            "Country",
            "InvoiceDate",
            "Total"
        FROM pdg.sales_summary
        ORDER BY "InvoiceDate", "CustomerId"
        LIMIT @take;
        """;

    private const string ReadAllowListSql = """
        SELECT
            "CustomerId",
            "Country",
            "InvoiceDate",
            "Total"
        FROM pdg.sales_summary
        WHERE "Country" = ANY(@countries)
        ORDER BY "InvoiceDate", "CustomerId"
        LIMIT @take;
        """;

    private readonly string _connectionString;

    public CorporateDataReader(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<IReadOnlyList<SalesSummaryRow>> ReadSalesSummaryAsync(
        RowScope effectiveScope,
        int take,
        CancellationToken ct)
    {
        if (take <= 0)
        {
            throw new PdgException(
                PdgErrorCategory.InternalError);
        }

        string sql;
        string[]? countries = null;

        switch (effectiveScope.Mode)
        {
            case RowScopeMode.All:
                sql = ReadAllSql;
                break;

            case RowScopeMode.AllowList:
                if (!effectiveScope.Dimensions.TryGetValue(
                        "country",
                        out var allowedCountries) ||
                    allowedCountries.Count == 0)
                {
                    throw new PdgException(
                        PdgErrorCategory.InternalError);
                }

                countries = allowedCountries.ToArray();
                sql = ReadAllowListSql;
                break;

            default:
                throw new PdgException(
                    PdgErrorCategory.InternalError);
        }

        try
        {
            await using var connection =
                new NpgsqlConnection(_connectionString);

            await connection.OpenAsync(ct);

            await using var command =
                new NpgsqlCommand(sql, connection);

            command.Parameters.Add(
                new NpgsqlParameter<int>(
                    "take",
                    NpgsqlDbType.Integer)
                {
                    TypedValue = take
                });

            if (countries is not null)
            {
                command.Parameters.Add(
                    new NpgsqlParameter<string[]>(
                        "countries",
                        NpgsqlDbType.Array |
                        NpgsqlDbType.Text)
                    {
                        TypedValue = countries
                    });
            }

            var rows = new List<SalesSummaryRow>();

            await using var reader =
                await command.ExecuteReaderAsync(ct);

            while (await reader.ReadAsync(ct))
            {
                rows.Add(
                    new SalesSummaryRow(
                        reader.GetInt32(0),
                        reader.GetString(1),
                        DateOnly.FromDateTime(
                            reader.GetDateTime(2)),
                        reader.GetDecimal(3)));
            }

            return rows;
        }
        catch (OperationCanceledException)
            when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (NpgsqlException ex)
        {
            throw new PdgException(
                PdgErrorCategory.CorporateDataSourceUnavailable,
                ex);
        }
    }
}