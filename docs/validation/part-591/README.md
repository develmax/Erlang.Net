# Part591 — sys port receipt — 2026-11-05

The implementation covers all 37 non-module_info exports of pinned OTP-29.1.1
(`ad05823719d77c8faee87348ea39513d4e2f99c5`) in C#, including behaviour_info/1.
Original sys.erl/sys.beam are unchanged. Implementation of the complete surface
preceded builds and focused checks. [Selection and dependency contract](../../sys-backends.md).

## Final acceptance

| Check | Actual result | Receipt |
| --- | --- | --- |
| Ordinary current-checkout focused build, no exclusions | 0 warnings/errors | [Build](final/build.log) |
| Pinned source/export inventory; no original-sys fallback | 37/37 | [Audit](final/source-audit.json) |
| Current C# implementation, depth 8 | 137/137 | [Managed](final/depth-8/Managed.json) |
| Current original Erlang/BEAM, depth 8 | 137/137 | [Beam](final/depth-8/Beam.json) |
| Depth 0 error subset, both alternatives | 52/52 each | [Managed](final/depth-0/Managed.json), [Beam](final/depth-0/Beam.json) |
| Depth 1 error subset, both alternatives | 52/52 each | [Managed](final/depth-1/Managed.json), [Beam](final/depth-1/Beam.json) |
| Depth 2 error subset, both alternatives | 52/52 each | [Managed](final/depth-2/Managed.json), [Beam](final/depth-2/Beam.json) |
| Depth 3 error subset, both alternatives | 52/52 each | [Managed](final/depth-3/Managed.json), [Beam](final/depth-3/Beam.json) |
| Depth 32 error subset, both alternatives | 52/52 each | [Managed](final/depth-32/Managed.json), [Beam](final/depth-32/Beam.json) |
| SDK layout, scoped source set | 0 files need formatting | [Layout](final/style.log) |
| Changed corpus / changed observer source | Refused before runtime creation | [Corpus](final/negative-corpus.log), [Source](final/negative-source.log) |

[The admitted matrix](final/matrix.json) verifies that all twelve final comparison
receipts identify the same Stdlib, Runtime, Terms and validator assemblies.
[Source inputs](final/source-inputs.json) include the real shared integration files;
their hashes identify inputs, not exclusive authorship or semantic correctness.
Canonical readiness and full regression reports are preserved, not replaced by
this partial test set.

## Compared scope

The full corpus exercises every exported signature, default/infinite/explicit
and zero timeouts, local names, actual system messages, suspended selective
receive, queued ordinary messages, parent EXIT, hibernation, status/state/code
callbacks, errors/throws/exits/bad returns and response errors. It checks bounded
log representations through wraparound, debug option ordering/duplicates,
identified/legacy callbacks and done/error removal, trace/print effects,
statistics message counters, file open/write/close/reopen/no_debug and failure
results. Real file bytes are compared in both alternatives.

Five unchanged upstream sys_SUITE testcase bodies run: log, stats, suspend,
install and special_process (including original sys_sp1/sys_sp2). Additional
stateful cases use actual original gen_server, gen_event and gen_statem. The
trace and file cases use original IO/formatting/file helpers on .NET. The real
file_server callback is started through gen_server:start/4, the non-master branch
of file_server's initializer wrapper; no file-server replacement is installed.
The standalone upstream cases use a valid acknowledging group-leader IO device;
trace/print messages and filesystem effects have separate observations.

Fixture-owned PIDs, references and function values are normalized to symbolic
identities; stack frames, locations and reasons are retained. Statistics option
initialization projects changing time/reduction samples to validated tags; live
statistics compare incoming/outgoing counters, not cross-VM clock or reduction
counts. These projections are explicit limits of the finite observations.
The error profile executes 51 exception observations and one nested code-change
error witness from the same corpus: 52 total, rather than all 137 observations.

## Shared BEAM correction

The current interpreter duplicated a failed tail-fun frame: sys:print_event/2
appeared twice for a non-function formatter. The original VM produces one frame.
Check the syntactic tail position before fun admission fails; retain the saved
continuation only for non-tail calls. Permanent authentic-BEAM controls distinguish
tail/non-tail badfun and badarity, preserving the non-tail behavior. Both backend
runs execute these controls. Other apply/receive paths retain their previous
continuation behavior; no global stack stripping or result normalization is used.

## Reproduce

Run from the repository with the admitted official provider already installed:

```powershell
dotnet build tools/Validation/Erlang.SysCompatibility -m:1 --nologo
py -3 tools/Validation/audit-sys-source.py
pwsh -NoProfile -File tools/validate-sys-original.ps1 -Output artifacts/sys/replay-001
```

Then run each backend sequentially because the full cases own files in the same
captured directory:

```powershell
dotnet tools/Validation/Erlang.SysCompatibility/bin/Debug/net10.0/Erlang.SysCompatibility.dll . artifacts/sys/replay-001 Managed
dotnet tools/Validation/Erlang.SysCompatibility/bin/Debug/net10.0/Erlang.SysCompatibility.dll . artifacts/sys/replay-001 Beam
```

For another depth, capture to a new output directory with `-BacktraceDepth N`,
then append `errors` to the validator command. Existing capture directories are
refused. Saved corpora retain their captured fixture directory; make a fresh
capture for portable replay. The launcher/VM/stdlib/kernel/erts closure is checked
before and after capture. The validator verifies source, fixture images, corpus,
feature state, exact original sys source/BEAM and per-export backend ownership.
Authentic erl_features persistent state is restored through native persistent_term.

## History and boundaries

Earlier unrelated concurrent-build failures and file/formatting-closure omissions
are retained under [history](history/). The original 80-row run also exposed a
harness problem: caller frames from write/erl_eval differed from a standalone
.NET invocation. Each observation now executes in a fresh process on both VMs;
no caller frames are removed. Missing net_kernel/raw_file_io/epp/scanner/feature
helpers were supplied as original modules, not stubbed results. The rejected
minimal init boot experiment is historical; final acceptance uses the actual
file_server behavior directly and makes no complete kernel-boot claim.

The [pre-fix original-BEAM receipt](history/before-tail-fix/Beam-132-of-133.json)
retains the duplicated frame. It is superseded by final receipts, not relabeled
as passing. A temporary frozen checkout enabled progress while other ports were
being written; all final receipts above were rerun against the ordinary current
checkout and identical final binaries, with no source exclusions.

Full seven-case Common Test capture integration (the unchanged trace/log_to_file
cases), genuinely connected remote/global/via peers, other OS/architectures,
owned-heap/resource/fatal-failure stress, all scheduler interleavings, metadata
and performance equivalence remain unverified. This is not a universal 100%
compatibility claim. No full Erlang.Tests run, validate.ps1, broad regression or
canonical readiness promotion was performed, as requested.

No commit, push, PR, merge or hosted-CI result is claimed. Shared integration
files already contain other in-progress ports; scoped publication must preserve
those changes and include the applicable native filesystem/runtime prerequisites.
