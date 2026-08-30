using Application.Common;
using Application.Common.Exceptions;
using Application.DTOs.Reports;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;

namespace Application.Services;

/// <summary>
/// Builds the registers the shop hands to its accountant, and the GST return shapes.
/// <para>
/// Every one of these reads stored documents and prints what they say. Nothing here recomputes tax
/// or re-applies a discount: the register has to agree with the paper the customer was given, even
/// where a later bug fix would have produced a different figure.
/// </para>
/// </summary>
public class ReportService : IReportService
{
    private readonly IReportRepository _repository;
    private readonly IShopSettingsRepository _shopSettings;
    private readonly ICurrentUser _currentUser;

    private readonly IShopClock _clock;

    public ReportService(
        IReportRepository repository,
        IShopSettingsRepository shopSettings,
        ICurrentUser currentUser,
        IShopClock clock)
    {
        _repository = repository;
        _shopSettings = shopSettings;
        _currentUser = currentUser;
        _clock = clock;
    }

    private static readonly RegisterSummaryDto[] Registers =
    [
        new("sales", "Sales Register", "Every bill raised, with its tax split", "Books"),
        new("purchase", "Purchase Register", "Every supplier bill entered", "Books"),
        new("sales-returns", "Sales Return Register", "Credit notes issued to customers", "Books"),
        new("purchase-returns", "Purchase Return Register", "Debit notes raised on suppliers", "Books"),
        new("receipts", "Receipt & Payment Register", "Money in and money out, by mode", "Books"),
        new("expenses", "Expense Register", "Rent, salary, freight and the rest", "Books"),

        new("ageing", "Receivables Ageing", "Who owes the shop, and how long it has been sitting there", "Balances", IsAsAt: true),
        new("outstanding", "Party Outstanding", "Who owes the shop, and who the shop owes, as at the date chosen", "Balances", IsAsAt: true),
        new("stock-valuation", "Stock Valuation", "What was on the shelf on the date chosen, and what it cost", "Balances", IsAsAt: true),
        new("stock-movement", "Stock Movement", "Every in and out, with its reference", "Balances"),

        new("gstr1-b2b", "GSTR-1 · B2B", "Bills to GSTIN holders, invoice by invoice", "GST"),
        new("gstr1-b2cl", "GSTR-1 · B2C Large",
            "Inter-state bills over ₹2.5 lakh to unregistered buyers, invoice by invoice", "GST"),
        new("gstr1-b2cs", "GSTR-1 · B2C Small", "Every other counter sale, summarised by state and rate", "GST"),
        new("gstr1-cdnr", "GSTR-1 · Credit / Debit Notes", "Notes against registered parties", "GST"),
        new("gstr1-nil", "GSTR-1 · Nil, Exempt, Non-GST",
            "Table 8 — supplies that carry no tax, kept out of taxable turnover", "GST"),
        new("gstr1-hsn", "GSTR-1 · HSN Summary", "Table 12 — quantity and value by HSN", "GST"),
        new("gstr1-docs", "GSTR-1 · Documents Issued",
            "Table 13 — the number series filed, and how many of each were cancelled", "GST"),
        new("gstr3b", "GSTR-3B Summary",
            "Outward tax and input credit, head by head. Credit is not set off across heads — the order IGST credit is used in is your accountant's call",
            "GST"),
    ];

    public IReadOnlyList<RegisterSummaryDto> GetRegisters() => Registers;

    public async Task<RegisterDto> BuildAsync(RegisterQuery query, CancellationToken cancellationToken)
    {
        _currentUser.Require(Permission.ReportView, "see the registers");

        var summary = Registers.FirstOrDefault(r => r.Key == query.Key)
            ?? throw new NotFoundException($"'{query.Key}' is not a register this app knows", "REGISTER_NOT_FOUND");

        if (query.ToDate < query.FromDate)
        {
            throw new ValidationAppException(new Dictionary<string, string[]>
            {
                ["ToDate"] = ["The end of the range comes before its start"],
            });
        }

        var builder = new RegisterBuilder();

        switch (query.Key)
        {
            case "sales": await Sales(builder, query, cancellationToken); break;
            case "purchase": await Purchases(builder, query, cancellationToken); break;
            case "sales-returns": await SalesReturns(builder, query, cancellationToken); break;
            case "purchase-returns": await PurchaseReturns(builder, query, cancellationToken); break;
            case "receipts": await Receipts(builder, query, cancellationToken); break;
            case "expenses": await Expenses(builder, query, cancellationToken); break;
            case "outstanding": await Outstanding(builder, query, cancellationToken); break;
            case "ageing": await Ageing(builder, query, cancellationToken); break;
            case "stock-valuation":
                // The only register that prints what the shop paid. Someone who may read the sales
                // register still has no business reading the shelf's cost.
                _currentUser.Require(Permission.CostView, "see what the stock cost");
                await StockValuation(builder, query, cancellationToken);
                break;
            case "stock-movement": await StockMovements(builder, query, cancellationToken); break;
            case "gstr1-b2b": await Gstr1B2B(builder, query, cancellationToken); break;
            case "gstr1-b2cl": await Gstr1B2Cl(builder, query, cancellationToken); break;
            case "gstr1-b2cs": await Gstr1B2Cs(builder, query, cancellationToken); break;
            case "gstr1-nil": await Gstr1Nil(builder, query, cancellationToken); break;
            case "gstr1-docs": await Gstr1Documents(builder, query, cancellationToken); break;
            case "gstr1-cdnr": await Gstr1Cdnr(builder, query, cancellationToken); break;
            case "gstr1-hsn": await Gstr1Hsn(builder, query, cancellationToken); break;
            case "gstr3b": await Gstr3B(builder, query, cancellationToken); break;
        }

        // A position register answers as at today whatever range came in, so it reports today's date
        // back rather than echoing a range it ignored. Printing 01-04-2026 above a stock figure that
        // is current would be the register lying about what it counted.
        var today = _clock.Today;
        var asAt = AsAtDate(query, today);

        return builder.Build(
            summary.Key,
            summary.Title,
            summary.Caption,
            summary.IsAsAt ? asAt : query.FromDate,
            summary.IsAsAt ? asAt : query.ToDate,
            summary.IsAsAt);
    }

