using Application.DTOs.Payments;
using Domain;
using Domain.Entities;

namespace Application.Mapping;

public static class PaymentMapper
{
    public static PaymentDto ToDto(this Payment payment) => new(
        payment.Id,
        payment.ReceiptNumber,
        payment.Direction.ToString(),
        payment.PaymentDate,
        payment.CustomerId,
        payment.SupplierId,
        payment.PartyName,
        payment.Amount,
        payment.AllocatedAmount,
        payment.UnallocatedAmount,
        payment.Mode.ToString(),
        payment.ReferenceNumber,
        payment.Notes,
        payment.Status.ToString(),
        payment.IsCounterPayment,
        payment.Cheque?.ToDto(),
        payment.Allocations.Select(a => a.ToDto()).ToList(),
        payment.CreatedAt);

    public static PaymentListItemDto ToListItemDto(this Payment payment) => new(
        payment.Id,
        payment.ReceiptNumber,
        payment.Direction.ToString(),
        payment.PaymentDate,
        payment.PartyName,
        payment.Amount,
        payment.UnallocatedAmount,
        payment.Mode.ToString(),
        payment.Status.ToString(),
        payment.IsCounterPayment,
        payment.Cheque?.ChequeNumber,
        payment.Cheque?.Status.ToString(),
        payment.Cheque?.ChequeDate);

    public static PaymentAllocationDto ToDto(this PaymentAllocation allocation) => new(
        allocation.Id,
        allocation.InvoiceId,
        allocation.PurchaseId,
        allocation.DocumentNumber,
        allocation.DocumentDate,
        allocation.Amount,
        allocation.IsReversed);

    public static ChequeDto ToDto(this ChequeDetail cheque) => new(
        cheque.ChequeNumber,
        cheque.BankName,
        cheque.ChequeDate,
        cheque.ReceivedOn,
        cheque.Status.ToString(),
        cheque.DepositedOn,
        cheque.ClearedOn,
        cheque.BouncedOn,
        cheque.BounceReason,
        // Sent with the row so the register can offer exactly the actions that will succeed, rather
        // than showing four buttons and letting three of them 409.
        ChequeTransitions.NextFrom(cheque.Status).Select(s => s.ToString()).ToList());

    public static PartyLedgerEntryDto ToDto(this PartyLedgerEntry entry) => new(
        entry.Id,
        entry.EntryType.ToString(),
        entry.Amount,
        entry.BalanceAfter,
        entry.EntryDate,
        entry.ReferenceId,
        entry.ReferenceNumber,
        entry.Notes);

    public static OpenDocumentDto ToOpenDocumentDto(this Invoice invoice, DateOnly asOf) => new(
        invoice.Id,
        invoice.InvoiceNumber,
        invoice.InvoiceDate,
        invoice.DueDate,
        invoice.GrandTotal,
        invoice.AmountPaid,
        invoice.BalanceDue,
        // Measured from the due date where one exists, so a customer on terms is not called overdue
        // on the day after billing.
        asOf.DayNumber - (invoice.DueDate ?? invoice.InvoiceDate).DayNumber);

    public static OpenDocumentDto ToOpenDocumentDto(this Purchase purchase, DateOnly asOf) => new(
        purchase.Id,
        purchase.PurchaseNumber,
        purchase.InvoiceDate,
        null,
        purchase.GrandTotal,
        purchase.AmountPaid,
        purchase.BalanceDue,
        asOf.DayNumber - purchase.InvoiceDate.DayNumber);
}
