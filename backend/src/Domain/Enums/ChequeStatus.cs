namespace Domain.Enums;

/// <summary>Where a cheque is on its way from the counter to the bank — or back again.</summary>
public enum ChequeStatus
{
    /// <summary>In the drawer. Either waiting to be banked, or post-dated and not yet bankable.</summary>
    Pending = 0,

    /// <summary>Given to the bank, waiting on clearing.</summary>
    Deposited = 1,

    Cleared = 2,

    Bounced = 3,

    /// <summary>The customer took it back, or swapped it for cash before it was banked.</summary>
    Cancelled = 4,
}
