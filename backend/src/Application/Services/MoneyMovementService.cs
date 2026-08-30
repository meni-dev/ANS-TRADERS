using Application.Common.Exceptions;
using Application.DTOs.Cash;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;

namespace Application.Services;

/// <summary>
/// Money that belongs to nobody — the float, the bank, the owner's own pocket.
/// </summary>
public class MoneyMovementService : IMoneyMovementService
{
    private readonly IMoneyMovementRepository _repository;
    private readonly IProductRepository _products;
    private readonly ICurrentUser _currentUser;
    private readonly IPeriodLock _periodLock;
    private readonly ICashDayLock _cashDayLock;
    private readonly ICashService _cash;
    private readonly IShopClock _clock;
    private readonly IAuditLog _audit;

    public MoneyMovementService(
        IMoneyMovementRepository repository,
        IProductRepository products,
        ICurrentUser currentUser,
        IPeriodLock periodLock,
        ICashDayLock cashDayLock,
        ICashService cash,
        IShopClock clock,
        IAuditLog audit)
    {
        _repository = repository;
        _products = products;
        _currentUser = currentUser;
        _periodLock = periodLock;
        _cashDayLock = cashDayLock;
        _cash = cash;
        _clock = clock;
        _audit = audit;
    }

    public async Task<IReadOnlyList<MoneyMovementDto>> SearchAsync(
        DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken)
    {
        _currentUser.RequireAny(
            "see where the shop's money came from", Permission.CapitalMovement, Permission.CostView);

        return (await _repository.SearchAsync(fromDate, toDate, cancellationToken))
            .Select(ToDto).ToList();
    }

    public async Task<MoneyMovementDto> RecordAsync(
        RecordMoneyMovementRequest request, CancellationToken cancellationToken)
    {
        _currentUser.Require(Permission.CapitalMovement, "move money in or out of the business");

        if (!Enum.TryParse<MoneyMovementKind>(request.Kind, ignoreCase: true, out var kind))
        {
            throw Invalid("Kind", $"'{request.Kind}' is not something this app knows how to record");
        }

        if (request.Amount <= 0)
        {
            throw Invalid("Amount", "Enter how much moved");
        }

        if (request.MovementDate > _clock.Today)
        {
            throw Invalid("MovementDate", "That has not happened yet");
        }

        await _periodLock.GuardAsync(request.MovementDate, "money movement", cancellationToken);
        await _cashDayLock.GuardAsync(
            request.MovementDate, "money movement", request.AffectsCash, cancellationToken);

        // A shop opens once. A second opening float would be counted again in every day close from
        // then on, and the drawer would be permanently over by that amount with nothing to show why.
        if (kind == MoneyMovementKind.OpeningFloat
            && await _repository.ExistsAsync(kind, cancellationToken))
        {
            throw new ConflictException(
                "The opening float has already been recorded. Cancel that one first if it was wrong.",
                "OPENING_FLOAT_EXISTS");
        }

        // Opening stock is goods, not notes, so it never touches the drawer whatever was asked
        // for. Moving between the bank and the till always does, by definition.
        var affectsCash = kind switch
        {
            MoneyMovementKind.OpeningStock => false,
            MoneyMovementKind.BankToCash or MoneyMovementKind.CashToBank => true,
            _ => request.AffectsCash,
        };

        // A till cannot hand over notes it does not have. Without this, banking or drawing more
        // than the drawer holds is accepted silently and every day close from then on is short by
        // the difference — with the day close itself reporting the shortfall as if somebody had
        // taken it.
        if (affectsCash && TakesFromTill(kind))
        {
            var amount = Round(request.Amount);
            var inTill = await _cash.GetExpectedCashAsync(request.MovementDate, cancellationToken);

            if (amount > inTill)
            {
                throw new ConflictException(
                    $"The till holds {inTill:0.00} on {request.MovementDate:dd MMM yyyy}, " +
                    $"so {amount:0.00} cannot come out of it. Record what went in first, or mark this " +
                    "as coming from the bank rather than through the till.",
                    "CASH_WOULD_GO_NEGATIVE");
            }
        }

        var movement = new MoneyMovement
        {
            MovementDate = request.MovementDate,
            Kind = kind,
            Amount = Round(request.Amount),
            AffectsCash = affectsCash,
            ReferenceNumber = Clean(request.ReferenceNumber),
            Notes = Clean(request.Notes),
        };

        await _repository.AddAsync(movement, cancellationToken);

        await _audit.RecordAsync(
            AuditAction.MoneyMoved, "MoneyMovement", movement.Id, Label(kind),
            $"{movement.Amount:0.00} on {movement.MovementDate:dd-MM-yyyy}", cancellationToken);

        await _repository.SaveChangesAsync(cancellationToken);

        return ToDto(movement);
    }

