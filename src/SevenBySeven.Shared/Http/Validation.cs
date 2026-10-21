using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace SevenBySeven.Shared.Http;

/// <summary>
/// Checks a request body against its validator before the handler runs, so nothing is
/// fetched or saved for a request that will be refused. Past a contract's limits the
/// database would refuse it as a 500, which says a bug where the fault is the client's
/// (docs/adr/0008).
/// </summary>
public static class Validation
{
    /// <summary>
    /// Refuses the endpoint's <typeparamref name="T"/> argument with a 400 naming each field
    /// that fails its <see cref="IValidator{T}"/>, which the module must have registered.
    /// </summary>
    public static RouteHandlerBuilder Validates<T>(this RouteHandlerBuilder endpoint)
    {
        ArgumentNullException.ThrowIfNull(endpoint);

        return endpoint
            .AddEndpointFilter(async (context, next) =>
            {
                // A body that did not bind never reaches here: binding has already answered 400.
                if (context.Arguments.OfType<T>().FirstOrDefault() is not { } argument)
                {
                    return await next(context);
                }

                var validator = context.HttpContext.RequestServices.GetRequiredService<IValidator<T>>();
                var result = await validator.ValidateAsync(argument, context.HttpContext.RequestAborted);

                if (result.IsValid)
                {
                    return await next(context);
                }

                return TypedResults.ValidationProblem(
                    result.Errors
                        .GroupBy(error => Field(error.PropertyName))
                        .ToDictionary(errors => errors.Key, errors => errors.Select(error => error.ErrorMessage).ToArray()));
            })
            .ProducesValidationProblem();
    }

    /// <summary>
    /// Named as the JSON names it, which is what the client sent: "Details.Location" is
    /// "details.location".
    /// </summary>
    private static string Field(string propertyName) =>
        string.Join('.', propertyName.Split('.').Select(part =>
            part.Length == 0 ? part : char.ToLowerInvariant(part[0]) + part[1..]));
}
