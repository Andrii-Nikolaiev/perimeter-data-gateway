using Perimeter.Gateway.Application.Abstractions;

namespace Perimeter.Gateway.Infrastructure.Time;

public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}