namespace Perimeter.Gateway.Application.Errors;

public sealed class PdgException : Exception
{
    public PdgException(string category)
        : base(category)
    {
        Category = category;
    }

    public PdgException(string category, Exception innerException)
        : base(category, innerException)
    {
        Category = category;
    }

    public string Category { get; }
}