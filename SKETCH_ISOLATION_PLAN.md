# Sketch Isolation Plan — making Animator crash-proof against user code

## Goal

A user sketch is arbitrary C# we compile and run. **No blunder in a sketch should ever take
down `Animator.exe`.** This document records what is already done (Phase 1) and the design for
full immunity (Phase 2).

## Threat model — how user code can kill the host

| Blunder | Mechanism | Catchable in-process? |
|---|---|---|
| Null deref, div-by-zero, index out of range, `throw` | normal managed exception | ✅ Already caught in `SketchRuntime.Tick`/`Start` |
| **Infinite / deep recursion** | `StackOverflowException` | ❌ CLR fails fast — **uncatchable** |
| Runaway allocation | `OutOfMemoryException` | ⚠️ Sometimes catchable, process often left unstable |
| Bad interop / unsafe code | `AccessViolationException` (corrupted state) | ❌ Not delivered to managed `catch` by default |
| Explicit `Environment.FailFast(...)` | immediate termination | ❌ By design |
| `while(true){}` (no recursion, no allocation) | never throws | ❌ Hangs the render thread forever |

Since .NET Core removed AppDomains, **the only hard isolation boundary left is an OS process.**
That is the core conclusion driving Phase 2.

## Phase 1 — Stack-guard injection (DONE)

Kills the entire `StackOverflowException` class — which is the most common real-world sketch
blunder (the reported `circlesFill.cs` crash was mutual `Grow`↔`Shrink` recursion).

- `Execution/StackGuardRewriter.cs` (namespace `Code2Viz.Execution`) — a `CSharpSyntaxRewriter`
  that injects `RuntimeHelpers.EnsureSufficientExecutionStack()` at the top of every method-like
  body (methods, local functions, constructors, operators, accessors, expression-bodied
  properties/indexers). That API throws a **catchable** `InsufficientExecutionStackException`
  *before* the stack actually overflows. Single-sourced here and **linked** into Animator
  (`Animator.csproj`) — same convention as `Editor/`.
- Wired into **both** compilers:
  - Code2Viz `ModuleCompiler.CreateCompilationAsync(project, injectStackGuards: true)` — covers
    Main() mode and Code2Viz sketch mode. The param defaults to `false` so shared callers that map
    editor offsets onto the compilation (`CheckSyntaxAsync`, `RefactoringProvider`, MainWindow) are
    unaffected (the guard shifts in-line offsets).
  - Animator `SketchCompiler.CompileAndRunAsync` — always injects (sketch-only, no offset-based
    features); recreates the tree with the original path + UTF-8 encoding for PDB line mapping.
- `SketchRuntime.ReportError` (Animator) gives a friendly "runaway recursion" message; Code2Viz's
  Main() path reports it via the normal runtime-error formatter.
- Tests: `Tests/StackGuardRewriterTests.cs` (reaches the rewriter via the Code2Viz project
  reference). The behavioral tests compile guarded mutual/expression-body recursion and assert it
  throws the catchable exception instead of crashing the test host.

**Phase 1 does NOT cover:** infinite loops, OOM, native AVs, or `FailFast`. Those need Phase 2.

## Phase 2 — Process isolation (PLANNED)

Run the compiled sketch in a **separate child process**. If the child dies (StackOverflow, OOM,
AV, FailFast) or hangs (watchdog timeout → kill), the UI process (`Animator.exe`) detects the
exit, reports it in the console, and stays alive. This is the only way to be immune to *every*
blunder, including infinite loops.

### Architecture

```
┌────────────────────────┐         named pipe          ┌──────────────────────────┐
│  Animator.exe (UI)     │  ── source / control ──▶    │  SketchHost.exe (child)   │
│  - AnimCanvas          │                             │  - compiles the sketch    │
│  - editor / console    │  ◀── frame shape data ──    │  - runs Setup/Draw loop   │
│  - watchdog + restart  │  ◀── console / errors ──    │  - DefaultRegistry sink   │
└────────────────────────┘                             └──────────────────────────┘
```

