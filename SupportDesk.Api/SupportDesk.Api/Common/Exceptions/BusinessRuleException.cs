namespace SupportDesk.Api.Common.Exceptions;

/// <summary>
/// Thrown when a request is well-formed but violates a workflow/business rule
/// (e.g. an invalid status transition, or assigning an inactive agent).
/// </summary>
public class BusinessRuleException : AppException
{
    public BusinessRuleException(string code, string message) : base(StatusCodes.Status409Conflict, code, message)
    {
    }
}
