namespace Domain.Enums;

public enum PaymentMode
{
    Cash = 0,
    Upi = 1,
    Card = 2,
    BankTransfer = 3,

    /// <summary>Nothing collected at the counter — the whole amount sits on the party's account.</summary>
    Credit = 4,

    /// <summary>
    /// Settles slowly and can fail, so a cheque carries its own row — see <see cref="Domain.Entities.ChequeDetail"/>.
    /// Values are stored as text, so appending here costs no migration.
    /// </summary>
    Cheque = 5,
}