- **New project `SketchHost/` (`SketchHost.exe`, console).** Owns the `AssemblyLoadContext`,
  the `SketchRuntime`, and the `ShapeRegistry`. Reuses `SketchCompiler` + `StackGuardRewriter`
  (keep the stack guard — cheaper than respawning on every recursion bug).
- **IPC over the child's redirected stdin/stdout** (decided during the POC — see Progress below).
  The `McpBridge` pipe was evaluated and rejected: it's request/response, one message per
  connection, byte-by-byte — unsuitable for a 60 fps push stream. Stdio needs no pipe-name
  management and the OS tears the pipes down when either process dies (clean crash detection).
- **UI → host messages:** `Compile(source, path)`, `Stop`, `SetLooping`, input state
  (`mouseX/Y`, `mousePressed`, `lastKey`), export-frame requests.
- **Host → UI messages:** per-frame shape batch, `Size`/`Background`/zoom requests, console
  lines, and a terminal `Error(phase, message)`.
- **Watchdog (UI side):** if a `Draw` frame exceeds a budget (e.g. 2–4 s) the UI kills and
  respawns the child and prints "sketch stopped: frame took too long (likely an infinite loop)".
  Catches the one class Phase 1 can't — `while(true){}`.
- **Crash handling (UI side):** subscribe to `Process.Exited`; a non-zero exit (or signal) →
  report "sketch process crashed: <exit reason>" and return the canvas to idle. The UI never
  shares a faulting stack/heap with the sketch, so it cannot be dragged down.

### Hard problem: per-frame geometry throughput

At 60 fps with thousands of shapes, serializing the full shape set every frame is the main risk.
Options, cheapest first:

1. **Compact binary frame protocol** — a flat per-shape record (type tag + numeric fields +
   color), length-prefixed, written to a `MemoryMappedFile` ring buffer; the pipe only carries a
   "frame N ready" signal. Avoids per-shape allocation and large pipe writes.
2. **Delta frames** — only send shapes that changed since last frame. Bigger win for mostly-static
   scenes, more bookkeeping.
3. **Render in the child, ship pixels** — child draws to an offscreen bitmap, ships the framebuffer
   (or a shared D3D/WIC surface). Trivially bounded bandwidth, but loses vector fidelity / canvas
   pan and complicates hit-testing.

Recommended: start with **(1)**; measure; add **(2)** only if a real sketch needs it.

### Migration steps

1. ✅ **Stand up `SketchHost.exe` + the IPC protocol; round-trip frames; prove crash/hang
   isolation.** (Increment 1 — see Progress below.)
2. ✅ **Wire the parent UI to the child** (Increment 2 — see below). `Animator/MainWindow.xaml.cs`
   branches on the `ANIMATOR_ISOLATE=1` flag: Run → `client.Run(...)`, `OnRendering` →
   `client.SendInput(...)`, `FrameReceived` → `Canvas.SetShapes(...)`, `BackgroundChanged`/
   `ZoomRequested` → canvas, `ConsoleLine` → console pane, `CompileCompleted`/`SketchStopped`/
   `Hung`/`Exited` → status; the child is (re)spawned on demand. In-process stays the default.
