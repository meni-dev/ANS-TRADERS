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

        new("outstanding", "Party Outstanding", "Who owes the shop, and who the shop owes, right now", "Balances", IsAsAt: true),
        new("stock-valuation", "Stock Valuation", "What is on the shelf right now and what it cost", "Balances", IsAsAt: true),
        new("stock-movement", "Stock Movement", "Every in and out, with its reference", "Balances"),

        new("gstr1-b2b", "GSTR-1 · B2B", "Bills to GSTIN holders, invoice by invoice", "GST"),
        new("gstr1-b2cs", "GSTR-1 · B2C Small", "Counter sales, summarised by state and rate", "GST"),
        new("gstr1-cdnr", "GSTR-1 · Credit / Debit Notes", "Notes against registered parties", "GST"),
        new("gstr1-hsn", "GSTR-1 · HSN Summary", "Table 12 — quantity and value by HSN", "GST"),
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
            case "outstanding": await Outstanding(builder, cancellationToken); break;
            case "stock-valuation":
                // The only register that prints what the shop paid. Someone who may read the sales
                // register still has no business reading the shelf's cost.
                _currentUser.Require(Permission.CostView, "see what the stock cost");
                await StockValuation(builder, cancellationToken);
                break;
            case "stock-movement": await StockMovements(builder, query, cancellationToken); break;
            case "gstr1-b2b": await Gstr1B2B(builder, query, cancellationToken); break;
            case "gstr1-b2cs": await Gstr1B2Cs(builder, query, cancellationToken); break;
            case "gstr1-cdnr": await Gstr1Cdnr(builder, query, cancellationToken); break;
            case "gstr1-hsn": await Gstr1Hsn(builder, query, cancellationToken); break;
            case "gstr3b": await Gstr3B(builder, query, cancellationToken); break;
        }

        // A position register answers as at today whatever range came in, so it reports today's date
        // back rather than echoing a range it ignored. Printing 01-04-2026 above a stock figure that
        // is current would be the register lying about what it counted.
        var today = _clock.Today;

        return builder.Build(
            summary.Key,
            summary.Title,
            summary.Caption,
            summary.IsAsAt ? today : query.FromDate,
            summary.IsAsAt ? today : query.ToDate,
            summary.IsAsAt);
    }

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

    private async Task Outstanding(RegisterBuilder b, CancellationToken ct)
    {
        b.Text("type", "Party Type")
            .Text("name", "Name")
            .Text("phone", "Phone")
            .Text("gstin", "GSTIN")
            .Money("receivable", "Receivable")
            .Money("payable", "Payable");

        var (customers, suppliers) = await _repository.GetOpenPartiesAsync(ct);

        foreach (var c in customers)
        {
            // A customer with a negative balance is holding an advance, which is money the shop
            // owes back — it belongs in the payable column, not as a negative receivable.
            b.Row(
                ("type", "Customer"),
                ("name", c.Name),
                ("phone", c.Phone),
                ("gstin", c.Gstin),
                ("receivable", c.OutstandingBalance > 0 ? c.OutstandingBalance : 0m),
                ("payable", c.OutstandingBalance < 0 ? -c.OutstandingBalance : 0m));
        }

        foreach (var s in suppliers)
        {
            b.Row(
                ("type", "Supplier"),
                ("name", s.Name),
                ("phone", s.Phone),
                ("gstin", s.Gstin),
                ("receivable", s.OutstandingBalance < 0 ? -s.OutstandingBalance : 0m),
                ("payable", s.OutstandingBalance > 0 ? s.OutstandingBalance : 0m));
        }
    }

    private async Task StockValuation(RegisterBuilder b, CancellationToken ct)
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

        foreach (var p in await _repository.GetProductsForValuationAsync(ct))
        {
            b.Row(
                ("partNumber", p.PartNumber),
                ("itemName", p.ItemName),
                ("hsn", p.Hsn),
                ("uqc", p.Uqc),
                ("stock", p.StockOnHand),
                ("purchaseRate", p.PurchaseRate),
                ("value", Money(p.StockOnHand * p.PurchaseRate)),
                ("sellingRate", p.SellingRate),
                ("saleValue", Money(p.StockOnHand * p.SellingRate)),
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

        foreach (var m in await _repository.GetMovementsAsync(q.FromDate, q.ToDate, ct))
        {
            b.Row(
                ("when", m.MovedAt),
                ("partNumber", m.PartNumber),
                ("itemName", m.ItemName),
                ("type", m.MovementType.ToString()),
                ("reason", m.AdjustmentReason?.ToString()),
                ("in", m.Quantity > 0 ? m.Quantity : 0m),
                ("out", m.Quantity < 0 ? -m.Quantity : 0m),
                ("balance", m.BalanceAfter),
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
            // repeats an invoice number against the same rate.
            foreach (var group in i.Items.GroupBy(x => x.GstRate).OrderBy(g => g.Key))
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
            if (i.Status == InvoiceStatus.Cancelled || !string.IsNullOrWhiteSpace(i.CustomerGstin))
            {
                continue;
            }

            var place = PlaceOfSupply(i.CustomerStateCode, shop.StateCode);

            foreach (var item in i.Items)
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

        var invoices = await _repository.GetInvoicesAsync(q.FromDate, q.ToDate, withItems: false, ct);
        var creditNotes = await _repository.GetCreditNotesAsync(q.FromDate, q.ToDate, ct);
        var purchases = await _repository.GetPurchasesAsync(q.FromDate, q.ToDate, ct);
        var debitNotes = await _repository.GetDebitNotesAsync(q.FromDate, q.ToDate, ct);

        var sales = invoices.Where(i => i.Status != InvoiceStatus.Cancelled).ToList();
        var credits = creditNotes.Where(n => n.Status != CreditNoteStatus.Cancelled).ToList();
        var buys = purchases.Where(p => p.Status != PurchaseStatus.Cancelled).ToList();
        var debits = debitNotes.Where(n => n.Status != DebitNoteStatus.Cancelled).ToList();

        b.Row(
            ("section", "3.1(a)"), ("line", "Outward taxable supplies"),
            ("taxable", Money(sales.Sum(i => i.TaxableAmount))),
            ("cgst", Money(sales.Sum(i => i.CgstAmount))),
            ("sgst", Money(sales.Sum(i => i.SgstAmount))),
            ("igst", Money(sales.Sum(i => i.IgstAmount))));

        // Section 34 credit notes reduce the outward liability of the period they are issued in,
        // not of the period the original bill belonged to — so they are shown as their own line
        // rather than netted into the one above, where they would be invisible.
        b.Row(
            ("section", "3.1(a)"), ("line", "Less: credit notes issued"),
            ("taxable", Money(-credits.Sum(n => n.TaxableAmount))),
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
