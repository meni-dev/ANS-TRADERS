using Application.Common.Exceptions;
using FluentValidation;

namespace Application.Common;

/// <summary>
/// Runs a FluentValidation validator and reshapes its failures into the property-keyed dictionary
/// the API returns. Shared so every service reports validation errors in one format.
/// </summary>
public static class ValidationHelper
{
    public static async Task ValidateAsync<T>(
        IValidator<T> validator, T request, CancellationToken cancellationToken)
    {
        var result = await validator.ValidateAsync(request, cancellationToken);

        if (result.IsValid)
        {
            return;
        }

        var errors = result.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

        throw new ValidationAppException(errors);
    }
}
