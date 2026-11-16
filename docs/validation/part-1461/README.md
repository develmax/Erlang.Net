# Part 1461 — Check release inlining and honor source no_inline

The source option binder keeps the automatic inlining request and opt-out independently. A source `no_inline` suppresses automatic inlining in either option order. The regression fixture checks the presence of the local helper with each order and its removal with `inline` alone.

## Release option experiment

An experimental build forwarded the release `inline`, `no_inline` and `inline_size` options to the existing AST inliner. Automatically copied functions received caller-local names. In the control fixture, both compilers removed the helper, produced the same closure names and returned the same tested values. The C# entry had 34 instructions and the OTP entry had 24.

The full experiment admitted all 1,366 sources, generated all images and recorded 383 exact matches with no status transitions. Both loaders accepted all 983 differing images. ELDAPv3 was Equal. It changed 56 images. Thirty-six functions gained normalized instruction equality and 98 lost it. Frame sizes moved closer to OTP for seven functions and farther for 862. The aggregate Code size gap grew by 121,466 bytes.

In `core_lint`, `body/4` and `gbody/4` gained one direct fold each, matching the reference count. C# emitted ten direct folds in `gexpr/4` and nineteen in `expr/4`, compared with eight and eleven in OTP. The pinned OTP `cerl_inline` pass measures weighted Core operations after substitution and uses a separate effort counter. The current C# pass counts AST expressions. Release option forwarding and the experimental closure numbering were removed from the current build pending closer inliner parity.

The previous release corpus had four or more differing section payloads in each of the 983 differing images. All differed in Code. An instruction change can move labels and change the export and local-function tables. Changes in atom indices can affect instructions and tables that refer to the same atoms.

- [Control fixture](probe.txt) and [experimental reproduction](check-release-inline.ps1)
- [Experiment corpus](experiment-otp-corpus.json), [focused checks](experiment-focused.json) and [compiler hash](experiment-compiler-hash.txt)
- [Core lint instructions](core-lint-inline.txt), [instruction equality](experiment-normalized-functions.txt), [frame and Code statistics](experiment-summary.txt) and [loader results](experiment-loader-and-code.txt)
- [Section payload comparison](experiment-chunk-equality.txt), [previous section comparison](previous-chunk-equality.txt) and [comparison script](compare-chunk-payloads.ps1)

The shared instruction ranking finds 676 images with projected fields moved after extraction, 530 with list checks scheduled differently around allocation, and 527 with tuple checks scheduled differently. The sets overlap and each image can contain several independent differences. [Shared Code ranking](shared-code-ranking.txt).

## Current build

```powershell
dotnet build tools/Validation/Erlang.BeamCorpus -m:1 -o artifacts/beam-backend/part1461-tool-checked
$otp = Join-Path $env:TEMP 'erl-cohort-7mzi5e1n/view/artifacts/library376-official-compiler/installed'
$env:ERLANG_BEAM_CORPUS_SAVE_DIFFERENCES = '1'
dotnet artifacts/beam-backend/part1461-tool-checked/Erlang.BeamCorpus.dll $otp artifacts/beam-backend/otp-corpus-part1461-checked.json artifacts/beam-backend/part929-ordered-headers artifacts/beam-backend/part1058-source-overlay
```

All 1,366 sources were admitted and generated a BEAM image. The full comparison records 383 Equal and 983 Different. Every generated file is identical to its Part 1460 counterpart. ELDAPv3 remains Equal after C# ASN.1 generation. Both loaders accepted all differing images. Three focused .NET tests passed against the same compiler binary. The full .NET suite and canonical readiness validation remain pending.

- [Current corpus](otp-corpus.json), [status changes](status-changes.txt), [loader results](loader-and-code.txt) and [section payloads](chunk-equality.txt)
- [Focused tests](focused.json), [compiler hash](compiler-hash.txt) and [OTP option precedence](otp-inline-opt-out.txt)
