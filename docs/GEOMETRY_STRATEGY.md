# Geometry Strategy

Code2Viz currently ships two geometry namespaces:

- `Code2Viz.Geometry`: app-integrated shapes (auto-registers with canvas/editor runtime).
- `C2VGeometry`: standalone reusable geometry library with no WPF/runtime coupling.

## Source of Truth

- Geometry algorithms are expected to stay behaviorally aligned across both libraries.
- Runtime/UI integration concerns remain only in `Code2Viz.Geometry`.

## Drift Guardrail

- `Tests/GeometryParityTests.cs` contains cross-library parity checks for key operations:
  - circle area/circumference
  - region move + containment
  - curve intersection behavior
  - polygon union area

These tests run in CI to detect regressions when one geometry implementation changes.
