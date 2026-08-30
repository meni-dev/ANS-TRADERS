using Application.Common;
using Application.Common.Exceptions;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;

namespace Application.Services;

public class PaymentLedger : IPaymentLedger
{
    private readonly IPaymentRepository _repository;
    private readonly IPartyLedger _partyLedger;

    private readonly IDocumentNumbers _numbers;

    public PaymentLedger(IPaymentRepository repository, IPartyLedger partyLedger, IDocumentNumbers numbers)
    {
        _repository = repository;
        _partyLedger = partyLedger;
        _numbers = numbers;
    }

    public Task<Payment> ReceiveAsync(PaymentDraft draft, CancellationToken cancellationToken) =>
        CreateAsync(draft, numbered: true, cancellationToken);

    public Task<Payment> RecordCounterPaymentAsync(PaymentDraft draft, CancellationToken cancellationToken) =>
        CreateAsync(draft with { IsCounterPayment = true }, numbered: false, cancellationToken);

    private async Task<Payment> CreateAsync(
        PaymentDraft draft, bool numbered, CancellationToken cancellationToken)
    {
        Validate(draft);

        var amount = Round(draft.Amount);

        // A post-dated cheque is real paper the shop is holding, but it is not money it can use, so
        // it is recorded and left to settle nothing until somebody banks it.
        var isPostDated = draft.Cheque is { } cheque && cheque.ChequeDate > draft.PaymentDate;

        var payment = new Payment
        {
            Direction = draft.Direction,
            PaymentDate = draft.PaymentDate,
            CustomerId = draft.Customer?.Id,
            SupplierId = draft.Supplier?.Id,
            PartyName = draft.PartyName,
            Amount = amount,
            Mode = draft.Mode,
            ReferenceNumber = Clean(draft.ReferenceNumber),
            Notes = Clean(draft.Notes),
            IsCounterPayment = draft.IsCounterPayment,
            Status = isPostDated ? PaymentStatus.Pending : PaymentStatus.Posted,
            FinancialYear = FinancialYear.For(draft.PaymentDate),
        };

        if (numbered)
        {
            // Counter payments deliberately skip this: the invoice the customer is handed already
            // serves as their receipt, and a second number for one event is what later produces two
            // documents for one transaction.
            var sequence = await _numbers.NextAsync(
                draft.Direction == PaymentDirection.Received ? DocumentKind.Receipt : DocumentKind.PaymentOut,
                payment.FinancialYear,
                cancellationToken);

            payment.Sequence = sequence;
            payment.ReceiptNumber = FormatNumber(draft.Direction, payment.FinancialYear, sequence);
        }

        if (draft.Cheque is { } chequeDraft)
        {
            payment.Cheque = new ChequeDetail
            {
                PaymentId = payment.Id,
                ChequeNumber = chequeDraft.ChequeNumber.Trim(),
                BankName = chequeDraft.BankName.Trim(),
                ChequeDate = chequeDraft.ChequeDate,
                ReceivedOn = chequeDraft.ReceivedOn,
                Status = ChequeStatus.Pending,
            };
        }

        await _repository.AddAsync(payment, cancellationToken);

        if (isPostDated)
        {
            // Deliberately allocates nothing. Writing allocation rows now and applying them later
            // would let two post-dated cheques reserve the same bill — each would see the full
            // balance still outstanding — and the pair would overpay it when they were banked. The
            // money waits as unallocated and is allocated in PostAsync, against whatever is actually
            // still open on the day it reaches the bank.
            RecomputeTotals(payment);
            return payment;
        }

        ApplyAllocations(payment, draft.Allocations, moveDocuments: true);

        await RecordPartyEntryAsync(
            payment,
            draft.Customer,
            draft.Supplier,
            amount,
            reversing: false,
            EntryTypeFor(draft.Direction, draft.Customer is not null),
            draft.PaymentDate,
            notes: null,
            cancellationToken);

        return payment;
    }

    public async Task PostAsync(Payment payment, DateOnly effectiveDate, CancellationToken cancellationToken)
    {
        if (payment.Status != PaymentStatus.Pending)
        {
            throw new ConflictException(
                $"Payment '{payment.ReceiptNumber ?? payment.Id.ToString()}' has already been posted",
                "PAYMENT_ALREADY_POSTED");
        }

        // The effective date moves to the day it was actually banked, so a cheque written for the
        // 1st but banked on the 5th lands in the month the money arrived.
        payment.PaymentDate = effectiveDate;
        payment.Status = PaymentStatus.Posted;
        payment.UpdatedAt = DateTimeOffset.UtcNow;

        // Allocated now rather than when the cheque was taken, because the bills it should settle
        // are the ones still open today — a month of other receipts may have closed the one the
        // customer had in mind.
        await AutoAllocateAsync(payment, cancellationToken);

        await RecordPartyEntryAsync(
            payment,
            payment.Customer,
            payment.Supplier,
            payment.Amount,
            reversing: false,
            EntryTypeFor(payment.Direction, payment.CustomerId is not null),
            effectiveDate,
            notes: null,
            cancellationToken);
    }

