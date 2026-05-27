using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using C2VGeometry;

namespace Animator.Ipc;

/// <summary>
/// Parent-side handle to an out-of-process sketch host (<c>SketchHost.exe</c>). Spawns the child,
/// streams control to it and frames/console/events back, and runs a watchdog that kills a child
/// that stops producing frames (an infinite loop in user Draw) so the parent UI never freezes.
///
/// Events fire on background threads (reader/watchdog) — a WPF host must marshal to the UI thread.
/// </summary>
public sealed class SketchHostClient : IDisposable
{
    private Process? _proc;
    private MessageChannel? _channel;
    private Thread? _reader;
    private Timer? _watchdog;
    private volatile bool _disposed;
    private volatile bool _sketchRunning;
    private long _lastFrameTicks;

    /// <summary>Max time a running sketch may go without producing a frame before it's deemed hung.</summary>
    public TimeSpan HangTimeout { get; set; } = TimeSpan.FromSeconds(3);

    public event Action<IReadOnlyList<Shape>, int>? FrameReceived;   // shapes, frameCount
    public event Action<string>? BackgroundChanged;                  // color
    public event Action<double, double>? ZoomRequested;              // width, height
    public event Action<int, string, string>? ConsoleLine;          // level, source, message
    public event Action<bool, string>? CompileCompleted;            // success, error
    public event Action? SketchStopped;                              // sketch ended/errored, child idle
    public event Action? Ready;                                      // child booted
    public event Action<string>? Hung;                               // watchdog tripped; child was killed
    public event Action<int>? Exited;                                // child process exited (exit code)

    public bool IsChildAlive => _proc is { HasExited: false };

    public void Start(string sketchHostExePath)
    {
        if (!File.Exists(sketchHostExePath))
            throw new FileNotFoundException("SketchHost executable not found", sketchHostExePath);

        var psi = new ProcessStartInfo(sketchHostExePath)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        _proc = Process.Start(psi) ?? throw new InvalidOperationException("Failed to start SketchHost");

        // Child diagnostics go to stderr (stdout is the binary protocol). Surface them for debugging.
        _proc.ErrorDataReceived += (_, e) => { if (e.Data != null) Debug.WriteLine($"[SketchHost] {e.Data}"); };
        _proc.BeginErrorReadLine();

        _channel = new MessageChannel(_proc.StandardOutput.BaseStream, _proc.StandardInput.BaseStream);

        _reader = new Thread(ReadLoop) { IsBackground = true, Name = "SketchHostReader" };
        _reader.Start();

        _watchdog = new Timer(CheckHang, null, 500, 500);
    }

    public void Run(string name, string source)
    {
        Touch();
        _sketchRunning = true;
        _channel?.Write(MessageType.RunSource, w => { w.Write(name); w.Write(source); });
    }

    public void SendInput(double mouseX, double mouseY, bool mousePressed, bool keyPressed, string lastKey)
        => _channel?.Write(MessageType.Input, w =>
        {
            w.Write(mouseX); w.Write(mouseY); w.Write(mousePressed); w.Write(keyPressed); w.Write(lastKey ?? "");
        });

    public void StopSketch()
    {
        _sketchRunning = false;
        try { _channel?.Write(MessageType.Stop); } catch { /* child may be gone */ }
    }

    private void ReadLoop()
    {
        try
        {
            Message? m;
            while ((m = _channel!.Read()) is { } msg)
                Dispatch(msg);
        }
        catch { /* stream torn down */ }

        // Stream closed -> child exited (cleanly, crashed, or killed).
        _sketchRunning = false;
        int code = -1;
        try { if (_proc != null) { _proc.WaitForExit(2000); code = _proc.ExitCode; } } catch { /* ignore */ }
        Exited?.Invoke(code);
    }

    private void Dispatch(Message msg)
    {
        var r = msg.Body;
        switch (msg.Type)
        {
            case MessageType.Ready:
                Ready?.Invoke();
                break;
            case MessageType.CompileResult:
                CompileCompleted?.Invoke(r.ReadBoolean(), r.ReadString());
                break;
            case MessageType.Frame:
            {
                int frameCount = r.ReadInt32();
                int n = r.ReadInt32();
                var shapes = new List<Shape>(n);
                for (int i = 0; i < n; i++) shapes.Add(ShapeCodec.Decode(r));
                Touch();
                FrameReceived?.Invoke(shapes, frameCount);
                break;
            }
            case MessageType.Background:
                BackgroundChanged?.Invoke(r.ReadString());
                break;
            case MessageType.Zoom:
                ZoomRequested?.Invoke(r.ReadDouble(), r.ReadDouble());
                break;
            case MessageType.ConsoleLine:
                ConsoleLine?.Invoke(r.ReadByte(), r.ReadString(), r.ReadString());
                break;
            case MessageType.SketchStopped:
                _sketchRunning = false;
                SketchStopped?.Invoke();
                break;
        }
    }

    private void CheckHang(object? _)
    {
        if (!_sketchRunning || _disposed) return;
        var idle = TimeSpan.FromTicks(DateTime.UtcNow.Ticks - Interlocked.Read(ref _lastFrameTicks));
        if (idle <= HangTimeout) return;

        // No frame for too long -> Draw is stuck (infinite loop). The only cure is to kill it.
        _sketchRunning = false;
        try { _proc?.Kill(entireProcessTree: true); } catch { /* already gone */ }
        Hung?.Invoke($"Sketch produced no frame for {idle.TotalSeconds:F1}s — likely an infinite loop. The sketch host was terminated; the app is unaffected.");
    }

    private void Touch() => Interlocked.Exchange(ref _lastFrameTicks, DateTime.UtcNow.Ticks);

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        try { _watchdog?.Dispose(); } catch { /* ignore */ }
        try { _channel?.Write(MessageType.Shutdown); } catch { /* child may be gone */ }

        try
        {
            if (_proc is { HasExited: false } && !_proc.WaitForExit(1000))
                _proc.Kill(entireProcessTree: true);
        }
        catch { /* ignore */ }

        try { _channel?.Dispose(); } catch { /* ignore */ }
        try { _proc?.Dispose(); } catch { /* ignore */ }
    }
}
