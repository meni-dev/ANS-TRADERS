namespace Application.Common.Exceptions;

public class ConflictException : Exception
{
    public string Code { get; }

    public ConflictException(string message, string code = "CONFLICT") : base(message)
    {
        Code = code;
    }
}