    public Task AllocateAsync(
        Payment payment, IReadOnlyList<AllocationTarget> targets, CancellationToken cancellationToken)
    {
        if (payment.Status == PaymentStatus.Reversed)
        {
            throw new ConflictException(
                "This payment has been reversed and cannot be allocated", "PAYMENT_REVERSED");
        }

        var requested = Round(targets.Sum(t => t.Amount));

        if (requested > payment.UnallocatedAmount)
        {
            throw new ValidationAppException(new Dictionary<string, string[]>
            {
                ["Allocations"] =
                [
                    $"Only {payment.UnallocatedAmount:0.00} of this payment is unallocated",
                ],
            });
        }

        ApplyAllocations(payment, targets, moveDocuments: payment.Status == PaymentStatus.Posted);

        return Task.CompletedTask;
    }

    public async Task ReverseAsync(
        Payment payment,
        PartyLedgerEntryType entryType,
        DateOnly onDate,
        string reason,
        CancellationToken cancellationToken)
    {
        if (payment.Status == PaymentStatus.Reversed)
        {
            throw new ConflictException(
                $"Payment '{payment.ReceiptNumber ?? payment.Id.ToString()}' is already reversed",
                "PAYMENT_ALREADY_REVERSED");
        }

        var wasPosted = payment.Status == PaymentStatus.Posted;
        var live = payment.Allocations.Where(a => !a.IsReversed).ToList();

        // Documents only need putting back if this payment had actually settled them. A post-dated
        // cheque cancelled before banking never touched a bill.
        ReleaseAllocations(payment, live, moveDocuments: wasPosted);

        payment.Status = PaymentStatus.Reversed;
        payment.Notes = string.IsNullOrWhiteSpace(payment.Notes)
            ? reason
            : $"{payment.Notes}\n{reason}";
        payment.UpdatedAt = DateTimeOffset.UtcNow;

        if (wasPosted)
        {
            await RecordPartyEntryAsync(
                payment, payment.Customer, payment.Supplier, payment.Amount, reversing: true,
                entryType, onDate, reason, cancellationToken);
        }
    }

    public Task ReleaseAllocationsForInvoiceAsync(
        Invoice invoice, IReadOnlyList<PaymentAllocation> allocations, CancellationToken cancellationToken)
    {
        Release(allocations, amount => ApplyToInvoice(invoice, -amount));
        return Task.CompletedTask;
    }

    public Task ReleaseAllocationsForPurchaseAsync(
        Purchase purchase, IReadOnlyList<PaymentAllocation> allocations, CancellationToken cancellationToken)
    {
        Release(allocations, amount => ApplyToPurchase(purchase, -amount));
        return Task.CompletedTask;
    }

    /// <summary>
    /// Takes money back off a cancelled document without touching the payments that supplied it. The
    /// customer really did hand it over, so each released amount returns to its own payment as
    /// unallocated — an advance they can spend on the re-issued bill.
    /// </summary>
    private static void Release(
        IReadOnlyList<PaymentAllocation> allocations, Action<decimal> giveBackToDocument)
    {
        foreach (var allocation in allocations.Where(a => !a.IsReversed))
        {
            allocation.IsReversed = true;
            giveBackToDocument(allocation.Amount);

            if (allocation.Payment is { } payment)
            {
                payment.AllocatedAmount = Round(payment.AllocatedAmount - allocation.Amount);
                payment.UnallocatedAmount = Round(payment.Amount - payment.AllocatedAmount);
                payment.UpdatedAt = DateTimeOffset.UtcNow;
            }
        }
    }

