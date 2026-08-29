namespace Perimeter.Gateway.Application.Abstractions;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}