    /// <summary>
    /// The day a position register answers for: the end of the range asked for, never later than
    /// today.
    /// <para>
    /// These registers used to answer for today whatever was asked, which made the two questions an
    /// accountant opens with — what was on the shelf on 31 March, and who owed what — impossible to
    /// answer from the app at all. A future date is capped rather than refused: a range that runs to
    /// the year end is an ordinary thing to ask for in September, and the honest answer is today's
    /// position, labelled today.
    /// </para>
    /// </summary>
    private static DateOnly AsAtDate(RegisterQuery query, DateOnly today) =>
        query.ToDate > today || query.ToDate == default ? today : query.ToDate;

    // ---------------------------------------------------------------- Books

    private async Task Sales(RegisterBuilder b, RegisterQuery q, CancellationToken ct)
    {
        b.Date("date", "Date")
            .Text("number", "Invoice No")
            .Text("party", "Customer")
            .Text("gstin", "GSTIN")
            .Text("place", "Place of Supply")
            .Money("taxable", "Taxable")
            .Money("cgst", "CGST")
            .Money("sgst", "SGST")
            .Money("igst", "IGST")
            .Money("roundOff", "Round Off")
            .Money("total", "Invoice Total")
            .Money("paid", "Paid")
            // Without this column the register does not add up: a bill settled by a credit note
            // shows a total, nothing paid and nothing outstanding, and the reader is left to guess
            // where the money went. A bill is settled three ways — cash, credit note, still owed —
            // and all three have to be on the page.
            .Money("creditApplied", "Credit Note")
            .Money("balance", "Balance")
            .Text("status", "Status")
            .Text("by", "Entered By");

        foreach (var i in await _repository.GetInvoicesAsync(q.FromDate, q.ToDate, withItems: false, ct))
        {
            var live = i.Status != InvoiceStatus.Cancelled;

            b.Row(
                ("date", i.InvoiceDate),
                ("number", i.InvoiceNumber),
                ("party", i.CustomerName),
                ("gstin", i.CustomerGstin),
                ("place", i.CustomerStateCode),
                // A cancelled bill contributes nothing to any total, but keeps its row so the
                // number series reads unbroken.
                ("taxable", live ? i.TaxableAmount : 0m),
                ("cgst", live ? i.CgstAmount : 0m),
                ("sgst", live ? i.SgstAmount : 0m),
                ("igst", live ? i.IgstAmount : 0m),
                ("roundOff", live ? i.RoundOff : 0m),
                ("total", live ? i.GrandTotal : 0m),
                ("paid", live ? i.AmountPaid : 0m),
                ("creditApplied", live ? i.CreditAppliedAmount : 0m),
                ("balance", live ? i.BalanceDue : 0m),
                ("status", i.Status.ToString()),
                ("by", i.CreatedByName));
        }
    }

    private async Task Purchases(RegisterBuilder b, RegisterQuery q, CancellationToken ct)
    {
        b.Date("date", "Date")
            .Text("number", "Entry No")
            .Text("supplierBill", "Supplier Bill No")
            .Text("party", "Supplier")
            .Text("gstin", "GSTIN")
            .Money("taxable", "Taxable")
            .Money("cgst", "CGST")
            .Money("sgst", "SGST")
            .Money("igst", "IGST")
            .Money("total", "Bill Total")
            .Money("paid", "Paid")
            .Money("debitApplied", "Debit Note")
            .Money("balance", "Balance")
            .Text("status", "Status")
            .Text("by", "Entered By");

        foreach (var p in await _repository.GetPurchasesAsync(q.FromDate, q.ToDate, ct))
        {
            var live = p.Status != PurchaseStatus.Cancelled;

            b.Row(
                ("date", p.InvoiceDate),
                ("number", p.PurchaseNumber),
                ("supplierBill", p.SupplierInvoiceNumber),
                ("party", p.SupplierName),
                ("gstin", p.SupplierGstin),
                ("taxable", live ? p.TaxableAmount : 0m),
                ("cgst", live ? p.CgstAmount : 0m),
                ("sgst", live ? p.SgstAmount : 0m),
                ("igst", live ? p.IgstAmount : 0m),
                ("total", live ? p.GrandTotal : 0m),
                ("paid", live ? p.AmountPaid : 0m),
                ("debitApplied", live ? p.DebitAppliedAmount : 0m),
                ("balance", live ? p.BalanceDue : 0m),
                ("status", p.Status.ToString()),
                ("by", p.CreatedByName));
        }
    }

    private async Task SalesReturns(RegisterBuilder b, RegisterQuery q, CancellationToken ct)
    {
        b.Date("date", "Date")
            .Text("number", "Credit Note No")
            .Text("against", "Against Invoice")
            .Text("party", "Customer")
            .Text("gstin", "GSTIN")
            .Text("reason", "Reason")
            .Money("taxable", "Taxable")
            .Money("cgst", "CGST")
            .Money("sgst", "SGST")
            .Money("igst", "IGST")
            .Money("total", "Note Total")
            .Money("applied", "Set Off Against Bill")
            .Money("refunded", "Refunded")
            // The third way a note is settled, and the one nobody thinks of: neither knocked off the
            // bill nor handed back in cash, but left sitting as credit the customer can spend next
            // visit. Without this column the register shows a note worth ₹983 that apparently went
            // nowhere.
            .Money("onAccount", "Left On Account")
            .Text("status", "Status");

        foreach (var n in await _repository.GetCreditNotesAsync(q.FromDate, q.ToDate, ct))
        {
            var live = n.Status != CreditNoteStatus.Cancelled;

            b.Row(
                ("date", n.NoteDate),
                ("number", n.CreditNoteNumber),
                ("against", n.InvoiceNumber),
                ("party", n.CustomerName),
                ("gstin", n.CustomerGstin),
                ("reason", n.Reason),
                ("taxable", live ? n.TaxableAmount : 0m),
                ("cgst", live ? n.CgstAmount : 0m),
                ("sgst", live ? n.SgstAmount : 0m),
                ("igst", live ? n.IgstAmount : 0m),
                ("total", live ? n.GrandTotal : 0m),
                ("applied", live ? n.AppliedToInvoiceAmount : 0m),
                ("refunded", live ? n.RefundedAmount : 0m),
                ("onAccount", live ? n.GrandTotal - n.AppliedToInvoiceAmount - n.RefundedAmount : 0m),
                ("status", n.Status.ToString()));
        }
    }

