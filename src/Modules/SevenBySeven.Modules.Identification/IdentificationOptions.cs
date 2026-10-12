namespace SevenBySeven.Modules.Identification;

public sealed class IdentificationOptions
{
    public const string SectionName = "Identification";

    /// <summary>
    /// Optional, set with user-secrets. When absent the SDK finds credentials itself:
    /// <c>ANTHROPIC_API_KEY</c>, or the sign-in that <c>ant auth login</c> stores.
    /// </summary>
    public string? AnthropicApiKey { get; set; }

    public string Model { get; set; } = "claude-opus-5";

    public bool HasApiKey => !string.IsNullOrWhiteSpace(AnthropicApiKey);
}