    public async Task CancelAsync(Guid id, CancellationToken cancellationToken)
    {
        _currentUser.Require(Permission.CapitalMovement, "cancel a money movement");

        var movement = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Movement '{id}' was not found", "MOVEMENT_NOT_FOUND");

        if (movement.IsCancelled)
        {
            throw new ConflictException("That was already cancelled", "ALREADY_CANCELLED");
        }

        await _periodLock.GuardUndoAsync(movement.MovementDate, "money movement", cancellationToken);

        movement.IsCancelled = true;
        movement.UpdatedAt = DateTimeOffset.UtcNow;

        await _audit.RecordAsync(
            AuditAction.Cancelled, "MoneyMovement", movement.Id, Label(movement.Kind),
            $"{movement.Amount:0.00}", cancellationToken);

        await _repository.SaveChangesAsync(cancellationToken);
    }

    public async Task<CapitalSummaryDto> GetCapitalAsync(CancellationToken cancellationToken)
    {
        _currentUser.RequireAny(
            "see what has been put into the business", Permission.CapitalMovement, Permission.CostView);

        var totals = await _repository.GetTotalsAsync(cancellationToken);

        decimal Of(MoneyMovementKind kind) => totals.GetValueOrDefault(kind);

        var openingFloat = Of(MoneyMovementKind.OpeningFloat);
        var openingStock = Of(MoneyMovementKind.OpeningStock);
        var introduced = Of(MoneyMovementKind.CapitalIntroduced);
        var drawings = Of(MoneyMovementKind.Drawings);

        return new CapitalSummaryDto(
            openingFloat,
            openingStock,
            introduced,
            drawings,
            Of(MoneyMovementKind.BankToCash),
            Of(MoneyMovementKind.CashToBank),
            // Bank and till transfers are left out on purpose: moving your own money from one pocket
            // to another is not putting anything in.
            Round(openingFloat + openingStock + introduced - drawings));
    }

    public static string Label(MoneyMovementKind kind) => kind switch
    {
        MoneyMovementKind.OpeningFloat => "Opening float",
        MoneyMovementKind.BankToCash => "Drawn from bank",
        MoneyMovementKind.CashToBank => "Banked",
        MoneyMovementKind.CapitalIntroduced => "Capital introduced",
        MoneyMovementKind.Drawings => "Drawings",
        _ => "Opening stock",
    };

    private static MoneyMovementDto ToDto(MoneyMovement m) => new(
        m.Id, m.MovementDate, m.Kind.ToString(), Label(m.Kind), m.Amount,
        m.AffectsCash, m.ReferenceNumber, m.Notes, m.IsCancelled, m.CreatedByName);

    private static decimal Round(decimal value) =>
        Math.Round(value, 2, MidpointRounding.AwayFromZero);

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    /// <summary>
    /// The kinds that take notes out of the drawer. Capital and the opening float only ever put
    /// money in, so they are never blocked by an empty till.
    /// </summary>
    private static bool TakesFromTill(MoneyMovementKind kind) =>
        kind is MoneyMovementKind.CashToBank or MoneyMovementKind.Drawings;

    private static ValidationAppException Invalid(string field, string message) =>
        new(new Dictionary<string, string[]> { [field] = [message] });
}
