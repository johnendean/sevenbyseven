using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace SevenBySeven.Shared.Http;

/// <summary>
/// How a module's answers cross HTTP (docs/adr/0009). Something that is not there comes back
/// from a module as null or false (docs/adr/0008), and over the wire that is a 404.
/// </summary>
public static class Outcomes
{
    public static Results<Ok<T>, NotFound> OkOrNotFound<T>(T? value)
        where T : class =>
        value is null ? TypedResults.NotFound() : TypedResults.Ok(value);

    public static Results<NoContent, NotFound> NoContentOrNotFound(bool found) =>
        found ? TypedResults.NoContent() : TypedResults.NotFound();
}
