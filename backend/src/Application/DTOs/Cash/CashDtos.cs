namespace Application.DTOs.Cash;

/// <summary>
/// What should be in the drawer right now, and how it got there.
/// <para>
/// Only cash. UPI and card money is real but it is in a bank, not in a drawer, and mixing them is
/// how "the till is short" becomes an argument nobody can settle.
/// </para>
/// </summary>
public record CashPositionDto(
    DateOnly Date,
    decimal OpeningCash,
    decimal CashReceived,
    decimal CashPaidOut,
    decimal CashExpenses,
    /// <summary>Float, bank withdrawals, capital — cash that arrived without a sale behind it.</summary>
    decimal CashOtherIn,
    /// <summary>Banked, and drawings.</summary>
    decimal CashOtherOut,
    decimal ExpectedCash,
    /// <summary>False when the previous day was never closed, so the opening figure is a guess.</summary>
    bool OpeningIsCarriedForward,
    bool IsClosed,
    decimal? CountedCash,
    decimal? Difference,
    string? Reason);

public record CloseDayRequest(
    DateOnly CloseDate,
    decimal CountedCash,
    string? Reason,
    string? Notes);

public record DayCloseDto(
    Guid Id,
    DateOnly CloseDate,
    decimal OpeningCash,
    decimal CashReceived,
    decimal CashPaidOut,
    decimal CashExpenses,
    /// <summary>Float, bank withdrawals, capital — cash that arrived without a sale behind it.</summary>
    decimal CashOtherIn,
    /// <summary>Banked, and drawings.</summary>
    decimal CashOtherOut,
    decimal ExpectedCash,
    decimal CountedCash,
    decimal Difference,
    string? Reason,
    string? Notes,
    DateTimeOffset CreatedAt);

/// <summary>One movement of cash, whichever document caused it.</summary>
public record CashBookEntryDto(
    DateOnly Date,
    string Kind,
    string Reference,
    string Particulars,
    decimal In,
    decimal Out,
    decimal Balance);

public record CashBookDto(
    DateOnly FromDate,
    DateOnly ToDate,
    decimal OpeningBalance,
    decimal ClosingBalance,
    IReadOnlyList<CashBookEntryDto> Entries);
