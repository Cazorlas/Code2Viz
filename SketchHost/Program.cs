using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Animator.Compiler;
using Animator.Console;
using Animator.Ipc;
using Animator.Sketching;
using C2VGeometry;

namespace SketchHost;

internal static class Program
{
    private static int Main() => new HostSession().Run();
}

/// <summary>
/// The child process. Owns the real <see cref="SketchRuntime"/> + <see cref="SketchCompiler"/>,
/// drives its own ~60 fps frame loop, and speaks the <see cref="MessageChannel"/> protocol over
/// stdin/stdout. Because it is a separate process, any blunder in user code — uncatchable stack
/// overflow, OOM, native crash, or infinite loop (killed by the parent's watchdog) — dies here
/// without touching the parent UI.
/// </summary>
internal sealed class HostSession
{
    private readonly MessageChannel _channel;
    private readonly SketchCompiler _compiler = new();

    private volatile bool _shutdown;

    private readonly object _inputLock = new();
    private double _mouseX, _mouseY;
    private bool _mousePressed, _keyPressed;
    private string _lastKey = "";

    private readonly object _consoleLock = new();
    private int _consoleSent;

    private bool _prevRunning;

    public HostSession()
    {
        // Capture the REAL stdout for the binary protocol, then redirect Console.Out to stderr so
        // a stray Console.WriteLine in user sketch code can't corrupt the channel.
        var stdout = Console.OpenStandardOutput();
        var stdin = Console.OpenStandardInput();
        Console.SetOut(new StreamWriter(Console.OpenStandardError()) { AutoFlush = true });
        _channel = new MessageChannel(stdin, stdout);
    }

    public int Run()
    {
        ConsoleOutput.Instance.Changed += OnConsoleChanged;
        SketchRuntime.Instance.FrameProduced += SendFrame;

        var loop = new Thread(FrameLoop) { IsBackground = true, Name = "SketchFrameLoop" };
        loop.Start();

        _channel.Write(MessageType.Ready);

        try
        {
            while (!_shutdown && _channel.Read() is { } msg)
                Handle(msg);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[SketchHost] fatal: {ex}");
        }

        _shutdown = true;
        try { SketchRuntime.Instance.Stop(); } catch { /* ignore */ }
        return 0;
    }

    private void Handle(Message msg)
    {
        var r = msg.Body;
        switch (msg.Type)
        {
            case MessageType.RunSource:
            {
                string name = r.ReadString();
                string source = r.ReadString();
                CompileResult result = _compiler.CompileAndRunAsync(source, name).GetAwaiter().GetResult();
                _channel.Write(MessageType.CompileResult, w => { w.Write(result.Success); w.Write(result.Error ?? ""); });
                // Compiled but the sketch didn't stay running (e.g. Setup threw) -> tell the parent.
                if (result.Success && !SketchRuntime.Instance.IsRunning)
                    _channel.Write(MessageType.SketchStopped);
                break;
            }
            case MessageType.Input:
            {
                double mx = r.ReadDouble(), my = r.ReadDouble();
                bool pressed = r.ReadBoolean(), keyPressed = r.ReadBoolean();
                string lastKey = r.ReadString();
                lock (_inputLock)
                {
                    _mouseX = mx; _mouseY = my;
                    _mousePressed = pressed; _keyPressed = keyPressed; _lastKey = lastKey;
                }
                break;
            }
            case MessageType.Stop:
                SketchRuntime.Instance.Stop();
                break;
            case MessageType.Shutdown:
                _shutdown = true;
                break;
        }
    }

    private void FrameLoop()
    {
        while (!_shutdown)
        {
            if (SketchRuntime.Instance.IsRunning)
            {
                lock (_inputLock)
                    SketchRuntime.Instance.UpdateInputState(_mouseX, _mouseY, _mousePressed, _keyPressed, _lastKey);

                SketchRuntime.Instance.Tick(); // runs user Draw(); fires FrameProduced -> SendFrame

                var bg = SketchRuntime.Instance.TryConsumeBackground();
                if (bg != null) _channel.Write(MessageType.Background, w => w.Write(bg));

                if (SketchRuntime.Instance.TryConsumeZoomRequest(out double zw, out double zh))
                    _channel.Write(MessageType.Zoom, w => { w.Write(zw); w.Write(zh); });
            }

            bool running = SketchRuntime.Instance.IsRunning;
            if (_prevRunning && !running)
                _channel.Write(MessageType.SketchStopped); // sketch errored during Draw and stopped itself
            _prevRunning = running;

            Thread.Sleep(16); // ~60 fps
        }
    }

    private void SendFrame(IReadOnlyList<Shape> shapes)
    {
        int frameCount = SketchRuntime.Instance.Active?.FrameCount ?? 0;
        _channel.Write(MessageType.Frame, w =>
        {
            w.Write(frameCount);
            var renderable = new List<Shape>(shapes.Count);
            foreach (var s in shapes)
                if (s.IsVisible && ShapeCodec.IsRenderable(s)) renderable.Add(s);

            w.Write(renderable.Count);
            foreach (var s in renderable) ShapeCodec.Encode(w, s);
        });
    }

    private void OnConsoleChanged(object? sender, EventArgs e)
    {
        lock (_consoleLock)
        {
            var snap = ConsoleOutput.Instance.Snapshot();
            if (snap.Count < _consoleSent) _consoleSent = 0; // console was cleared
            for (; _consoleSent < snap.Count; _consoleSent++)
            {
                var line = snap[_consoleSent];
                _channel.Write(MessageType.ConsoleLine, w =>
                {
                    w.Write((byte)line.Level);
                    w.Write(line.Source);
                    w.Write(line.Message);
                });
            }
        }
    }
}
