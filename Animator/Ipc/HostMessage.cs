using System;
using System.Buffers.Binary;
using System.IO;
using System.Text;

namespace Animator.Ipc;

/// <summary>
/// Message tags on the parent&lt;-&gt;child sketch-host wire. Parent-&gt;child are control
/// messages; child-&gt;parent are data/event messages.
/// </summary>
public enum MessageType : byte
{
    // parent -> child (control)
    RunSource = 1,   // string name, string source
    Input = 2,       // double mouseX, double mouseY, bool mousePressed, bool keyPressed, string lastKey
    Stop = 3,        // (none) stop the running sketch; child stays alive, idle
    Shutdown = 4,    // (none) child exits cleanly

    // child -> parent (data / events)
    CompileResult = 10, // bool success, string error
    Frame = 11,         // int frameCount, int shapeCount, [ShapeCodec records...]
    Background = 12,    // string color
    Zoom = 13,          // double width, double height
    ConsoleLine = 14,   // byte level, string source, string message
    SketchStopped = 15, // (none) the running sketch ended/errored; child idle
    Ready = 16,         // (none) child booted and is listening
}

/// <summary>One decoded inbound message: its tag plus a reader positioned at the body.</summary>
public readonly record struct Message(MessageType Type, BinaryReader Body);

/// <summary>
/// Length-prefixed binary message channel over a duplex byte stream (the child's redirected
/// stdin/stdout). Frame: [4-byte little-endian length][1-byte type][body]. Writes are
/// serialized so a frame thread and a console-forwarding callback can share one output stream.
/// </summary>
public sealed class MessageChannel : IDisposable
{
    private const int MaxMessageBytes = 64 * 1024 * 1024; // guard against a corrupt length

    private readonly Stream _in;
    private readonly Stream _out;
    private readonly object _writeLock = new();
    private bool _disposed;

    public MessageChannel(Stream input, Stream output)
    {
        _in = input;
        _out = output;
    }

    /// <summary>Writes one framed message. <paramref name="body"/> fills the payload after the tag.</summary>
    public void Write(MessageType type, Action<BinaryWriter>? body = null)
    {
        using var ms = new MemoryStream(256);
        using (var bw = new BinaryWriter(ms, Encoding.UTF8, leaveOpen: true))
        {
            bw.Write((byte)type);
            body?.Invoke(bw);
        }
        var payload = ms.GetBuffer();
        int len = (int)ms.Length;

        lock (_writeLock)
        {
            Span<byte> header = stackalloc byte[4];
            BinaryPrimitives.WriteInt32LittleEndian(header, len);
            _out.Write(header);
            _out.Write(payload, 0, len);
            _out.Flush();
        }
    }

    /// <summary>Reads one framed message, blocking. Returns null when the peer closes the stream.</summary>
    public Message? Read()
    {
        Span<byte> header = stackalloc byte[4];
        if (!ReadExactly(header)) return null;
        int len = BinaryPrimitives.ReadInt32LittleEndian(header);
        if (len <= 0 || len > MaxMessageBytes) return null;

        var buf = new byte[len];
        if (!ReadExactly(buf)) return null;

        var type = (MessageType)buf[0];
        var reader = new BinaryReader(new MemoryStream(buf, 1, len - 1, writable: false), Encoding.UTF8);
        return new Message(type, reader);
    }

    private bool ReadExactly(Span<byte> buffer)
    {
        int offset = 0;
        while (offset < buffer.Length)
        {
            int read;
            try { read = _in.Read(buffer.Slice(offset)); }
            catch (IOException) { return false; }      // peer died mid-read
            catch (ObjectDisposedException) { return false; }
            if (read <= 0) return false;               // clean EOF
            offset += read;
        }
        return true;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        try { _in.Dispose(); } catch { /* ignore */ }
        try { _out.Dispose(); } catch { /* ignore */ }
    }
}
