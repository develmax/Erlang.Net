# Part604 — sys backend validation

The C# and original Erlang/BEAM backends were compared with the admitted OTP
29.1.1 capture (ad05823719d77c8faee87348ea39513d4e2f99c5) on Windows.
The isolated branch starts at main commit 4f442034928cd00ca76d4e99074ff865cb3b9a03.

The scoped build completed with zero warnings and errors. The default-depth
matrix covers 137 observations per backend, including five unchanged
sys_SUITE testcases, actual file logging and original gen_server, gen_event
and gen_statem integration. The error profiles cover 52 observations per
backend at depths 0, 1, 2, 3 and 32. The main branch already supplies the BEAM
tail-fun frame handling used in these comparisons.

See [matrix](matrix.json), [build](build.log), [source audit](source-audit.json)
and the per-depth reports. Capture provenance records the pinned source,
fixture, feature state and ETF hashes. The observer normalizes ephemeral
identities and changing statistics samples. This scope leaves distributed
peers, cross-platform and resource stress, and complete Common Test integration
unverified. Full regression and canonical readiness regeneration are outside
the requested checks.

Run from the repository with the admitted official OTP provider available.

```powershell
dotnet build tools/Validation/Erlang.SysCompatibility -m:1 --nologo
py -3 tools/Validation/audit-sys-source.py
pwsh -File tools/validate-sys-original.ps1 -Output artifacts/sys/replay-001
dotnet tools/Validation/Erlang.SysCompatibility/bin/Debug/net10.0/Erlang.SysCompatibility.dll . artifacts/sys/replay-001 Managed
dotnet tools/Validation/Erlang.SysCompatibility/bin/Debug/net10.0/Erlang.SysCompatibility.dll . artifacts/sys/replay-001 Beam
```

Run the full backends sequentially because the file cases use the captured
fixture directory. To reproduce an error profile, capture to a fresh directory
with `-BacktraceDepth N` and append `errors` to each replay command.