    private async Task PurchaseReturns(RegisterBuilder b, RegisterQuery q, CancellationToken ct)
    {
        b.Date("date", "Date")
            .Text("number", "Debit Note No")
            .Text("against", "Against Purchase")
            .Text("party", "Supplier")
            .Text("gstin", "GSTIN")
            .Text("reason", "Reason")
            .Money("taxable", "Taxable")
            .Money("cgst", "CGST")
            .Money("sgst", "SGST")
            .Money("igst", "IGST")
            .Money("total", "Note Total")
            .Money("applied", "Set Off Against Bill")
            .Money("refunded", "Refunded")
            .Money("onAccount", "Left On Account")
            .Text("status", "Status");

        foreach (var n in await _repository.GetDebitNotesAsync(q.FromDate, q.ToDate, ct))
        {
            var live = n.Status != DebitNoteStatus.Cancelled;

            b.Row(
                ("date", n.NoteDate),
                ("number", n.DebitNoteNumber),
                ("against", n.PurchaseNumber),
                ("party", n.SupplierName),
                ("gstin", n.SupplierGstin),
                ("reason", n.Reason),
                ("taxable", live ? n.TaxableAmount : 0m),
                ("cgst", live ? n.CgstAmount : 0m),
                ("sgst", live ? n.SgstAmount : 0m),
                ("igst", live ? n.IgstAmount : 0m),
                ("total", live ? n.GrandTotal : 0m),
                ("applied", live ? n.AppliedToPurchaseAmount : 0m),
                ("refunded", live ? n.RefundedAmount : 0m),
                ("onAccount", live ? n.GrandTotal - n.AppliedToPurchaseAmount - n.RefundedAmount : 0m),
                ("status", n.Status.ToString()));
        }
    }

    private async Task Receipts(RegisterBuilder b, RegisterQuery q, CancellationToken ct)
    {
        b.Date("date", "Date")
            .Text("number", "Receipt No")
            .Text("against", "Against")
            .Text("direction", "In / Out")
            .Text("party", "Party")
            .Text("mode", "Mode")
            .Text("reference", "Reference")
            .Money("received", "Received")
            .Money("paid", "Paid Out")
            .Money("unallocated", "On Account", total: false)
            .Text("status", "Status");

        foreach (var p in await _repository.GetPaymentsAsync(q.FromDate, q.ToDate, ct))
        {
            // Reversed covers both a cancellation and a bounced cheque; Pending is a post-dated
            // cheque that has touched no balance yet. Only a posted receipt is money the shop has.
            var live = p.Status == PaymentStatus.Posted;
            var incoming = p.Direction == PaymentDirection.Received;

            // The documents this money settled, from the allocation's own snapshot.
            //
            // A live payment shows what it is against; a reversed one shows what it *was* against,
            // because every one of its allocations is reversed and filtering them out would leave
            // the row with a zero and nothing to identify it by — exactly the row somebody is
            // looking for when they ask what came back.
            var relevant = live ? p.Allocations.Where(a => !a.IsReversed) : p.Allocations;

            var against = string.Join(", ", relevant.Select(a => a.DocumentNumber).Distinct());

            b.Row(
                ("date", p.PaymentDate),
                // Counter money has no receipt number of its own. Saying "With bill" is honest about
                // why the cell is empty, where a blank reads as a missing record.
                ("number", p.ReceiptNumber ?? (p.IsCounterPayment ? "With bill" : null)),
                ("against", against.Length == 0 ? null : against),
                ("direction", incoming ? "Received" : "Paid"),
                ("party", p.PartyName),
                ("mode", p.Mode.ToString()),
                ("reference", p.ReferenceNumber),
                ("received", live && incoming ? p.Amount : 0m),
                ("paid", live && !incoming ? p.Amount : 0m),
                ("unallocated", live ? p.UnallocatedAmount : 0m),
                ("status", p.Status.ToString()));
        }
    }

    private async Task Expenses(RegisterBuilder b, RegisterQuery q, CancellationToken ct)
    {
        b.Date("date", "Date")
            .Text("number", "Voucher No")
            .Text("category", "Category")
            .Text("paidTo", "Paid To")
            .Text("mode", "Mode")
            .Text("reference", "Reference")
            .Money("amount", "Amount")
            .Text("notes", "Notes")
            .Text("by", "Entered By");

        foreach (var e in await _repository.GetExpensesAsync(q.FromDate, q.ToDate, ct))
        {
            b.Row(
                ("date", e.ExpenseDate),
                ("number", e.ExpenseNumber),
                ("category", e.Category.ToString()),
                ("paidTo", e.PaidTo),
                ("mode", e.Mode.ToString()),
                ("reference", e.ReferenceNumber),
                ("amount", e.IsCancelled ? 0m : e.Amount),
                ("notes", e.IsCancelled ? "Cancelled" : e.Notes),
                ("by", e.CreatedByName));
        }
    }

    // ------------------------------------------------------------- Balances

