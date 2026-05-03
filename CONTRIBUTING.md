# Contributing to Code2Viz

Thanks for your interest in contributing! Code2Viz is a WPF-based 2D geometry visualization tool, and contributions of all kinds are welcome — bug reports, feature requests, documentation improvements, and pull requests.

## Getting Started

### Prerequisites
- **.NET 9.0 SDK** or later
- **Windows** (the main app is WPF; Code2Viz.Web runs cross-platform)
- A C#-aware editor (Visual Studio, Rider, or VS Code with the C# Dev Kit)

### Build and Run
```bash
# Clone
git clone https://github.com/harilalmn/Code2Viz.git
cd Code2Viz

# Build
dotnet build

# Run the desktop app
dotnet run

# Run tests
dotnet test Tests/Code2Viz.Tests.csproj
```

For project layout, conventions, and the shape system overview, see [`CLAUDE.md`](CLAUDE.md) — it's the de-facto architecture doc.

## Reporting Bugs

Open an issue with:
1. **What you did** — code snippet or steps to reproduce
2. **What you expected** — the result you were hoping for
3. **What actually happened** — screenshot, error message, or stack trace
4. **Environment** — OS version, .NET SDK version

## Suggesting Features

Open an issue describing the use case before opening a PR for anything non-trivial. This avoids you spending time on a change that doesn't fit the project's direction.

## Pull Requests

1. **Fork** the repo and create a topic branch (`feature/my-thing` or `fix/some-bug`).
2. **Match the existing style.** No reformatting passes mixed with logic changes.
3. **Keep PRs focused.** One concern per PR.
4. **Update documentation** if your change affects the public API. The project has a strict docs policy — see [CLAUDE.md](CLAUDE.md) for the full list (README, `DocGenerator.cs`, `McpServer/SKILL.md`, `McpServer/Resources/ApiReferenceResource.cs`).
5. **Run tests** locally with `dotnet test` and confirm `dotnet build` is clean.
6. **Write a clear commit message** — first line summarizes the change, body explains the *why*.

### Coding Conventions
- Use the type aliases (`Point2D`, `Line2D`, etc.) inside `RenderCanvas.cs` to avoid clashes with WPF types.
- All shapes auto-register on construction — don't add `Draw()` calls.
- Mathematical coordinates: Y-axis is **up**, origin is at canvas center.
- Use `VPoint.Internal(x, y)` for intermediate points that should not register on the canvas.
- Avoid adding new top-level files unless necessary; prefer extending existing ones.

## Code of Conduct

Be respectful, constructive, and patient. Disagree with ideas, not people. Maintainers may close or lock conversations that get heated.

## Licensing

By submitting a contribution, you agree your work will be released under the project's [MIT License](LICENSE).
