# Focused sys validation

This executable compares the selected C# or original Erlang/BEAM sys against an
admitted OTP-29.1.1 capture. It verifies all 37 export signatures and per-export
ownership before executing the same Erlang observers. Production needs no ERTS.

From the repository:

```powershell
dotnet build tools/Validation/Erlang.SysCompatibility -m:1
pwsh -File tools/validate-sys-original.ps1 -Output artifacts/sys/my-new-capture
dotnet tools/Validation/Erlang.SysCompatibility/bin/Debug/net10.0/Erlang.SysCompatibility.dll . artifacts/sys/my-new-capture Managed
dotnet tools/Validation/Erlang.SysCompatibility/bin/Debug/net10.0/Erlang.SysCompatibility.dll . artifacts/sys/my-new-capture Beam
```

Run full backends sequentially: they own files in the captured directory. Supply
`-BacktraceDepth N` when capturing and append `errors` when replaying the focused
52-row error subset. Captures are immutable; use a new directory each time.
Corpora retain fixture paths, so create a fresh capture for portable replay.

The original helper closure and actual file_server/file IO processes execute on
.NET; feature state is restored from the oracle through native persistent_term.
No sys implementation is substituted for the chosen backend. Five unchanged
upstream sys_SUITE testcase bodies and original sys_sp1/sys_sp2 are included.
Reference sources preserve their upstream Apache-2.0 notices.

The observer normalizes ephemeral PIDs/references/functions and projects changing
statistics samples while retaining exception frames and locations. This finite
Windows acceptance does not certify universal compatibility. See the complete
[Part591 receipt](../../../docs/validation/part-591/README.md) and
[backend contract](../../../docs/sys-backends.md).
