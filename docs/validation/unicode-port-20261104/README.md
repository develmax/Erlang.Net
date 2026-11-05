# Part563 — Unicode C# alternative and focused OTP acceptance

Baseline: **OTP 29.1.1**, commit
`ad05823719d77c8faee87348ea39513d4e2f99c5`. Local Windows x64, .NET 10.0.400.
Implementation of all **22 exported signatures** is complete. The unchanged
original Erlang sources/images remain selectable. Universal platform/resource
equivalence is not certified; the compatibility classification remains partial.

## Results

| Check | C# | Original BEAM on .NET |
| --- | ---: | ---: |
| Main original-OTP corpus | 65,339/65,339 | 65,339/65,339 |
| Actual calls from six unchanged upstream testcases | 711/711 | 711/711 |
| Long binaries, 8192-byte boundaries and nested normalization | 408/408 | 408/408 |
| Primary observations per backend | **66,458** | **66,458** |
| Permanent subset, overlapping the main corpus | 490/490 | 490/490 |
| Binary backing profiles | 29/29 | 29/29 |

Main-corpus outcomes include 52,533 values and 12,806 complete exception stacks.
The upstream-inclusive primary total is 53,185 values and 13,273 exceptions per
backend. Comparisons use complete terms, classes, reasons and stacks. No stack
frames are stripped. The identical observer worker body transports its outcome
through the send return value; the runtime-local pid is outside that outcome.

The four BEAM main-corpus partitions contain 16,335/16,335/16,335/16,334 records.
The admitted provider reconstructs their concatenation to the **identical ETF
byte stream**. These are the same inputs, not four alternative samples.

Nineteen explicitly selected regression groups pass: three new Unicode groups,
eight shared runtime/binary/cancellation/function groups and eight string/guest/
opaque-function groups. Both ordinary MSBuild consumer selections build and run
with equal UTF16/NFC results and backend/image/MVID receipts. Builds report zero
warnings/errors. Full regression was **not run**, as explicitly requested.

## Changes and reproduced repairs

- C# conversion wrappers/traps, BOMs, classification and eight normalization
  exports; shared native BIFs remain runtime primitives. The selected pinned
  `unicode_util` supplies data and normalization rules to both alternatives.
- Iterative `ml_map` port preserves nested residual structures, split characters,
  endian aliases, 8192-byte splitting and OTP's `cbv` incomplete-prefix rules.
- Negative integers with Latin1 output raise `badarg`; they are not substituted
  with an error tuple. Normalization rejects a non-binary conversion row with
  Erlang `badarg`, repairing the reproduced CLR `InvalidCastException`.
- Source helpers preserve `unicode.erl` lines 1373/1376, original function-clause
  locations and fake-stack `erl_stdlib_errors` metadata.
- Native UTF8 returns preserve source backing; Latin1 conversion retains its
  separately observed copy behavior. The `/3` ASCII no-conversion paths preserve
  the original input. External output uses the existing writable append model.
- BEAM ordinary `append` now uses the existing append engine as `private_append`
  already does, with actual guest writer admission. Internal BEAM fun calls use
  the existing guest invocation route; public/opaque callers retain their checks.
  These repairs close the measured backing-size mismatch in original Unicode.
- Exact reviewed native/library delegates preserve guest inputs and adopt fresh
  output scaffolding. Dependency refusal, source ownership, three shared native
  entries and separate backend identities are permanently tested.

## Evidence and reproduction

- [Admission summary](audit-summary.json), [API/source inventory](api-audit.md),
  [18 source bindings](api-source-catalog.json), [provider provenance](provenance.json).
- [Partition provenance](partition-provenance.json), complete per-corpus JSON
  reports beside this file, three partial regression reports and two example
  receipts. Default registry/API classifications are not promoted.
- [Raw evidence](raw-evidence.zip) contains complete corpora/partitions, original
  observations, unchanged upstream results, failed attempts, build/runtime logs,
  scoped source/binary snapshots and the incomplete-report rejection control.
  [File hashes](file-hashes.json) bind the published local receipt files.

```powershell
dotnet build tools/Validation/Erlang.UnicodeCompatibility -m:1
pwsh -File tools/validate-unicode-original.ps1 -Output artifacts/a-new-unicode-run -IncludeUpstreamSuite
py tools/Validation/audit-unicode-source.py artifacts/a-new-unicode-run
```

The validator acquires only from the admitted official provider, verifying its
execution inputs before and after acquisition. The retained source audit checks
the reference checkout by `git show OTP-29.1.1`, not its newer working HEAD.
For the partitioned acceptance, `summarize-unicode-audit.py` independently verifies
the reconstructed corpus, complete reports, current source/assembly hashes,
focused regression results and both consumer receipts. A deliberately incomplete
report is rejected. See the [backend guide](../../unicode-backends.md).

## Retained failures and scope

The first observer captured acquisition callers beyond the module stack; isolated
workers correct that harness error. Retain the negative-Latin1 mismatch, missing
error source metadata, normalization CLR failure, backing mismatches and temporary
diagnostic traces. All temporary runtime logging was removed. Earlier interrupted
runs are not complete acceptance.

The 200-case long-BEAM aggregate exceeded the two-minute command window. The final
long replay uses **one case per command under the same deadline** and passes all
408 cases. No timeout is recast as a valid Unicode result. The two batch drivers
use identical recorded runtime/stdlib assemblies. Timings are not a benchmark.

The initial consumer used an outdated prebuilt MSBuild task. Building the current
`tools/Build/Erlang.BeamBuild` closes that local setup gate. Two test-build name
collisions introduced by the new `UnicodeModule` type are fixed with explicit
qualification; the corrected test build passes. All failures remain in the archive.

Global regression and global readiness regeneration are deliberately excluded;
partial reports cannot substitute for the complete report required by readiness.
The prior generated snapshot remains historical. Physical ERTS resource ceilings,
all platforms and general module metadata equivalence remain separate acceptance
boundaries. The module is not advertised as universally 100% compatible from a
finite corpus. No scoped commit/push or CI/publication is claimed: pre-existing
unrelated dirty string/math/map work is preserved, including overlapping files.
