using System.IO.Pipes;
using System.Text;
using System.Text.Json;

namespace Code2Viz.McpBridge;

/// <summary>
/// Named pipe client used by the MCP server to send commands to the WPF app.
/// </summary>
public class IpcClient : IDisposable
{
    public const string PipeName = "Code2VizMcpBridge";
    private const int ConnectTimeoutMs = 5000;
    private const int ReadTimeoutMs = 30000;

    public async Task<IpcResponse> SendAsync(IpcRequest request, CancellationToken ct = default)
    {
        using var pipe = new NamedPipeClientStream(".", PipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
        await pipe.ConnectAsync(ConnectTimeoutMs, ct);

        // Write request as a single JSON line
        var json = JsonSerializer.Serialize(request);
        var bytes = Encoding.UTF8.GetBytes(json + "\n");
        await pipe.WriteAsync(bytes, ct);
        await pipe.FlushAsync(ct);

        // Read response line
        var response = await ReadLineAsync(pipe, ct);
        if (response == null)
            return IpcResponse.Fail(request.Id, "No response from Code2Viz");

        return JsonSerializer.Deserialize<IpcResponse>(response)
               ?? IpcResponse.Fail(request.Id, "Invalid response from Code2Viz");
    }

    private static async Task<string?> ReadLineAsync(Stream stream, CancellationToken ct)
    {
        var buffer = new byte[1];
        var sb = new StringBuilder();
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(ReadTimeoutMs);

        while (true)
        {
            var read = await stream.ReadAsync(buffer, 0, 1, cts.Token);
            if (read == 0) break;
            if (buffer[0] == '\n') break;
            sb.Append((char)buffer[0]);
        }

        return sb.Length > 0 ? sb.ToString() : null;
    }

    public void Dispose() { }
}
