using Application.DTOs.Returns;
using Domain.Entities;

namespace Application.Mapping;

public static class ReturnMapper
{
    public static CreditNoteDto ToDto(this CreditNote note) => new(
        note.Id,
        note.CreditNoteNumber,
        note.FinancialYear,
        note.NoteDate,
        note.InvoiceId,
        note.InvoiceNumber,
        note.InvoiceDate,
        note.CustomerId,
        note.CustomerName,
        note.CustomerPhone,
        note.CustomerGstin,
        note.CustomerStateCode,
        note.IsInterState,
        note.ItemCount,
        note.Reason,
        note.SubTotal,
        note.DiscountAmount,
        note.TaxableAmount,
        note.CgstAmount,
        note.SgstAmount,
        note.IgstAmount,
        note.TotalTax,
        note.RoundOff,
        note.GrandTotal,
        note.AppliedToInvoiceAmount,
        note.RefundedAmount,
        // Only what never went against the bill can still be handed back — the rest was set off
        // against what the customer owed and was never cash the shop held.
        Round(note.GrandTotal - note.AppliedToInvoiceAmount - note.RefundedAmount),
        note.Status.ToString(),
        note.Items.Select(i => i.ToDto()).ToList(),
        note.CreatedAt);

    public static CreditNoteListItemDto ToListItemDto(this CreditNote note) => new(
        note.Id,
        note.CreditNoteNumber,
        note.NoteDate,
        note.InvoiceNumber,
        note.CustomerName,
        note.ItemCount,
        note.GrandTotal,
        note.RefundedAmount,
        note.Status.ToString());

    public static ReturnNoteItemDto ToDto(this CreditNoteItem item) => new(
        item.Id,
        item.InvoiceItemId,
        item.ProductId,
        item.PartNumber,
        item.ItemName,
        item.Hsn,
        item.Uqc,
        item.Quantity,
        item.Rate,
        item.DiscountPercent,
        item.DiscountAmount,
        item.GrossAmount,
        item.TaxableAmount,
        item.GstRate,
        item.CgstAmount,
        item.SgstAmount,
        item.IgstAmount,
        item.LineTotal);

    public static DebitNoteDto ToDto(this DebitNote note) => new(
        note.Id,
        note.DebitNoteNumber,
        note.FinancialYear,
        note.NoteDate,
        note.PurchaseId,
        note.PurchaseNumber,
        note.PurchaseDate,
        note.SupplierId,
        note.SupplierName,
        null,
        note.SupplierGstin,
        note.SupplierStateCode,
        note.IsInterState,
        note.ItemCount,
        note.Reason,
        note.SubTotal,
        note.DiscountAmount,
        note.TaxableAmount,
        note.CgstAmount,
        note.SgstAmount,
        note.IgstAmount,
        note.TotalTax,
        note.RoundOff,
        note.GrandTotal,
        note.AppliedToPurchaseAmount,
        note.RefundedAmount,
        Round(note.GrandTotal - note.AppliedToPurchaseAmount - note.RefundedAmount),
        note.Status.ToString(),
        note.Items.Select(i => i.ToDto()).ToList(),
        note.CreatedAt);

    public static DebitNoteListItemDto ToListItemDto(this DebitNote note) => new(
        note.Id,
        note.DebitNoteNumber,
        note.NoteDate,
        note.PurchaseNumber,
        note.SupplierName,
        note.ItemCount,
        note.GrandTotal,
        note.RefundedAmount,
        note.Status.ToString());

    public static ReturnNoteItemDto ToDto(this DebitNoteItem item) => new(
        item.Id,
        item.PurchaseItemId,
        item.ProductId,
        item.PartNumber,
        item.ItemName,
        item.Hsn,
        item.Uqc,
        item.Quantity,
        item.Rate,
        item.DiscountPercent,
        item.DiscountAmount,
        item.GrossAmount,
        item.TaxableAmount,
        item.GstRate,
        item.CgstAmount,
        item.SgstAmount,
        item.IgstAmount,
        item.LineTotal);

    private static decimal Round(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
