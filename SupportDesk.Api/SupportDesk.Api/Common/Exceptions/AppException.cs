namespace SupportDesk.Api.Common.Exceptions;

/// <summary>
/// Base type for exceptions that should be translated into a machine-readable
/// { code, message } error response by ExceptionHandlingMiddleware, instead of a raw 500.
/// </summary>
public abstract class AppException : Exception
{
    public int StatusCode { get; }
    public string Code { get; }

    protected AppException(int statusCode, string code, string message) : base(message)
    {
        StatusCode = statusCode;
        Code = code;
    }
}
