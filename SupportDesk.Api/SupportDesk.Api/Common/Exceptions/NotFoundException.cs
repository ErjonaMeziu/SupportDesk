namespace SupportDesk.Api.Common.Exceptions;

public class NotFoundException : AppException
{
    public NotFoundException(string code, string message) : base(StatusCodes.Status404NotFound, code, message)
    {
    }
}