    /// <summary>
    /// Attaches allocation rows to a payment and, when the payment is live, moves the documents they
    /// point at. A pending post-dated cheque builds its rows now and moves nothing.
    /// </summary>
    /// <summary>
    /// Settles the party's open documents oldest first with whatever of this payment is still
    /// unallocated. Used when a post-dated cheque reaches the bank, which is the one moment the
    /// shop's own rule — "put it against my account, oldest first" — is applied without anyone
    /// having chosen documents.
    /// </summary>
    private async Task AutoAllocateAsync(Payment payment, CancellationToken cancellationToken)
    {
        if (payment.UnallocatedAmount <= 0)
        {
            return;
        }

        if (payment.CustomerId is { } customerId)
        {
            var invoices = await _repository.GetOpenInvoicesForCustomerAsync(customerId, cancellationToken);

            var plan = PaymentAllocationPlanner.Plan(
                payment.UnallocatedAmount,
                invoices.Select(i => new OpenDocument(i.Id, i.InvoiceDate, i.BalanceDue)));

            ApplyAllocations(
                payment,
                plan.Select(p => new AllocationTarget(
                    invoices.First(i => i.Id == p.DocumentId), null, p.Amount)).ToList(),
                moveDocuments: true);

            return;
        }

        if (payment.SupplierId is { } supplierId)
        {
            var purchases = await _repository.GetOpenPurchasesForSupplierAsync(supplierId, cancellationToken);

            var plan = PaymentAllocationPlanner.Plan(
                payment.UnallocatedAmount,
                purchases.Select(p => new OpenDocument(p.Id, p.InvoiceDate, p.BalanceDue)));

            ApplyAllocations(
                payment,
                plan.Select(p => new AllocationTarget(
                    null, purchases.First(x => x.Id == p.DocumentId), p.Amount)).ToList(),
                moveDocuments: true);
        }

        // A walk-in has no account to settle against, so the money simply stands as collected.
    }

    private void ApplyAllocations(
        Payment payment, IReadOnlyList<AllocationTarget> targets, bool moveDocuments)
    {
        foreach (var target in targets)
        {
            var amount = Round(target.Amount);

            if (amount <= 0)
            {
                continue;
            }

            var allocation = new PaymentAllocation
            {
                PaymentId = payment.Id,
                InvoiceId = target.Invoice?.Id,
                PurchaseId = target.Purchase?.Id,
                CreditNoteId = target.CreditNote?.Id,
                DebitNoteId = target.DebitNote?.Id,
                DocumentNumber = target.Invoice?.InvoiceNumber
                                 ?? target.Purchase?.PurchaseNumber
                                 ?? target.CreditNote?.CreditNoteNumber
                                 ?? target.DebitNote?.DebitNoteNumber
                                 ?? string.Empty,
                DocumentDate = target.Invoice?.InvoiceDate
                               ?? target.Purchase?.InvoiceDate
                               ?? target.CreditNote?.NoteDate
                               ?? target.DebitNote?.NoteDate
                               ?? payment.PaymentDate,
                Amount = amount,
                AllocatedAt = DateTimeOffset.UtcNow,
            };

            payment.Allocations.Add(allocation);

            // Staged explicitly — see IPaymentRepository.AddAllocation for why the collection alone
            // is not enough once the payment is already tracked.
            _repository.AddAllocation(allocation);

            if (moveDocuments)
            {
                if (target.Invoice is { } invoice) ApplyToInvoice(invoice, amount);
                if (target.Purchase is { } purchase) ApplyToPurchase(purchase, amount);
                if (target.CreditNote is { } creditNote) ApplyToCreditNote(creditNote, amount);
                if (target.DebitNote is { } debitNote) ApplyToDebitNote(debitNote, amount);
            }
        }

        RecomputeTotals(payment);
    }

    private static void ReleaseAllocations(
        Payment payment, IReadOnlyList<PaymentAllocation> allocations, bool moveDocuments)
    {
        foreach (var allocation in allocations)
        {
            allocation.IsReversed = true;

            if (!moveDocuments)
            {
                continue;
            }

            if (allocation.Invoice is { } invoice) ApplyToInvoice(invoice, -allocation.Amount);
            if (allocation.Purchase is { } purchase) ApplyToPurchase(purchase, -allocation.Amount);
            if (allocation.CreditNote is { } creditNote) ApplyToCreditNote(creditNote, -allocation.Amount);
            if (allocation.DebitNote is { } debitNote) ApplyToDebitNote(debitNote, -allocation.Amount);
        }

        RecomputeTotals(payment);
    }

    private static void MoveDocuments(IEnumerable<PaymentAllocation> allocations, bool settling)
    {
        var sign = settling ? 1 : -1;

        foreach (var allocation in allocations)
        {
            if (allocation.Invoice is { } invoice) ApplyToInvoice(invoice, sign * allocation.Amount);
            if (allocation.Purchase is { } purchase) ApplyToPurchase(purchase, sign * allocation.Amount);
            if (allocation.CreditNote is { } creditNote) ApplyToCreditNote(creditNote, sign * allocation.Amount);
            if (allocation.DebitNote is { } debitNote) ApplyToDebitNote(debitNote, sign * allocation.Amount);
        }
    }