    /// <summary>
    /// Every open bill, bucketed by how long it has been past its due date.
    /// <para>
    /// The dashboard already carries one over-60 figure, which answers "is there a problem" and not
    /// "who do I ring". A running balance cannot say how old any part of it is, so this is built
    /// from the bills themselves — which is also the shape an accountant asks for as the debtors
    /// schedule.
    /// </para>
    /// <para>
    /// Age runs from the due date, not the invoice date, so a customer on thirty-day terms is not
    /// called overdue on the day he is billed.
    /// </para>
    /// </summary>
    private async Task Ageing(RegisterBuilder b, RegisterQuery q, CancellationToken ct)
    {
        b.Text("party", "Customer")
            .Text("phone", "Phone")
            .Money("notDue", "Not Yet Due", total: true)
            .Money("upTo30", "1-30 Days", total: true)
            .Money("upTo60", "31-60 Days", total: true)
            .Money("upTo90", "61-90 Days", total: true)
            .Money("over90", "Over 90 Days", total: true)
            .Money("total", "Total Owed", total: true)
            .Text("oldest", "Oldest Bill");

        var asAt = AsAtDate(q, _clock.Today);
        var buckets = new Dictionary<Guid, (string Name, string? Phone, decimal[] Ages, DateOnly Oldest, string OldestNumber)>();

        foreach (var i in await _repository.GetOpenInvoicesAsync(ct))
        {
            if (i.CustomerId is not { } id)
            {
                continue;
            }

            var due = i.DueDate ?? i.InvoiceDate;
            var daysOver = asAt.DayNumber - due.DayNumber;

            var slot = daysOver switch
            {
                <= 0 => 0,
                <= 30 => 1,
                <= 60 => 2,
                <= 90 => 3,
                _ => 4,
            };

            var row = buckets.GetValueOrDefault(
                id, (i.CustomerName, null, new decimal[5], i.InvoiceDate, i.InvoiceNumber));

            row.Ages[slot] += i.BalanceDue;

            if (i.InvoiceDate < row.Oldest)
            {
                row = row with { Oldest = i.InvoiceDate, OldestNumber = i.InvoiceNumber };
            }

            buckets[id] = row;
        }

        var (customers, _) = await _repository.GetOpenPartiesAsync(ct);
        var phones = customers.ToDictionary(c => c.Id, c => c.Phone);

        // Worst first: the register exists to be worked down, and the ninety-day column is the one
        // somebody is going to ring about this morning.
        foreach (var (id, row) in buckets.OrderByDescending(x => x.Value.Ages[4])
                                         .ThenByDescending(x => x.Value.Ages[3])
                                         .ThenByDescending(x => x.Value.Ages.Sum()))
        {
            b.Row(
                ("party", row.Name),
                ("phone", phones.GetValueOrDefault(id)),
                ("notDue", Money(row.Ages[0])),
                ("upTo30", Money(row.Ages[1])),
                ("upTo60", Money(row.Ages[2])),
                ("upTo90", Money(row.Ages[3])),
                ("over90", Money(row.Ages[4])),
                ("total", Money(row.Ages.Sum())),
                ("oldest", $"{row.OldestNumber} · {row.Oldest:dd MMM yyyy}"));
        }
    }

    private async Task Outstanding(RegisterBuilder b, RegisterQuery q, CancellationToken ct)
    {
        b.Text("type", "Party Type")
            .Text("name", "Name")
            .Text("phone", "Phone")
            .Text("gstin", "GSTIN")
            .Money("receivable", "Receivable")
            .Money("payable", "Payable");

        var (customers, suppliers) = await _repository.GetOpenPartiesAsync(ct);

        var asAt = AsAtDate(q, _clock.Today);
        var isToday = asAt >= _clock.Today;

        // Today's answer is the balance the party already carries. A past date has to be rebuilt
        // from the ledger, because the carried balance only knows about now — and "who owed what at
        // the year end" is the first schedule an accountant asks for.
        var owedByCustomers = isToday ? null : await _repository.GetPartyBalancesOnAsync(asAt, true, ct);
        var owedToSuppliers = isToday ? null : await _repository.GetPartyBalancesOnAsync(asAt, false, ct);

        foreach (var c in customers)
        {
            var balance = isToday ? c.OutstandingBalance : owedByCustomers!.GetValueOrDefault(c.Id);

            if (!isToday && balance == 0)
            {
                continue;
            }

            // A customer with a negative balance is holding an advance, which is money the shop
            // owes back — it belongs in the payable column, not as a negative receivable.
            b.Row(
                ("type", "Customer"),
                ("name", c.Name),
                ("phone", c.Phone),
                ("gstin", c.Gstin),
                ("receivable", balance > 0 ? balance : 0m),
                ("payable", balance < 0 ? -balance : 0m));
        }

        foreach (var s in suppliers)
        {
            var balance = isToday ? s.OutstandingBalance : owedToSuppliers!.GetValueOrDefault(s.Id);

            if (!isToday && balance == 0)
            {
                continue;
            }

            b.Row(
                ("type", "Supplier"),
                ("name", s.Name),
                ("phone", s.Phone),
                ("gstin", s.Gstin),
                ("receivable", balance < 0 ? -balance : 0m),
                ("payable", balance > 0 ? balance : 0m));
        }
    }

    private async Task StockValuation(RegisterBuilder b, RegisterQuery q, CancellationToken ct)
    {
        b.Text("partNumber", "Part No")
            .Text("itemName", "Item")
            .Text("hsn", "HSN")
            .Text("uqc", "UQC")
            .Quantity("stock", "On Hand", total: true)
            .Money("purchaseRate", "Purchase Rate", total: false)
            .Money("value", "Value At Cost")
            .Money("sellingRate", "Selling Rate", total: false)
            .Money("saleValue", "Value At Selling Rate")
            .Text("active", "Active");

        var asAt = AsAtDate(q, _clock.Today);
        var isToday = asAt >= _clock.Today;

        // For today the shelf's own figure is the answer and no query is needed. For a past date it
        // has to come from the movements, because StockOnHand only ever knows about now.
        var balances = isToday
            ? null
            : await _repository.GetStockBalancesOnAsync(asAt, ct);

        foreach (var p in await _repository.GetProductsForValuationAsync(ct))
        {
            var held = isToday ? p.StockOnHand : balances!.GetValueOrDefault(p.Id);

            // A part the shop had not started stocking on that date is left off rather than
            // printed as a zero line, so a year-end valuation is the length it should be.
            if (!isToday && held == 0)
            {
                continue;
            }

            // Valued at the rate on the item master today, not at what it cost on the date asked
            // for — costing here is a snapshot taken when goods are sold, not a running cost
            // ledger. See decisions.md. It is the right figure for a shelf whose rates have not
            // moved, and an approximation for one whose have.
            b.Row(
                ("partNumber", p.PartNumber),
                ("itemName", p.ItemName),
                ("hsn", p.Hsn),
                ("uqc", p.Uqc),
                ("stock", held),
                ("purchaseRate", p.PurchaseRate),
                ("value", Money(held * p.PurchaseRate)),
                ("sellingRate", p.SellingRate),
                ("saleValue", Money(held * p.SellingRate)),
                ("active", p.IsActive));
        }
    }

