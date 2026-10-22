# Build-only BEAM artifact inspection

`BeamArtifactInspector.Inspect(bytes)` returns immutable metadata: module name, the on_load flag and the SHA256 of owned input bytes. Source postcompile/cache checks, application snapshots and application MSBuild preflight use this explicit inspection path. Each caller retains its existing name, digest and on_load rules.

Valid external-function literals such as `fun lists:reverse/1` are inspected using a private per-call export-entry table. Inspection creates no ProcessRuntime, registers no module and invokes no guest function or on_load callback. The returned metadata contains no executable module or function. The same bounded structure/link reader still validates the image; metadata acceptance does not guarantee support for every instruction or import.

Runtime loading remains separate: use the owning runtime's `Modules.PrepareBeam` with explicit dependencies and backend selection, then publish the prepared image. Runtime-owned factories provide actual lazy function pointers that dispatch through the calling context's current module registry. Part383 verifies a source-built module returning such a pointer and an actual call against explicitly selected original lists BEAM.

Ordinary `BeamModule.Prepare` and general ETF decoding retain their existing factory restrictions. Inspection does not enable local NEW_FUN literals, implicit C# library fallback, startup/config application, build-time transforms or on_load execution. Original OTP libraries remain Erlang; the original compiler can be an admitted build-time dependency while production execution uses our .NET runtime.

The internal deadline observation seam has no CLI, environment or request selector. Normal child deadlines remain System120seconds. Scoped validation delivers the registered timer callback early while an admitted original child is observed alive; it does not claim120seconds elapsed. Cleanup observations cover retained handles and a bounded descendant snapshot. Source/header mutation controls prove the same retained emulator alive around the staged write and native input_admission refusal, without claiming a specific parser or ReadFile instruction.
