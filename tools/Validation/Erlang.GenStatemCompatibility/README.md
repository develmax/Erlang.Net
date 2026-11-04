# Focused gen_statem compatibility runner

Implement first, then capture/replay only the connected module contracts.
The checked-in Erlang observers execute the same calls against original
OTP-29.1.1 and both .NET alternatives. Values, complete exception terms/stacks
and deterministic scenario outcomes are compared without removing frames.

```powershell
pwsh -File tools/validate-gen-statem-original.ps1 -Output artifacts/gen-statem/oracle-new
dotnet build tools/Validation/Erlang.GenStatemCompatibility -m:1 -o artifacts/gen-statem/runner-new
dotnet artifacts/gen-statem/runner-new/Erlang.GenStatemCompatibility.dll . artifacts/gen-statem/oracle-new Managed
dotnet artifacts/gen-statem/runner-new/Erlang.GenStatemCompatibility.dll . artifacts/gen-statem/oracle-new Beam
python tools/Validation/audit-gen-statem-source.py artifacts/gen-statem/source-audit.json
python tools/Validation/summarize-gen-statem.py . artifacts/gen-statem/oracle-new artifacts/gen-statem/source-audit.json artifacts/gen-statem/acceptance.json
```

Use the installed Python executable if it is not on PATH. The capture command
refuses an existing output directory and rechecks the admitted kernel/stdlib/
ERTS source/image inventory and VM files before and after execution. Replay
refuses stale observer/companion source, changed corpora/features/export files
and a different gen_statem original image. Source admission and the summary
bind the actual C# files and both actual assembly hashes. A named fourth
argument runs a subset, marked incomplete in the report; unknown names fail.

The runtime uses actual original helpers and the retained
`beam_code_server_subject410` fixture to boot the original init/loader/code_server
with real ETS and filesystem primitives. Feature configuration comes from the
original OTP bootstrap; Logger primary level is `none`. No production MFA is
stubbed by the runner. One backend is selected for gen_statem; its original
image is absent from the managed execution plan, and export owners are checked.

The corpus has 74 API rows and 311 scenarios, including a 256-case mode/event
matrix. A full focused pass is separate from full upstream-suite acceptance,
native resource and platform parity, metadata parity and global regression.
See [backend scope](../../../docs/gen-statem-backends.md).

The Managed-only `sys_retention` fourth argument runs a native-callback lifecycle
probe using the real original sys/gen/proc_lib closure. It installs32 successive
states and repeatedly gets state/status and suspends/resumes the process. A
bounded walk from the actual mailbox Task counts retained managed machine
instances, and stop must execute terminate exactly once. This host retention
check is separate from OTP differential corpus counts. See Part587 for its
old-library reproduction, exact assembly hashes and retained failed prototypes.
