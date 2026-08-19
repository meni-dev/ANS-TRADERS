namespace Application.Common.Exceptions;

/// <summary>
/// The caller is signed in but is not allowed to do this. Distinct from not being signed in at all —
/// one means "sign in", the other means "ask the owner".
/// </summary>
public class ForbiddenException : Exception
{
    public string Code { get; }

    public ForbiddenException(string message, string code = "FORBIDDEN") : base(message)
    {
        Code = code;
    }
}
