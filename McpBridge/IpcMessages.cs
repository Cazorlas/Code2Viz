using System.Text.Json.Serialization;

namespace Code2Viz.McpBridge;

public record IpcRequest
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = Guid.NewGuid().ToString();

    [JsonPropertyName("command")]
    public string Command { get; init; } = "";

    [JsonPropertyName("payload")]
    public string? Payload { get; init; }
}

public record IpcResponse
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = "";

    [JsonPropertyName("success")]
    public bool Success { get; init; }

    [JsonPropertyName("result")]
    public string? Result { get; init; }

    [JsonPropertyName("error")]
    public string? Error { get; init; }

    public static IpcResponse Ok(string id, string? result = null) =>
        new() { Id = id, Success = true, Result = result };

    public static IpcResponse Fail(string id, string error) =>
        new() { Id = id, Success = false, Error = error };
}
