using Perimeter.Gateway.Domain.Models;

namespace Perimeter.Gateway.Application.Abstractions;

public interface IAuditWriter
{
    Task WriteAsync(
        AuditRecord record,
        CancellationToken ct);
}