    private async Task StockMovements(RegisterBuilder b, RegisterQuery q, CancellationToken ct)
    {
        b.Text("when", "When")
            .Text("partNumber", "Part No")
            .Text("itemName", "Item")
            .Text("type", "Movement")
            .Text("reason", "Reason")
            .Quantity("in", "In", total: true)
            .Quantity("out", "Out", total: true)
            .Quantity("balance", "Balance After")
            .Text("reference", "Reference")
            .Text("notes", "Notes");

        var movements = await _repository.GetMovementsAsync(q.FromDate, q.ToDate, ct);

        // The balance is computed down the page, not read off the row.
        //
        // StockMovement.BalanceAfter records the total at the moment the row was *written*. This
        // register reads in the order things *happened*, and the two stopped agreeing the day a
        // back-dated bill was keyed: its row sits in the 5th but its stored balance was worked out
        // on the 20th. Exactly the trap the party statement already hit — see the ledger note in
        // docs/architecture.md — and the answer is the same one, run the figure forward here.
        //
        // Opening comes from everything before the range, so the first row of a filtered register
        // still starts from the truth rather than from zero.
        var balances = new Dictionary<Guid, decimal>();

        foreach (var productId in movements.Select(m => m.ProductId).Distinct())
        {
            balances[productId] = await _repository.GetStockBalanceBeforeAsync(productId, q.FromDate, ct);
        }

        foreach (var m in movements)
        {
            var balance = balances[m.ProductId] + m.Quantity;
            balances[m.ProductId] = balance;

            b.Row(
                ("when", m.MovementDate),
                ("partNumber", m.PartNumber),
                ("itemName", m.ItemName),
                ("type", m.MovementType.ToString()),
                ("reason", m.AdjustmentReason?.ToString()),
                ("in", m.Quantity > 0 ? m.Quantity : 0m),
                ("out", m.Quantity < 0 ? -m.Quantity : 0m),
                ("balance", balance),
                ("reference", m.ReferenceNumber),
                ("notes", m.Notes));
        }
    }

    // ------------------------------------------------------------------ GST

    private async Task Gstr1B2B(RegisterBuilder b, RegisterQuery q, CancellationToken ct)
    {
        b.Text("gstin", "GSTIN of Recipient")
            .Text("party", "Receiver Name")
            .Text("number", "Invoice Number")
            .Date("date", "Invoice Date")
            // Not totalled. The return wants one line per tax rate and repeats the invoice value on
            // each of them — that is the portal's own shape — so adding the column up counts a
            // two-rate bill twice. The taxable column is the one that sums to something real.
            .Money("invoiceValue", "Invoice Value", total: false)
            .Text("place", "Place Of Supply")
            .Text("reverseCharge", "Reverse Charge")
            .Text("invoiceType", "Invoice Type")
            .Number("rate", "Rate")
            .Money("taxable", "Taxable Value")
            .Money("cess", "Cess", total: false);

        var shop = await _shopSettings.GetAsync(ct);

        foreach (var i in await _repository.GetInvoicesAsync(q.FromDate, q.ToDate, withItems: true, ct))
        {
            // B2B is defined by the recipient having a GSTIN. Everything else is B2C and belongs in
            // the summary table instead, so filtering here is what keeps the two from double-counting.
            if (i.Status == InvoiceStatus.Cancelled || string.IsNullOrWhiteSpace(i.CustomerGstin))
            {
                continue;
            }

            // The return wants one line per tax rate, not per item — the portal rejects a file that
            // repeats an invoice number against the same rate. Untaxed lines are left out here and
            // reported in Table 8 instead; counting them as taxable overstates turnover.
            foreach (var group in i.Items
                         .Where(x => x.SupplyType == SupplyType.Taxable)
                         .GroupBy(x => x.GstRate)
                         .OrderBy(g => g.Key))
            {
                b.Row(
                    ("gstin", i.CustomerGstin),
                    ("party", i.CustomerName),
                    ("number", i.InvoiceNumber),
                    ("date", i.InvoiceDate),
                    ("invoiceValue", i.GrandTotal),
                    ("place", PlaceOfSupply(i.CustomerStateCode, shop.StateCode)),
                    ("reverseCharge", "N"),
                    ("invoiceType", "Regular B2B"),
                    ("rate", group.Key),
                    ("taxable", Money(group.Sum(x => x.TaxableAmount))),
                    ("cess", 0m));
            }
        }
    }

    private async Task Gstr1B2Cs(RegisterBuilder b, RegisterQuery q, CancellationToken ct)
    {
        b.Text("type", "Type")
            .Text("place", "Place Of Supply")
            .Number("rate", "Rate")
            .Money("taxable", "Taxable Value")
            .Money("cess", "Cess", total: false);

        var shop = await _shopSettings.GetAsync(ct);
        var totals = new Dictionary<(string Place, decimal Rate), decimal>();

        foreach (var i in await _repository.GetInvoicesAsync(q.FromDate, q.ToDate, withItems: true, ct))
        {
            // Large inter-state sales to unregistered buyers go invoice by invoice into B2CL, so
            // reporting them here as well would count the same supply twice.
            if (i.Status == InvoiceStatus.Cancelled
                || !string.IsNullOrWhiteSpace(i.CustomerGstin)
                || IsB2CLarge(i, shop.StateCode))
            {
                continue;
            }

            var place = PlaceOfSupply(i.CustomerStateCode, shop.StateCode);

            foreach (var item in i.Items.Where(x => x.SupplyType == SupplyType.Taxable))
            {
                var key = (place, item.GstRate);
                totals[key] = totals.GetValueOrDefault(key) + item.TaxableAmount;
            }
        }

        foreach (var ((place, rate), taxable) in totals.OrderBy(t => t.Key.Place).ThenBy(t => t.Key.Rate))
        {
            b.Row(
                ("type", "OE"),
                ("place", place),
                ("rate", rate),
                ("taxable", Money(taxable)),
                ("cess", 0m));
        }
    }

