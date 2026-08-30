namespace Application.DTOs.Cash;

public record MoneyMovementDto(
    Guid Id,
    DateOnly MovementDate,
    string Kind,
    string KindLabel,
    decimal Amount,
    /// <summary>False when the money never passed through the till — straight into the bank.</summary>
    bool AffectsCash,
    string? ReferenceNumber,
    string? Notes,
    bool IsCancelled,
    string? CreatedByName);

public record RecordMoneyMovementRequest(
    DateOnly MovementDate,
    string Kind,
    decimal Amount,
    bool AffectsCash,
    string? ReferenceNumber,
    string? Notes);

/// <summary>
/// What the owner has put in and taken out, and what the shop opened with. The question a balance
/// sheet answers, for a shop that does not keep one.
/// </summary>
public record CapitalSummaryDto(
    decimal OpeningFloat,
    decimal OpeningStockValue,
    decimal CapitalIntroduced,
    decimal Drawings,
    decimal BankToCash,
    decimal CashToBank,
    /// <summary>Everything put in, less everything taken back out.</summary>
    decimal NetInvested);
