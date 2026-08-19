using Domain.Enums;

namespace Domain;

/// <summary>
/// Which cheque status may follow which. Kept as a table rather than scattered <c>if</c> statements
/// so the whole state machine can be read — and tested — in one place.
/// </summary>
public static class ChequeTransitions
{
    private static readonly Dictionary<ChequeStatus, ChequeStatus[]> Allowed = new()
    {
        // Straight from Pending to Bounced is legal on purpose: the shop banked it and simply never
        // recorded the deposit. Refusing would be pedantic about bookkeeping the user did not do.
        [ChequeStatus.Pending] = [ChequeStatus.Deposited, ChequeStatus.Bounced, ChequeStatus.Cancelled],

        // Back to Pending undoes a mis-click. The other two are the real outcomes.
        [ChequeStatus.Deposited] = [ChequeStatus.Cleared, ChequeStatus.Bounced, ChequeStatus.Pending],

        // A cheque that has cleared, bounced or been handed back is finished. Re-presenting a
        // bounced cheque is a brand-new payment, because one that bounces twice has to appear
        // twice on the statement.
        [ChequeStatus.Cleared] = [],
        [ChequeStatus.Bounced] = [],
        [ChequeStatus.Cancelled] = [],
    };

    public static bool IsAllowed(ChequeStatus from, ChequeStatus to) =>
        Allowed.TryGetValue(from, out var next) && next.Contains(to);

    /// <summary>Where a cheque can still go from here — drives the row actions on the register.</summary>
    public static IReadOnlyList<ChequeStatus> NextFrom(ChequeStatus from) =>
        Allowed.TryGetValue(from, out var next) ? next : [];

    /// <summary>Cleared, bounced or handed back — nothing more will happen to it.</summary>
    public static bool IsTerminal(ChequeStatus status) => NextFrom(status).Count == 0;

    /// <summary>Still in the shop's hands or the bank's. What the register's open tabs list.</summary>
    public static bool IsOutstanding(ChequeStatus status) =>
        status is ChequeStatus.Pending or ChequeStatus.Deposited;
}