3. ✅ **Locate `SketchHost.exe` at runtime** — `AppSwitcher.FindSketchHostExe()` (mirrors
   `FindSiblingApp`: `{app}\SketchHost\` installed; solution-root `SketchHost\bin\{Config}\…` in dev).
4. ⬜ **Throughput pass if needed** — measure frame serialization under a heavy sketch; only then
   add the shared-memory ring / delta frames from the "Hard problem" section. (POC uses plain
   length-prefixed stdio, which was fine for the test sketches.)
5. ⬜ **Bundle `SketchHost.exe` into the installer** next to `Animator.exe` (mirror the existing
   `{app}\Animator\` packaging rule — see `installer.iss`) and add a "Build SketchHost (Release)"
   step to the release workflow.
6. ⬜ **Delete the in-process `Start`/`Tick` path** once parity is verified.

Optional cleanup (not required): extract `Sketch`/`SketchRuntime`/`SketchCompiler`/`ShapeRegistry`/
`ConsoleOutput` into a non-WPF library so the child needn't reference the WPF Animator assembly.
Today `SketchHost` references `Animator.csproj` and runs headless; it works, but pulls WPF/AvalonEdit
into the child.

### Progress — Increment 1 (POC, done)

A working vertical slice landed, additive and non-invasive (the live in-process path is untouched):

- **`Animator/Ipc/HostMessage.cs`** — `MessageType` tags + `MessageChannel`, a length-prefixed
  binary framing over a duplex stream (`[4-byte LE length][1-byte type][body]`), with synchronized
  writes so the frame loop and console callback can share one output stream.
- **`Animator/Ipc/ShapeCodec.cs`** — encodes the 8 shape types AnimCanvas renders (others dropped,
  as today) and decodes them back into reconstructed `C2VGeometry.Shape`s, so `AnimCanvas.SetShapes`
  is a drop-in on the parent.
- **`Animator/Ipc/SketchHostClient.cs`** — parent handle: spawns the child, pumps the channel on a
  reader thread, raises events (`FrameReceived`, `ConsoleLine`, `BackgroundChanged`, `ZoomRequested`,
  `CompileCompleted`, `SketchStopped`, `Hung`, `Exited`), and runs the **watchdog** that kills a
  child producing no frames (infinite loop) so the parent UI never freezes.
- **`SketchHost/` (`SketchHost.exe`)** — the child: owns the real `SketchRuntime` + `SketchCompiler`,
  drives its own ~60 fps loop, redirects `Console.Out`→stderr so stray user `Console.WriteLine`
  can't corrupt the binary channel, and speaks the protocol over stdio. Added to `Code2Viz.sln`.

**Verified** with a 3-sketch harness (parent process spawning the child):
- benign sketch → frames stream and decode (1 shape/frame), zoom + background cross the boundary;
- `circlesFill.cs` → recursion caught by the Phase-1 guard, error reported over IPC, **child stays
  alive**;
- `while(true){}` → **watchdog kills the child** (~2.3 s), child exits, **parent survives**.

All three passed; the parent process survived all three. This proves spawn + duplex IPC + frame
streaming + crash isolation + the infinite-loop watchdog — the whole risky core.

### Progress — Increment 2 (UI wiring, done; flag-gated)

`Animator/MainWindow.xaml.cs` now runs sketches out-of-process when `ANIMATOR_ISOLATE=1`
(env var; default OFF so the in-process path stays authoritative). A `SketchIsRunning` property
unifies the running-state check across both paths; `EnsureHostClient()` (re)spawns the child and
wires its events to the canvas/console/status on the dispatcher; `RunSketch`/`StopSketch`/
`OnRendering`/`ToggleRun`/export all branch on the flag. `SketchHostClient.IsSketchRunning` added.

**Verified in the real GUI** (UI Automation invoking the actual ▶ Run button under
`ANIMATOR_ISOLATE=1`):
- benign sketch → invoking Run spawns a `SketchHost.exe` child; killing the parent leaves **no
  orphan** (child self-exits on stdin EOF);
- `while(true){}` → child spawns and hangs, the **watchdog kills it ~3 s later**, and the Animator
  parent **stays alive** — the freeze case the in-process path can't survive.

Remaining before this can become the default: steps 4–6 (throughput check, installer bundling,
delete the in-process path).

### Cost / risk

- **Effort:** medium-large remaining (UI rewiring in step 2, packaging in step 5). The risky core
  (IPC + isolation) is now proven.
- **Risk:** frame-serialization throughput is the remaining unknown; plain stdio sufficed for the
  POC, optimize only if a heavy sketch needs it (step 4).
- **Payoff:** complete immunity — infinite loops, OOM, native crashes, and `FailFast` all become
  "the sketch process died, here's why" instead of a dead Animator.