    private async Task Gstr1Cdnr(RegisterBuilder b, RegisterQuery q, CancellationToken ct)
    {
        b.Text("gstin", "GSTIN of Recipient")
            .Text("party", "Receiver Name")
            .Text("noteNumber", "Note Number")
            .Date("noteDate", "Note Date")
            .Text("noteType", "Note Type")
            .Text("against", "Against Invoice")
            .Date("againstDate", "Invoice Date")
            .Text("place", "Place Of Supply")
            .Money("noteValue", "Note Value")
            .Number("rate", "Rate")
            .Money("taxable", "Taxable Value");

        var shop = await _shopSettings.GetAsync(ct);

        foreach (var n in await _repository.GetCreditNotesAsync(q.FromDate, q.ToDate, ct))
        {
            if (n.Status == CreditNoteStatus.Cancelled || string.IsNullOrWhiteSpace(n.CustomerGstin))
            {
                continue;
            }

            // Notes are reported against a registered recipient only. Rate is taken from the note's
            // own totals rather than its lines, since a note is issued at the rate its bill carried.
            b.Row(
                ("gstin", n.CustomerGstin),
                ("party", n.CustomerName),
                ("noteNumber", n.CreditNoteNumber),
                ("noteDate", n.NoteDate),
                ("noteType", "C"),
                ("against", n.InvoiceNumber),
                ("againstDate", n.InvoiceDate),
                ("place", PlaceOfSupply(n.CustomerStateCode, shop.StateCode)),
                ("noteValue", n.GrandTotal),
                ("rate", EffectiveRate(n.TaxableAmount, n.TotalTax)),
                ("taxable", n.TaxableAmount));
        }
    }

    private async Task Gstr1Hsn(RegisterBuilder b, RegisterQuery q, CancellationToken ct)
    {
        b.Text("hsn", "HSN")
            .Text("description", "Description")
            .Text("uqc", "UQC")
            .Quantity("quantity", "Total Quantity", total: true)
            .Money("value", "Total Value")
            .Number("rate", "Rate")
            .Money("taxable", "Taxable Value")
            .Money("cgst", "CGST")
            .Money("sgst", "SGST")
            .Money("igst", "IGST");

        // Table 12 is grouped by HSN, UQC and rate together — the same part sold at two rates in a
        // period is two lines, and the portal will not accept them merged.
        var groups = new Dictionary<(string Hsn, string Uqc, decimal Rate),
            (string Description, decimal Quantity, decimal Value, decimal Taxable, decimal Cgst, decimal Sgst, decimal Igst)>();

        foreach (var i in await _repository.GetInvoicesAsync(q.FromDate, q.ToDate, withItems: true, ct))
        {
            if (i.Status == InvoiceStatus.Cancelled)
            {
                continue;
            }

            foreach (var item in i.Items)
            {
                var key = (item.Hsn, item.Uqc, item.GstRate);
                var current = groups.GetValueOrDefault(key);

                groups[key] = (
                    current.Description ?? item.ItemName,
                    current.Quantity + item.Quantity,
                    current.Value + item.LineTotal,
                    current.Taxable + item.TaxableAmount,
                    current.Cgst + item.CgstAmount,
                    current.Sgst + item.SgstAmount,
                    current.Igst + item.IgstAmount);
            }
        }

        foreach (var (key, value) in groups.OrderBy(g => g.Key.Hsn).ThenBy(g => g.Key.Rate))
        {
            b.Row(
                ("hsn", key.Hsn),
                ("description", value.Description),
                ("uqc", key.Uqc),
                ("quantity", value.Quantity),
                ("value", Money(value.Value)),
                ("rate", key.Rate),
                ("taxable", Money(value.Taxable)),
                ("cgst", Money(value.Cgst)),
                ("sgst", Money(value.Sgst)),
                ("igst", Money(value.Igst)));
        }
    }

    /// <summary>
    /// Inter-state, to somebody with no GSTIN, and over the threshold — the three things together
    /// are what makes a supply B2C Large, reportable invoice by invoice rather than summarised.
    /// </summary>
    private const decimal B2CLargeThreshold = 250_000m;

    private static bool IsB2CLarge(Invoice invoice, string shopStateCode) =>
        string.IsNullOrWhiteSpace(invoice.CustomerGstin)
        && invoice.IsInterState
        && invoice.GrandTotal > B2CLargeThreshold;

    private async Task Gstr1B2Cl(RegisterBuilder b, RegisterQuery q, CancellationToken ct)
    {
        b.Text("place", "Place Of Supply")
            .Text("number", "Invoice Number")
            .Date("date", "Invoice Date")
            .Money("invoiceValue", "Invoice Value", total: false)
            .Number("rate", "Rate")
            .Money("taxable", "Taxable Value")
            .Money("cess", "Cess", total: false)
            .Text("ecommerce", "E-Commerce GSTIN");

        var shop = await _shopSettings.GetAsync(ct);

        foreach (var i in await _repository.GetInvoicesAsync(q.FromDate, q.ToDate, withItems: true, ct))
        {
            if (i.Status == InvoiceStatus.Cancelled || !IsB2CLarge(i, shop.StateCode))
            {
                continue;
            }

            foreach (var group in i.Items
                         .Where(x => x.SupplyType == SupplyType.Taxable)
                         .GroupBy(x => x.GstRate)
                         .OrderBy(g => g.Key))
            {
                b.Row(
                    ("place", PlaceOfSupply(i.CustomerStateCode, shop.StateCode)),
                    ("number", i.InvoiceNumber),
                    ("date", i.InvoiceDate),
                    ("invoiceValue", i.GrandTotal),
                    ("rate", group.Key),
                    ("taxable", Money(group.Sum(x => x.TaxableAmount))),
                    ("cess", 0m),
                    ("ecommerce", null));
            }
        }
    }

