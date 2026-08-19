using Application.DTOs.Invoices;
using Domain.Entities;

namespace Application.Mapping;

public static class InvoiceMapper
{
    public static InvoiceDto ToDto(this Invoice invoice) => new(
        invoice.Id,
        invoice.InvoiceNumber,
        invoice.FinancialYear,
        invoice.InvoiceDate,
        invoice.DueDate,
        invoice.CustomerId,
        invoice.CustomerName,
        invoice.CustomerPhone,
        invoice.CustomerGstin,
        invoice.CustomerStateCode,
        invoice.IsInterState,
        invoice.SubTotal,
        invoice.DiscountAmount,
        invoice.TaxableAmount,
        invoice.CgstAmount,
        invoice.SgstAmount,
        invoice.IgstAmount,
        invoice.TotalTax,
        invoice.RoundOff,
        invoice.GrandTotal,
        invoice.AmountPaid,
        invoice.BalanceDue,
        invoice.PaymentMode.ToString(),
        invoice.Notes,
        invoice.Status.ToString(),
        invoice.Items.Select(i => i.ToDto()).ToList(),
        invoice.CreatedAt,
        invoice.UpdatedAt);

    public static InvoiceListItemDto ToListItemDto(this Invoice invoice) => new(
        invoice.Id,
        invoice.InvoiceNumber,
        invoice.InvoiceDate,
        invoice.CustomerId,
        invoice.CustomerName,
        invoice.CustomerPhone,
        invoice.ItemCount,
        invoice.TaxableAmount,
        invoice.TotalTax,
        invoice.GrandTotal,
        invoice.AmountPaid,
        invoice.BalanceDue,
        invoice.PaymentMode.ToString(),
        invoice.Status.ToString());

    public static InvoiceItemDto ToDto(this InvoiceItem item) => new(
        item.Id,
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
}
