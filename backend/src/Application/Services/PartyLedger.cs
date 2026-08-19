using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;

namespace Application.Services;

public class PartyLedger : IPartyLedger
{
    private readonly IPartyLedgerRepository _repository;

    public PartyLedger(IPartyLedgerRepository repository)
    {
        _repository = repository;
    }

    public Task RecordForCustomerAsync(
        Customer customer,
        decimal signedAmount,
        PartyLedgerEntryType entryType,
        DateOnly entryDate,
        Guid? referenceId,
        string? referenceNumber,
        string? notes,
        CancellationToken cancellationToken)
    {
        customer.OutstandingBalance = Round(customer.OutstandingBalance + signedAmount);
        customer.UpdatedAt = DateTimeOffset.UtcNow;

        return AppendAsync(
            new PartyLedgerEntry
            {
                CustomerId = customer.Id,
                PartyName = customer.Name,
                BalanceAfter = customer.OutstandingBalance,
            },
            signedAmount, entryType, entryDate, referenceId, referenceNumber, notes, cancellationToken);
    }

    public Task RecordForSupplierAsync(
        Supplier supplier,
        decimal signedAmount,
        PartyLedgerEntryType entryType,
        DateOnly entryDate,
        Guid? referenceId,
        string? referenceNumber,
        string? notes,
        CancellationToken cancellationToken)
    {
        supplier.OutstandingBalance = Round(supplier.OutstandingBalance + signedAmount);
        supplier.UpdatedAt = DateTimeOffset.UtcNow;

        return AppendAsync(
            new PartyLedgerEntry
            {
                SupplierId = supplier.Id,
                PartyName = supplier.Name,
                BalanceAfter = supplier.OutstandingBalance,
            },
            signedAmount, entryType, entryDate, referenceId, referenceNumber, notes, cancellationToken);
    }

    /// <summary>
    /// Everything the two overloads share. The caller has already set the party columns and stamped
    /// the balance; this fills in the rest and hands it to the repository — without saving, so the
    /// entry lands in the same transaction as whatever caused it.
    /// </summary>
    private Task AppendAsync(
        PartyLedgerEntry entry,
        decimal signedAmount,
        PartyLedgerEntryType entryType,
        DateOnly entryDate,
        Guid? referenceId,
        string? referenceNumber,
        string? notes,
        CancellationToken cancellationToken)
    {
        entry.EntryType = entryType;
        entry.Amount = Round(signedAmount);
        entry.EntryDate = entryDate;
        entry.RecordedAt = DateTimeOffset.UtcNow;
        entry.ReferenceId = referenceId;
        entry.ReferenceNumber = referenceNumber;
        entry.Notes = notes;

        return _repository.AddEntryAsync(entry, cancellationToken);
    }

    private static decimal Round(decimal value) =>
        Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
