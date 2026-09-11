namespace SevenBySeven.Modules.Identification;

public sealed class IdentificationOptions
{
    public const string SectionName = "Identification";

    /// <summary>
    /// Set with user-secrets. When absent, vision extraction is skipped and only the
    /// barcode path can identify a record.
    /// </summary>
    public string? AnthropicApiKey { get; set; }

    public string Model { get; set; } = "claude-opus-5";

    public bool IsConfigured => !string.IsNullOrWhiteSpace(AnthropicApiKey);
}