    /// <summary>
    /// Table 8 — the supplies that carry no tax, split the way the return splits them and by whether
    /// the buyer was registered.
    /// </summary>
    private async Task Gstr1Nil(RegisterBuilder b, RegisterQuery q, CancellationToken ct)
    {
        b.Text("description", "Description")
            .Money("nilInter", "Nil Rated · Inter-state")
            .Money("nilIntra", "Nil Rated · Intra-state")
            .Money("exemptInter", "Exempted · Inter-state")
            .Money("exemptIntra", "Exempted · Intra-state")
            .Money("nonGstInter", "Non-GST · Inter-state")
            .Money("nonGstIntra", "Non-GST · Intra-state");

        // Registered and unregistered buyers are two separate rows in the table, so they are
        // accumulated apart rather than added up and split afterwards.
        var rows = new Dictionary<string, decimal[]>
        {
            ["Inter-state supplies to registered persons"] = new decimal[6],
            ["Intra-state supplies to registered persons"] = new decimal[6],
            ["Inter-state supplies to unregistered persons"] = new decimal[6],
            ["Intra-state supplies to unregistered persons"] = new decimal[6],
        };

        foreach (var i in await _repository.GetInvoicesAsync(q.FromDate, q.ToDate, withItems: true, ct))
        {
            if (i.Status == InvoiceStatus.Cancelled)
            {
                continue;
            }

            var registered = !string.IsNullOrWhiteSpace(i.CustomerGstin);
            var key = (i.IsInterState ? "Inter-state" : "Intra-state")
                + (registered ? " supplies to registered persons" : " supplies to unregistered persons");

            foreach (var item in i.Items.Where(x => x.SupplyType != SupplyType.Taxable))
            {
                var column = item.SupplyType switch
                {
                    SupplyType.NilRated => 0,
                    SupplyType.Exempt => 2,
                    _ => 4,
                };

                rows[key][column + (i.IsInterState ? 0 : 1)] += item.TaxableAmount;
            }
        }

        // Goods that came back are not turnover the shop had. Without this the table declares the
        // full sale of a nil-rated part that was returned the next day — and the figure it declares
        // is the one the portal cross-checks against 3.1(c).
        foreach (var n in await _repository.GetCreditNotesAsync(q.FromDate, q.ToDate, ct))
        {
            if (n.Status == CreditNoteStatus.Cancelled)
            {
                continue;
            }

            var registered = !string.IsNullOrWhiteSpace(n.CustomerGstin);
            var key = (n.IsInterState ? "Inter-state" : "Intra-state")
                + (registered ? " supplies to registered persons" : " supplies to unregistered persons");

            foreach (var item in n.Items.Where(x => x.SupplyType != SupplyType.Taxable))
            {
                var column = item.SupplyType switch
                {
                    SupplyType.NilRated => 0,
                    SupplyType.Exempt => 2,
                    _ => 4,
                };

                rows[key][column + (n.IsInterState ? 0 : 1)] -= item.TaxableAmount;
            }
        }

        foreach (var (description, values) in rows)
        {
            b.Row(
                ("description", description),
                ("nilInter", Money(values[0])), ("nilIntra", Money(values[1])),
                ("exemptInter", Money(values[2])), ("exemptIntra", Money(values[3])),
                ("nonGstInter", Money(values[4])), ("nonGstIntra", Money(values[5])));
        }
    }

    /// <summary>
    /// Table 13 — every number series the shop filed under, from where to where, and how many were
    /// cancelled. The one table the shop can produce better than anybody, because it is the only
    /// party that knows which numbers it actually issued.
    /// </summary>
    private async Task Gstr1Documents(RegisterBuilder b, RegisterQuery q, CancellationToken ct)
    {
        b.Text("nature", "Nature Of Document")
            .Text("from", "Sr. No. From")
            .Text("to", "Sr. No. To")
            .Number("total", "Total Number")
            .Number("cancelled", "Cancelled")
            .Number("net", "Net Issued");

        void Series(string nature, IReadOnlyList<(string Number, bool Cancelled)> documents)
        {
            if (documents.Count == 0)
            {
                return;
            }

            var ordered = documents.OrderBy(d => d.Number, StringComparer.Ordinal).ToList();
            var cancelled = ordered.Count(d => d.Cancelled);

            b.Row(
                ("nature", nature),
                ("from", ordered[0].Number),
                ("to", ordered[^1].Number),
                ("total", ordered.Count),
                ("cancelled", cancelled),
                // Cancelled documents keep their numbers — that is what makes the series unbroken —
                // so the net is what the return actually reports.
                ("net", ordered.Count - cancelled));
        }

        Series("Invoices for outward supply",
            (await _repository.GetInvoicesAsync(q.FromDate, q.ToDate, withItems: false, ct))
                .Select(i => (i.InvoiceNumber, i.Status == InvoiceStatus.Cancelled)).ToList());

        Series("Credit notes",
            (await _repository.GetCreditNotesAsync(q.FromDate, q.ToDate, ct))
                .Select(n => (n.CreditNoteNumber, n.Status == CreditNoteStatus.Cancelled)).ToList());

        Series("Debit notes",
            (await _repository.GetDebitNotesAsync(q.FromDate, q.ToDate, ct))
                .Select(n => (n.DebitNoteNumber, n.Status == DebitNoteStatus.Cancelled)).ToList());

        // Not part of the return, but the same question gets asked in an audit and the shop already
        // knows the answer.
        Series("Purchase entries (not filed, for your own record)",
            (await _repository.GetPurchasesAsync(q.FromDate, q.ToDate, ct))
                .Select(p => (p.PurchaseNumber, p.Status == PurchaseStatus.Cancelled)).ToList());

        Series("Receipts (not filed, for your own record)",
            (await _repository.GetPaymentsAsync(q.FromDate, q.ToDate, ct))
                .Where(p => p.ReceiptNumber is not null)
                .Select(p => (p.ReceiptNumber!, p.Status != PaymentStatus.Posted)).ToList());
    }

