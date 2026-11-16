# Part 1471 — Keep call results in X registers for following BIFs

The existing entry BIF planner can now return a BIF result directly. A sequence may read a call result from x0 and original arguments from saved Y slots. Temporary BIF results stay in X registers. The planner emits a GC live count only for GC BIFs. It preserves the order of operand evaluation, source locations and exception paths.

Successful size and length operations refine subsequent operands drawn from the same call result. Standard list producers provide a proper-list type. The compiler checks BIF resolution so that a local function of the same name keeps ordinary call semantics. Numeric range information for byte size and min matches the pinned OTP reference.

The control fixture generates the same 852-byte image as OTP 29.1.1. It covers a returned binary part with a saved length limit and an ordinary tail call with a list length and the original list. The original and generated images give the same results and badarg failures. The managed regression also tests a call whose result type is established by byte_size.

- [Deterministic source](../../../tests/Erlang.Tests/Resources/Beam/beam_call_result_bif.erl) and [OTP fixture](../../../tests/Erlang.Tests/Resources/Beam/beam_call_result_bif.beam)
- [Image hashes](fixture-image.txt), [instruction and execution comparison](probe-checked.txt), and [release prefixes](release-prefixes.txt)

```powershell
dotnet build tools/Validation/Erlang.BeamCorpus -m:1 -o artifacts/beam-backend/part1471-tool-checked
$otp = Join-Path $env:TEMP 'erl-cohort-7mzi5e1n/view/artifacts/library376-official-compiler/installed'
$env:ERLANG_BEAM_CORPUS_SAVE_DIFFERENCES = '1'
dotnet artifacts/beam-backend/part1471-tool-checked/Erlang.BeamCorpus.dll $otp artifacts/beam-backend/otp-corpus-part1471-checked.json artifacts/beam-backend/part929-ordered-headers artifacts/beam-backend/part1058-source-overlay
```

The corrected Windows OTP 29.1.1 comparison generates all 1,366 images. It records 383 Equal and 983 Different with zero source rejections and no status transitions. ELDAPv3 remains Equal after C# ASN.1 generation. OTP accepts both images of every differing pair.

Six release images change. Ten functions avoid the immediate x0-to-Y spill and reach the reference frame size. The aggregate Code size gap falls by 79 bytes. The macN/5 instructions match after label normalization. No functions lose normalized equality. The eight public_key encoders still lack the proper-list annotation on length's operand.

The previous 629-function spill group contains 230 functions in 153 modules whose first reference to the saved slot is a reload into X. This ranking counts instruction order. Each candidate still needs a liveness check across calls and branch edges.

- [Full corpus](otp-corpus.json), [status changes](status-changes.txt), and [OTP loading](loader-and-code.txt)
- [Spill differences](spill-delta.txt), [frame and Code totals](summary.txt), and [normalized functions](normalized-functions.txt)
- [Remaining instructions](release-remaining.txt) and [spill consumers](spill-consumers.txt)

All four focused .NET cases passed on the same compiler as the release comparison. The byte and runtime checks for the new fixture run in a dedicated case. The existing whole-module fixture case exceeded its 15-second asynchronous limit on this machine and completed in a direct run. Its individual limit is now 30 seconds. The previous Part 1469 full run ended with 9,515 passed and 185 failed. Canonical readiness and publication remain pending. The readiness comparison baseline remains the immutable Part 561 snapshot.

- [Focused tests](focused.json), [previous timeout](focused-default-budget.json), [direct whole-module execution](whole-module-direct.txt), and [compiler hashes](compiler-hash.txt)
- [Managed execution of the new BIF chain](managed-probe.txt)
