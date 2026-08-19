namespace Domain.Enums;

/// <summary>
/// Which way the money went. Stored explicitly rather than inferred from which party is set, so a
/// refund — money paid <em>to</em> a customer — needs a validator change rather than a migration.
/// </summary>
public enum PaymentDirection
{
    Received = 0,
    Paid = 1,
}

/// <summary>
/// Whether a payment has actually moved anything.
/// <para>
/// The middle state is load-bearing. A post-dated cheque is recorded and visible the day it is
/// handed over, but it settles nothing until it can be banked — so it sits <see cref="Pending"/>,
/// with no ledger entry and no balance movement behind it.
/// </para>
/// </summary>
public enum PaymentStatus
{
    /// <summary>Recorded, but has touched no balance. Only ever a post-dated cheque.</summary>
    Pending = 0,

    /// <summary>Ledger entry written, documents settled, party balance moved.</summary>
    Posted = 1,

    /// <summary>
    /// It happened and was then undone — cancelled, or a cheque that bounced. Never deleted; which
    /// of the two it was is told by the ledger entry type, not by this.
    /// </summary>
    Reversed = 2,
}
