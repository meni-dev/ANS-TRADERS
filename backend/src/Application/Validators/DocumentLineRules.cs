using Application.Interfaces;
using FluentValidation;

namespace Application.Validators;

/// <summary>
/// Line rules shared by the purchase and invoice validators. A billed line is the same shape on
/// both sides of the counter, and a quantity that is rejected on a sale must be rejected on a
/// purchase for the same reason.
/// </summary>
public static class DocumentLineRules
{
    public static IRuleBuilderOptions<T, decimal> LineQuantity<T>(this IRuleBuilder<T, decimal> rule) =>
        rule.GreaterThan(0).WithMessage("Quantity must be greater than zero");

    public static IRuleBuilderOptions<T, decimal> LineRate<T>(this IRuleBuilder<T, decimal> rule) =>
        rule.GreaterThanOrEqualTo(0).WithMessage("Rate cannot be negative");

    public static IRuleBuilderOptions<T, decimal> LineDiscountPercent<T>(this IRuleBuilder<T, decimal> rule) =>
        rule.InclusiveBetween(0, 100).WithMessage("Discount must be between 0 and 100%");

    public static IRuleBuilderOptions<T, Guid> LineProductId<T>(this IRuleBuilder<T, Guid> rule) =>
        rule.NotEmpty().WithMessage("Pick a product for every line");

    /// <summary>
    /// A document dated in the future would land in a GST return period that has not opened yet.
    /// Back-dating is left alone: bills genuinely arrive days after they are raised.
    /// <para>
    /// Today is the shop's today, not the server's. This used to allow <c>UtcNow + 1 day</c>, which
    /// on a UTC server made tomorrow a legal bill date — while an expense or a day close, which ask
    /// <see cref="IShopClock"/>, refused it. One rule now, from one clock.
    /// </para>
    /// </summary>
    public static IRuleBuilderOptions<T, DateOnly> DocumentDate<T>(
        this IRuleBuilder<T, DateOnly> rule, IShopClock clock) =>
        rule.NotEmpty().WithMessage("Pick a date")
            .LessThanOrEqualTo(_ => clock.Today)
            .WithMessage(_ => $"That date has not arrived yet — today is {clock.Today:dd MMM yyyy}");
}
