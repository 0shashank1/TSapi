namespace TS.Application.Common;

public abstract class ApiException : Exception
{
    protected ApiException(string problemType, string detail)
        : base(detail)
    {
        ProblemType = problemType;
    }

    public string ProblemType { get; }
}

public sealed class NotFoundException : ApiException
{
    public NotFoundException(string detail)
        : base("not-found", detail)
    {
    }
}

public sealed class ForbiddenException : ApiException
{
    public ForbiddenException(string detail)
        : base("access-denied", detail)
    {
    }
}

public sealed class UnauthorizedException : ApiException
{
    public UnauthorizedException(string detail, string problemType = "unauthorized")
        : base(problemType, detail)
    {
    }
}

public sealed class ConflictException : ApiException
{
    public ConflictException(string detail)
        : base("conflict", detail)
    {
    }
}

public sealed class ConcurrencyConflictException : ApiException
{
    public ConcurrencyConflictException(string detail)
        : base("concurrency-conflict", detail)
    {
    }
}

public sealed class ValidationException : ApiException
{
    public ValidationException(string detail, IDictionary<string, string[]>? errors = null)
        : base("validation-error", detail)
    {
        Errors = errors;
    }

    public IDictionary<string, string[]>? Errors { get; }
}