    private async Task Gstr3B(RegisterBuilder b, RegisterQuery q, CancellationToken ct)
    {
        // No column totals here. 3B's lines are already the summary, and adding a row that says
        // "outward plus input credit" would be a figure that means nothing to anybody.
        b.Text("section", "Section")
            .Text("line", "Line")
            .Money("taxable", "Taxable Value", total: false)
            .Money("cgst", "CGST", total: false)
            .Money("sgst", "SGST", total: false)
            .Money("igst", "IGST", total: false);

        // With items, because 3.1(a) and 3.1(c) are decided line by line now: one bill can carry a
        // taxable part and a nil-rated one, and they belong on different lines of the return.
        var invoices = await _repository.GetInvoicesAsync(q.FromDate, q.ToDate, withItems: true, ct);
        var creditNotes = await _repository.GetCreditNotesAsync(q.FromDate, q.ToDate, ct);
        var purchases = await _repository.GetPurchasesAsync(q.FromDate, q.ToDate, ct);
        var debitNotes = await _repository.GetDebitNotesAsync(q.FromDate, q.ToDate, ct);

        var sales = invoices.Where(i => i.Status != InvoiceStatus.Cancelled).ToList();
        var credits = creditNotes.Where(n => n.Status != CreditNoteStatus.Cancelled).ToList();
        var buys = purchases.Where(p => p.Status != PurchaseStatus.Cancelled).ToList();
        var debits = debitNotes.Where(n => n.Status != DebitNoteStatus.Cancelled).ToList();

        var taxableLines = sales.SelectMany(i => i.Items).Where(x => x.SupplyType == SupplyType.Taxable).ToList();
        var untaxedLines = sales.SelectMany(i => i.Items).Where(x => x.SupplyType != SupplyType.Taxable).ToList();

        // Split the same way as the sales, and for the same reason. A nil-rated part that comes
        // back reduces 3.1(c); putting it against 3.1(a) understates taxable turnover and leaves
        // exempt turnover standing at the full sale — two wrong figures that happen to net to the
        // same tax, which is why it survives a casual read.
        var creditedTaxable = credits.SelectMany(n => n.Items)
            .Where(x => x.SupplyType == SupplyType.Taxable).ToList();
        var creditedUntaxed = credits.SelectMany(n => n.Items)
            .Where(x => x.SupplyType != SupplyType.Taxable).ToList();

        b.Row(
            ("section", "3.1(a)"), ("line", "Outward taxable supplies"),
            ("taxable", Money(taxableLines.Sum(x => x.TaxableAmount))),
            ("cgst", Money(sales.Sum(i => i.CgstAmount))),
            ("sgst", Money(sales.Sum(i => i.SgstAmount))),
            ("igst", Money(sales.Sum(i => i.IgstAmount))));

        // Its own line, not folded into the one above. Nil rated, exempt and non-GST goods are
        // turnover the shop had and tax it never charged; counting them as taxable declares a
        // liability that does not exist.
        b.Row(
            ("section", "3.1(c)"), ("line", "Other outward supplies — nil rated, exempted"),
            ("taxable", Money(untaxedLines.Sum(x => x.TaxableAmount)
                              - creditedUntaxed.Sum(x => x.TaxableAmount))),
            ("cgst", 0m), ("sgst", 0m), ("igst", 0m));

        // Section 34 credit notes reduce the outward liability of the period they are issued in,
        // not of the period the original bill belonged to — so they are shown as their own line
        // rather than netted into the one above, where they would be invisible.
        b.Row(
            ("section", "3.1(a)"), ("line", "Less: credit notes issued"),
            ("taxable", Money(-creditedTaxable.Sum(x => x.TaxableAmount))),
            ("cgst", Money(-credits.Sum(n => n.CgstAmount))),
            ("sgst", Money(-credits.Sum(n => n.SgstAmount))),
            ("igst", Money(-credits.Sum(n => n.IgstAmount))));

        b.Row(
            ("section", "4(A)(5)"), ("line", "Input tax credit — all other ITC"),
            ("taxable", Money(buys.Sum(p => p.TaxableAmount))),
            ("cgst", Money(buys.Sum(p => p.CgstAmount))),
            ("sgst", Money(buys.Sum(p => p.SgstAmount))),
            ("igst", Money(buys.Sum(p => p.IgstAmount))));

        b.Row(
            ("section", "4(B)(2)"), ("line", "Less: ITC reversed — debit notes raised"),
            ("taxable", Money(-debits.Sum(n => n.TaxableAmount))),
            ("cgst", Money(-debits.Sum(n => n.CgstAmount))),
            ("sgst", Money(-debits.Sum(n => n.SgstAmount))),
            ("igst", Money(-debits.Sum(n => n.IgstAmount))));

        // Output less credit, head by head — and nothing more. Section 49A makes IGST credit be used
        // up before CGST or SGST credit, and it may be applied against either of them; which way it
        // goes is a decision, not arithmetic. Calling this "payable" would have told a shop holding
        // ₹26,000 of IGST credit that it owed CGST and SGST in cash.
        b.Row(
            ("section", "Net"), ("line", "Output less credit, per head — before cross-head set-off"),
            ("taxable", null),
            ("cgst", Money(sales.Sum(i => i.CgstAmount) - credits.Sum(n => n.CgstAmount)
                - buys.Sum(p => p.CgstAmount) + debits.Sum(n => n.CgstAmount))),
            ("sgst", Money(sales.Sum(i => i.SgstAmount) - credits.Sum(n => n.SgstAmount)
                - buys.Sum(p => p.SgstAmount) + debits.Sum(n => n.SgstAmount))),
            ("igst", Money(sales.Sum(i => i.IgstAmount) - credits.Sum(n => n.IgstAmount)
                - buys.Sum(p => p.IgstAmount) + debits.Sum(n => n.IgstAmount))));
    }

    // --------------------------------------------------------------- Shared

    /// <summary>
    /// A counter sale to somebody who gave no address is a supply in the shop's own state — that is
    /// where the goods changed hands. Guessing anything else would put the tax in the wrong state.
    /// </summary>
    private static string PlaceOfSupply(string? partyStateCode, string shopStateCode) =>
        string.IsNullOrWhiteSpace(partyStateCode) ? shopStateCode : partyStateCode;

    /// <summary>
    /// The rate a document was issued at, derived from what it charged. A note carrying lines at two
    /// rates has no single rate, and this returns the blended one — visible as an odd number, which
    /// is the point: it tells whoever files that the note has to be split by hand.
    /// </summary>
    private static decimal EffectiveRate(decimal taxable, decimal tax) =>
        taxable == 0 ? 0m : Math.Round(100m * tax / taxable, 2, MidpointRounding.AwayFromZero);

    private static decimal Money(decimal value) =>
        Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