    /// <summary>
    /// The one place an invoice's paid figures move. Both columns always change together, which is
    /// what the database CHECK on <c>BalanceDue = GrandTotal - AmountPaid</c> holds them to.
    /// </summary>
    private static void ApplyToInvoice(Invoice invoice, decimal amount)
    {
        invoice.AmountPaid = Round(invoice.AmountPaid + amount);
        RecomputeInvoiceBalance(invoice);
    }

    /// <summary>
    /// The balance identity, in one place. Three terms: what was billed, less what was paid, less
    /// what came back as goods. A database check enforces the same expression, so anything that
    /// moves one term without coming through here fails at the save rather than silently.
    /// </summary>
    private static void RecomputeInvoiceBalance(Invoice invoice)
    {
        invoice.BalanceDue = Round(
            invoice.GrandTotal - invoice.AmountPaid - invoice.CreditAppliedAmount);

        invoice.UpdatedAt = DateTimeOffset.UtcNow;
    }

    private static void ApplyToPurchase(Purchase purchase, decimal amount)
    {
        purchase.AmountPaid = Round(purchase.AmountPaid + amount);
        RecomputePurchaseBalance(purchase);
    }

    private static void RecomputePurchaseBalance(Purchase purchase)
    {
        purchase.BalanceDue = Round(
            purchase.GrandTotal - purchase.AmountPaid - purchase.DebitAppliedAmount);

        purchase.UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Cash handed back against a return. The fourth instance of the house pattern: a denormalised
    /// total backed by allocation rows, so "which notes are still unrefunded" is a plain query.
    /// </summary>
    private static void ApplyToCreditNote(CreditNote note, decimal amount)
    {
        note.RefundedAmount = Round(note.RefundedAmount + amount);
        note.UpdatedAt = DateTimeOffset.UtcNow;
    }

    private static void ApplyToDebitNote(DebitNote note, decimal amount)
    {
        note.RefundedAmount = Round(note.RefundedAmount + amount);
        note.UpdatedAt = DateTimeOffset.UtcNow;
    }

    public decimal ApplyCredit(Invoice invoice, decimal creditAmount)
    {
        var applied = Math.Min(Round(creditAmount), Math.Max(0m, invoice.BalanceDue));

        invoice.CreditAppliedAmount = Round(invoice.CreditAppliedAmount + applied);

        // Called even when nothing was absorbed. A settled bill has no money column to move, so
        // without this EF would emit no UPDATE at all — and the xmin row version would never be
        // checked, letting two simultaneous returns against the same bill both read the same
        // balance and both apply against it.
        RecomputeInvoiceBalance(invoice);

        return applied;
    }

    public void ReleaseCredit(Invoice invoice, decimal creditAmount)
    {
        invoice.CreditAppliedAmount = Round(invoice.CreditAppliedAmount - Round(creditAmount));
        RecomputeInvoiceBalance(invoice);
    }

    public decimal ApplyDebit(Purchase purchase, decimal debitAmount)
    {
        var applied = Math.Min(Round(debitAmount), Math.Max(0m, purchase.BalanceDue));

        purchase.DebitAppliedAmount = Round(purchase.DebitAppliedAmount + applied);
        RecomputePurchaseBalance(purchase);

        return applied;
    }

    public void ReleaseDebit(Purchase purchase, decimal debitAmount)
    {
        purchase.DebitAppliedAmount = Round(purchase.DebitAppliedAmount - Round(debitAmount));
        RecomputePurchaseBalance(purchase);
    }

    private static void RecomputeTotals(Payment payment)
    {
        payment.AllocatedAmount = Round(payment.Allocations.Where(a => !a.IsReversed).Sum(a => a.Amount));
        payment.UnallocatedAmount = Round(payment.Amount - payment.AllocatedAmount);
    }

    /// <summary>
    /// Which way a payment moves a party's balance, given who the party is and which way the money
    /// went. A positive entry increases what is open on that account — a receivable for a customer,
    /// a payable for a supplier.
    /// </summary>
    /// <remarks>
    /// Derived rather than passed in. The four combinations do not share a sign, and the two that
    /// exist today — money in from a customer, money out to a supplier — happen to share one, which
    /// is why hard-coding it at the call sites worked for as long as it did. Refunding a customer is
    /// the third combination, and it is the one that would have gone the wrong way.
    /// </remarks>
    /// <summary>
    /// What the statement calls this movement. Money out to a customer is a refund, not a payment
    /// made — the latter reads as "we paid a supplier" and on a customer's own statement that is
    /// actively misleading.
    /// </summary>
    private static PartyLedgerEntryType EntryTypeFor(PaymentDirection direction, bool isCustomer) =>
        direction == PaymentDirection.Received
            ? PartyLedgerEntryType.PaymentReceived
            : isCustomer
                ? PartyLedgerEntryType.RefundPaid
                : PartyLedgerEntryType.PaymentMade;

    private static decimal SignedAmount(
        bool isCustomer, PaymentDirection direction, decimal amount, bool reversing)
    {
        var settles = isCustomer == (direction == PaymentDirection.Received) ? -1m : 1m;
        return amount * settles * (reversing ? -1m : 1m);
    }

    private async Task RecordPartyEntryAsync(
        Payment payment,
        Customer? customer,
        Supplier? supplier,
        decimal amount,
        bool reversing,
        PartyLedgerEntryType entryType,
        DateOnly entryDate,
        string? notes,
        CancellationToken cancellationToken)
    {
        var reference = payment.ReceiptNumber
            ?? payment.Allocations.FirstOrDefault()?.DocumentNumber;

        if (customer is not null)
        {
            await _partyLedger.RecordForCustomerAsync(
                customer,
                SignedAmount(isCustomer: true, payment.Direction, amount, reversing),
                entryType, entryDate, payment.Id, reference, notes, cancellationToken);
        }
        else if (supplier is not null)
        {
            await _partyLedger.RecordForSupplierAsync(
                supplier,
                SignedAmount(isCustomer: false, payment.Direction, amount, reversing),
                entryType, entryDate, payment.Id, reference, notes, cancellationToken);
        }

        // A walk-in has no account to move. The payment row still exists, so the cash book stays
        // complete — there is simply nobody to carry a balance.
    }

    /// <summary>
    /// Everything that must be true before a single field is touched, so a bad request leaves the
    /// documents exactly as it found them.
    /// </summary>
    private static void Validate(PaymentDraft draft)
    {
        var errors = new Dictionary<string, string[]>();

        if (draft.Amount <= 0)
        {
            errors["Amount"] = ["Enter an amount greater than zero"];
        }

        if (draft.Mode == PaymentMode.Credit)
        {
            errors["Mode"] = ["\"Credit\" means no money changed hands — pick how it was paid"];
        }

        if (draft.Customer is not null && draft.Supplier is not null)
        {
            errors["Party"] = ["A payment belongs to a customer or a supplier, not both"];
        }

        if (draft.Mode == PaymentMode.Cheque && draft.Cheque is null)
        {
            errors["Cheque"] = ["Enter the cheque number, bank and date"];
        }

        if (draft.Mode != PaymentMode.Cheque && draft.Cheque is not null)
        {
            errors["Cheque"] = ["Cheque details only belong on a cheque payment"];
        }

        if (draft.Cheque is { } cheque)
        {
            // A cheque dated far ahead is a typo; one dated far back will simply be refused at the
            // counter, and recording it as money in hand would overstate what the shop holds.
            if (cheque.ChequeDate > draft.PaymentDate.AddMonths(6))
            {
                errors["ChequeDate"] = ["That is more than six months ahead — check the date"];
            }

            if (cheque.ChequeDate < draft.PaymentDate.AddMonths(-3))
            {
                errors["ChequeDate"] = ["A cheque older than three months will not be accepted by the bank"];
            }
        }

        var allocated = Round(draft.Allocations.Sum(a => a.Amount));

        if (allocated > Round(draft.Amount))
        {
            errors["Allocations"] = ["Allocated more than the payment itself"];
        }

        foreach (var target in draft.Allocations)
        {
            // Four possible targets now, not two: a refund settles a credit or debit note the same
            // way a receipt settles a bill. Counting rather than comparing pairs, so adding a fifth
            // later cannot quietly leave a hole here.
            var targets = new object?[] { target.Invoice, target.Purchase, target.CreditNote, target.DebitNote };

            if (targets.Count(t => t is not null) != 1)
            {
                errors["Allocations"] = ["Each allocation settles exactly one document"];
                break;
            }
        }

        if (errors.Count > 0)
        {
            throw new ValidationAppException(errors);
        }
    }

    private static string FormatNumber(PaymentDirection direction, string financialYear, int sequence) =>
        direction == PaymentDirection.Received
            ? $"RCT/{financialYear}/{sequence:D4}"
            : $"PMT/{financialYear}/{sequence:D4}";

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static decimal Round(decimal value) =>
        Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
