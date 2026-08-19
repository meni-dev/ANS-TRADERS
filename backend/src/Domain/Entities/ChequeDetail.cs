using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

/// <summary>
/// The cheque behind a payment. A separate row sharing the payment's primary key rather than nine
/// nullable columns on <see cref="Payment"/>, for three reasons:
/// <list type="bullet">
/// <item>a payment is immutable once recorded apart from its status, whereas a cheque genuinely
/// changes over days — keeping the moving part in its own table means there is one answer to
/// "what can change after the fact";</item>
/// <item>columns that must all be null unless the mode is Cheque need a constraint somebody will
/// forget to write, while a missing row cannot be in a wrong state;</item>
/// <item>the shared key enforces <em>one cheque per payment</em>. That matters: a customer handing
/// over three post-dated cheques must produce three payments, because each one clears, bounces and
/// allocates on its own.</item>
/// </list>
/// </summary>
public class ChequeDetail : AuditableEntity
{
    /// <summary>Primary key and foreign key both — this row exists only as part of its payment.</summary>
    public Guid PaymentId { get; set; }
    public Payment? Payment { get; set; }

    public string ChequeNumber { get; set; } = string.Empty;

    /// <summary>The drawer's bank — the one anybody actually asks about.</summary>
    public string BankName { get; set; } = string.Empty;

    /// <summary>
    /// The date written on the cheque. May be in the future: a post-dated cheque is normal here, and
    /// it is this date, not <see cref="ReceivedOn"/>, that decides when the money becomes bankable.
    /// </summary>
    public DateOnly ChequeDate { get; set; }

    /// <summary>The day it was physically handed over. Equal to <see cref="ChequeDate"/> for a normal cheque.</summary>
    public DateOnly ReceivedOn { get; set; }

    public ChequeStatus Status { get; set; } = ChequeStatus.Pending;

    public DateOnly? DepositedOn { get; set; }
    public DateOnly? ClearedOn { get; set; }
    public DateOnly? BouncedOn { get; set; }

    /// <summary>What the bank said. Printed on the register, and it is what the shop argues with.</summary>
    public string? BounceReason { get; set; }
}
