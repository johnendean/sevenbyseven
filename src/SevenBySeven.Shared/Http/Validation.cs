using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace SevenBySeven.Shared.Http;

/// <summary>
/// Checks a request body against the limits its contract declares, before anything is
/// fetched or saved. Past those limits the database would refuse it as a 500, which says a
/// bug where the fault is the client's (docs/adr/0008).
/// </summary>
public static class Validation
{
    /// <summary>A 400 naming each field that is out of bounds, or null when every part is fine.</summary>
    /// <param name="parts">
    /// Each object to check and the name it goes by in the request. Validation does not
    /// descend into nested objects by itself, so a body that carries one names it here.
    /// </param>
    public static ValidationProblem? Problem(params (string Name, object? Value)[] parts)
    {
        var errors = new Dictionary<string, List<string>>();

        foreach (var (name, value) in parts)
        {
            if (value is null)
            {
                continue;
            }

            var results = new List<ValidationResult>();
            Validator.TryValidateObject(value, new ValidationContext(value), results, validateAllProperties: true);

            foreach (var result in results)
            {
                foreach (var member in result.MemberNames.DefaultIfEmpty(string.Empty))
                {
                    var key = Field(name, member);

                    if (!errors.TryGetValue(key, out var messages))
                    {
                        errors[key] = messages = [];
                    }

                    messages.Add(result.ErrorMessage ?? "This value is not allowed.");
                }
            }
        }

        return errors.Count == 0
            ? null
            : TypedResults.ValidationProblem(errors.ToDictionary(error => error.Key, error => error.Value.ToArray()));
    }

    private static string Field(string name, string member) =>
        (name, member) switch
        {
            ("", _) => Camel(member),
            (_, "") => name,
            _ => $"{name}.{Camel(member)}",
        };

    /// <summary>Named as the JSON names them, which is what the client sent.</summary>
    private static string Camel(string member) =>
        member.Length == 0 ? member : char.ToLowerInvariant(member[0]) + member[1..];
}
