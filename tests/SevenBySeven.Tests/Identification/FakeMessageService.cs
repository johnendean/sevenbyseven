using System.Text.Json;
using Anthropic.Core;
using Anthropic.Models.Messages;
using Anthropic.Services;

namespace SevenBySeven.Tests.Identification;

/// <summary>
/// Stands in for the Anthropic SDK's message service. The sleeve reader depends on this
/// interface rather than on <c>AnthropicClient</c>, so a test can assert on what we do
/// with the answer without pinning itself to the request shape the SDK puts on the wire
/// — that shape is theirs to change, and a test that guards it would break on an SDK
/// upgrade without anything of ours being wrong.
/// </summary>
internal sealed class FakeMessageService(Func<MessageCreateParams, Message> respond) : IMessageService
{
    public MessageCreateParams? LastRequest { get; private set; }

    public int Calls { get; private set; }

    /// <summary>Answers with a single text block, which is what the JSON format produces.</summary>
    public static FakeMessageService Saying(string text) =>
        new(_ => MessageWith(text));

    public static FakeMessageService Refusing(string category) =>
        new(_ => Refusal(category));

    public static FakeMessageService Failing(Exception failure) =>
        new(_ => throw failure);

    public Task<Message> Create(MessageCreateParams parameters, CancellationToken cancellationToken = default)
    {
        LastRequest = parameters;
        Calls++;
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(respond(parameters));
    }

    // Nothing we write calls these. Throwing rather than returning an empty answer means
    // a future caller finds out here instead of in production.
    public IMessageServiceWithRawResponse WithRawResponse => throw new NotSupportedException();

    public Anthropic.Services.Messages.IBatchService Batches => throw new NotSupportedException();

    public IMessageService WithOptions(Func<ClientOptions, ClientOptions> modifier) =>
        throw new NotSupportedException();

    public IAsyncEnumerable<RawMessageStreamEvent> CreateStreaming(
        MessageCreateParams parameters,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<MessageTokensCount> CountTokens(
        MessageCountTokensParams parameters,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    // Built from JSON rather than an object initialiser: Message carries a long tail of
    // required members we do not care about, and this is the shape the SDK itself parses.
    private static Message MessageWith(string text) => Parse($$"""
        {
          "id": "msg_test",
          "type": "message",
          "role": "assistant",
          "model": "claude-opus-5",
          "content": [{"type": "text", "text": {{Json(text)}}}],
          "stop_reason": "end_turn",
          "stop_sequence": null,
          "usage": {"input_tokens": 1, "output_tokens": 1}
        }
        """);

    private static Message Refusal(string category) => Parse($$"""
        {
          "id": "msg_test",
          "type": "message",
          "role": "assistant",
          "model": "claude-opus-5",
          "content": [],
          "stop_reason": "refusal",
          "stop_details": {"type": "refusal", "category": {{Json(category)}}},
          "stop_sequence": null,
          "usage": {"input_tokens": 1, "output_tokens": 0}
        }
        """);

    private static string Json(string value) => JsonSerializer.Serialize(value);

    private static Message Parse(string json) =>
        JsonSerializer.Deserialize<Message>(json)
        ?? throw new InvalidOperationException("The stubbed message did not deserialise.");
}
