namespace Application.Common.Exceptions;

public class NotFoundException : Exception
{
    public string Code { get; }

    public NotFoundException(string message, string code = "NOT_FOUND") : base(message)
    {
        Code = code;
    }
}
