# Compiled binary_part/2 gc_bif2 contract observations (818)

Status: **integrated and executed locally by Root on the active Part552 product**. The source-only packet author did not execute it; Root subsequently built the actual product and ran the admitted original compiler and both runtime profiles. Actual compiler coverage is6/6; all38 rows match as complete raw AND deterministic ETF in each profile, including complete error stacks. Both runtime closures and source/provider cleanup pass. Active affected regression passes53/53. Independent review of the exact two-file correction and actual ETF artifacts found no blocker. Exact publication-head CI remains pending. API registry delta **0**; no full API compatibility promotion.

## Scope and real process-entry context

The 38-row fixture has 30 binary_part cases and 8 arithmetic controls. Exported unknown-argument functions plus `no_inline` and dynamic `apply/3` prevent result constant folding. Both engines execute the SAME original `capture/0` BEAM; it calls actual `spawn_monitor(Module,entry,[Parent])`. The real MFA child calls the same `rows/0 -> row/3 -> exported function` chain, catches complete `{Class,Reason,Stack}` and sends its complete rows back. Parent erl_eval versus C# host entry does not appear in child stacks. No stack prefix removal, source metadata filtering, frame projection, result normalization or manually assembled instruction image is used.

Body forward/negative-length/empty cases, bad binary/range/type/bounds/large-integer errors, both operand aliases, live-register preservation, guards, catch/try and guard nonzero failure labels are covered. The original compiler must prove all six opcode gates in `coverage.json`; if it emits a different legal register plan the preparation FAILS with whole disassembly retained. Do not silently waive a failed compiler coverage gate. `disassembly.etf` and textual disassembly retain all original function/instruction metadata.

The managed driver publishes exactly this original image with no fixture native replacement; checks its source owner and SHA; verifies all non-fixture exports remain unchanged; and serializes actual results through the real runtime `erlang:term_to_binary/2`. Both whole ordinary and deterministic ETF files must match the original exactly, including badarg/badarith reasons, full stacks, error_info and source metadata. All original rows and all complete actual child failures remain on disk.

## Integrated runtime change

The active Core admits only native catalog erlang:binary_part/2 in gc_bif2; all other imports still pass the preexisting ArithmeticImport allowlist. The interpreter reads both operands before destination mutation. Binary_part uses the actual BeamExternalImports native entry; arithmetic keeps Modules.Call with the same Erlang module/function and same operand objects. The preexisting exception/failure-label dispatch remains unchanged. Eight controls compare numeric outcomes AND complete badarith/zero-divisor stacks. The active53/53 regression includes existing BeamSmallOpcode controls and ordinary/native entry coverage; this fixture does not replace them.

## Integration and exact execution

1. Copy only payload targets, after checking manifest preimages. Apply the ONE guarded Program method-dispatch hunk from patches/program.json; proof/Program.cs is syntax evidence only, never a wholesale replacement.
2. Root builds the backend against the actual two-file candidate and records its source/build receipt. The collector snapshots actual source bytes and loaded host closure, but deliberately records `SourceToBinaryBuildAssociationIndependentlyProven=false`: that snapshot alone is not a compiler/build provenance proof.
3. Execute on Windows with the admitted original provider (or Linux with the existing owned pinned-source build receipt):

```powershell
py -3 -B tools/Compiler/Erlang.GcBinary818Observations/collect.py `
  --repository <actual-repository> `
  --runtime-source-root <actual-candidate-build-source-root> `
  --host <actual-built-Erlang.NifNativeBackend.dll> `
  --dotnet <absolute-dotnet.exe> `
  --official-admission <official-admission.json> `
  --admission-sha256 <actual-provider-receipt-sha256> `
  --output <repository>/artifacts/gc-binary818-fresh01
```

Linux uses `--build-receipt`, `--otp-source`, `--expected-head` instead of `--official-admission`; the imported existing provider enforces the pinned build and inventories it before/after. The collector uses existing owned original/child runners with 180-second bound and physical process cleanup receipts. Fixture receives have independent 30-second bounds; managed context/disposal uses the existing harness deadline. Preparation failure prevents dependent managed runs. Once preparation succeeds, BOTH ordinary/private-owned profiles are attempted independently and failures aggregate. Nothing is retried with looser checks.

Recorded local result: genuine original compiler coverage6/6, original38 rows, ordinary whole ETF38/38, private-owned whole ETF38/38, both runtime closures and unchanged registry. Raw receipts are retained in `artifacts/gc-binary818/active552-01`; exact source/build/observation checkpoint is frozen under the workspace's `artifacts/batch816/gc-binary-part-active`. This is a finite compiled-opcode contract, not all binary_part API boundaries, process/runtime GC closure or full API promotion.
