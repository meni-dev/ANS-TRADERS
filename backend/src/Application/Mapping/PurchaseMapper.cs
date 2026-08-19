using Application.DTOs.Purchases;
using Domain.Entities;

namespace Application.Mapping;

public static class PurchaseMapper
{
    public static PurchaseDto ToDto(this Purchase purchase) => new(
        purchase.Id,
        purchase.PurchaseNumber,
        purchase.FinancialYear,
        purchase.SupplierInvoiceNumber,
        purchase.InvoiceDate,
        purchase.SupplierId,
        purchase.SupplierName,
        purchase.SupplierGstin,
        purchase.SupplierStateCode,
        purchase.IsInterState,
        purchase.SubTotal,
        purchase.DiscountAmount,
        purchase.TaxableAmount,
        purchase.CgstAmount,
        purchase.SgstAmount,
        purchase.IgstAmount,
        purchase.TotalTax,
        purchase.RoundOff,
        purchase.GrandTotal,
        purchase.AmountPaid,
        purchase.BalanceDue,
        purchase.PaymentMode.ToString(),
        purchase.Notes,
        purchase.Status.ToString(),
        purchase.Items.Select(i => i.ToDto()).ToList(),
        purchase.CreatedAt,
        purchase.UpdatedAt);

    public static PurchaseListItemDto ToListItemDto(this Purchase purchase) => new(
        purchase.Id,
        purchase.PurchaseNumber,
        purchase.SupplierInvoiceNumber,
        purchase.InvoiceDate,
        purchase.SupplierId,
        purchase.SupplierName,
        purchase.ItemCount,
        purchase.TaxableAmount,
        purchase.TotalTax,
        purchase.GrandTotal,
        purchase.AmountPaid,
        purchase.BalanceDue,
        purchase.PaymentMode.ToString(),
        purchase.Status.ToString());

    public static PurchaseItemDto ToDto(this PurchaseItem item) => new(
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
