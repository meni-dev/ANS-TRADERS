using Domain.Enums;

namespace Application.Interfaces;

/// <summary>
/// Hands out the next number in a document series.
/// <para>
/// It exists because the obvious way — read <c>MAX(Sequence)</c>, add one, insert — is a race. Two
/// bills raised in the same second read the same number, and the unique index rejects whichever
/// saves second. At one counter that is rare; with two tills, or on Lambda where several copies run
/// at once, it is an ordinary Tuesday.
/// </para>
/// <para>
/// <b>Reserving is part of the caller's transaction</b>, like the ledgers: the row this takes is
/// held until the caller commits, so a number is never handed out twice and never quietly consumed
/// by a document that then failed to save.
/// </para>
/// </summary>
public interface IDocumentNumbers
{
    Task<int> NextAsync(DocumentKind kind, string financialYear, CancellationToken cancellationToken);
}
