using System.IO.Pipes;
using System.Text;
using System.Text.Json;

namespace Code2Viz.McpBridge;

/// <summary>
/// Named pipe server used by the WPF app to receive commands from the MCP server.
/// Listens for connections in a loop and dispatches requests to a handler.
/// </summary>
public class IpcServer : IDisposable
{
    private CancellationTokenSource? _cts;
    private Task? _listenTask;
    private readonly Func<IpcRequest, Task<IpcResponse>> _handler;

    public IpcServer(Func<IpcRequest, Task<IpcResponse>> handler)
    {
        _handler = handler;
    }

    public void Start()
    {
        _cts = new CancellationTokenSource();
        _listenTask = ListenLoop(_cts.Token);
    }

    public void Stop()
    {
        _cts?.Cancel();
        try { _listenTask?.Wait(TimeSpan.FromSeconds(2)); } catch { }
        _cts?.Dispose();
        _cts = null;
    }

    private async Task ListenLoop(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                using var pipe = new NamedPipeServerStream(
                    IpcClient.PipeName,
                    PipeDirection.InOut,
                    NamedPipeServerStream.MaxAllowedServerInstances,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous);

                await pipe.WaitForConnectionAsync(ct);

                try
                {
                    // Read request line
                    var line = await ReadLineAsync(pipe, ct);
                    if (line != null)
                    {
                        var request = JsonSerializer.Deserialize<IpcRequest>(line);
                        if (request != null)
                        {
                            var response = await _handler(request);
                            var json = JsonSerializer.Serialize(response);
                            var bytes = Encoding.UTF8.GetBytes(json + "\n");
                            await pipe.WriteAsync(bytes, ct);
                            await pipe.FlushAsync(ct);
                        }
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    // Log but continue listening
                    System.Diagnostics.Debug.WriteLine($"[McpBridge] Error handling request: {ex.Message}");
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[McpBridge] Pipe error: {ex.Message}");
                // Brief delay before retrying
                try { await Task.Delay(500, ct); } catch { break; }
            }
        }
    }

    private static async Task<string?> ReadLineAsync(Stream stream, CancellationToken ct)
    {
        var buffer = new byte[1];
        var sb = new StringBuilder();

        while (!ct.IsCancellationRequested)
        {
            var read = await stream.ReadAsync(buffer, 0, 1, ct);
            if (read == 0) break;
            if (buffer[0] == '\n') break;
            sb.Append((char)buffer[0]);
        }

        return sb.Length > 0 ? sb.ToString() : null;
    }

    public void Dispose()
    {
        Stop();
    }
}
