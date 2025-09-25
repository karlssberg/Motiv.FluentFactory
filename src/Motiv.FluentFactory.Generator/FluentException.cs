using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace Motiv.FluentFactory.Generator;

[ExcludeFromCodeCoverage]
internal class FluentException : Exception
{
    public FluentException()
    {
    }

    protected FluentException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }

    public FluentException(string message) : base(message)
    {
    }

    public FluentException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
