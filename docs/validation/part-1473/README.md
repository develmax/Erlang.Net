# Part 1473 — Keep guarded case selectors in registers

The case entry fold aliases a leading call result inside the branches that read the original value. It checks references in patterns, guards, branch bodies and subsequent expressions before removing the assignment. The compiler tests eligible tuple patterns and guards against x0 and saves the selector on the accepted path when the branch needs it. An unguarded final arm returning the selector uses the value already in x0 when the caller can consume it directly.

The control fixture's preserve/2 uses one frame slot, down from three and equal to OTP's frame size. Its comparison still lacks the typed tuple operand and uses a shared return. The survive/2 example preserves the selector through put/2. It still uses three frame slots, compared with two in OTP. The pinned OTP VM produces the same values and errors for the reference, previous C# and current C# images.

- [Control source](../../../tests/Erlang.Tests/Resources/Beam/beam_entry_result_case.erl) and [comparison](probe.txt)
- [Focused tests](focused.json) and [compiler hashes](compiler-hash.txt)

The reference is Windows OTP 29.1.1, commit ad05823719d77c8faee87348ea39513d4e2f99c5. Its beam_ssa_pre_codegen pass places frames, finds values that need Y registers, computes live intervals and allocates registers with linear scan. The current change reuses the C# emitter's case entry and reference analysis. It adds no dependency.

```powershell
dotnet build tools/Validation/Erlang.BeamCorpus -m:1 -o artifacts/beam-backend/part1473-checked-tool
dotnet build tests/Erlang.Tests -m:1 -o artifacts/beam-backend/part1473-tests
$otp = Join-Path $env:TEMP 'erl-cohort-7mzi5e1n/view/artifacts/library376-official-compiler/installed'
$env:ERLANG_BEAM_CORPUS_SAVE_DIFFERENCES = '1'
dotnet artifacts/beam-backend/part1473-checked-tool/Erlang.BeamCorpus.dll $otp artifacts/beam-backend/otp-corpus-part1473-checked.json artifacts/beam-backend/part929-ordered-headers artifacts/beam-backend/part1058-source-overlay
dotnet artifacts/beam-backend/part1473-tests/Erlang.Tests.dll --case compiler/beam/documentation-payload-matches-otp --case compiler/beam/rebuild-installed-otp-release-images --case compiler/beam/whole-module-matches-otp --case compiler/beam/call-result-bif-keeps-register --case compiler/beam/call-result-case-keeps-register --report artifacts/beam-backend/part1473-focused.json
```

The Windows release comparison generates all 1,366 images. It records 383 Equal and 983 Different, with zero source rejections, compiler errors and status transitions. ELDAPv3 remains Equal after C# ASN.1 generation. OTP accepts both images in all 983 differing pairs. Five focused .NET tests pass with the same compiler binary used for the corpus.

The new emission changes 369 images and 1,007 normalized function bodies. Frame sizes move closer to OTP in 318 functions, including 107 that reach the OTP size. The remaining 689 changed bodies keep the same distance in frame size. Seven functions stop immediately spilling x0 to Y. The aggregate Code size gap falls by 4,311 bytes. The set of functions matching OTP after instruction normalization stays the same.

Every remaining differing image has a Code payload difference. In 887 modules the first normalized instruction difference in at least one function is a larger allocate frame with the same live-register operand. These instruction signatures overlap with other differences. Use wxDisplay and logger_handler_watcher to confirm whole-file results while measuring common frame placement and live-range changes across the corpus.

- [Full corpus](otp-corpus.json), [status changes](status-changes.txt), and [OTP loading](loader-and-code.txt)
- [Frame and Code totals](summary.txt), [normalized functions](normalized-functions.txt), and [result spills](spill-delta.txt)
- [Frame ranking](frame-ranking.txt), [chunk ranking](chunk-ranking.txt), and [closest files by chunk count](closest-instructions.txt)

The full .NET suite was not rerun for this candidate. Canonical readiness and publication remain pending. The readiness comparison baseline remains the immutable Part 561 snapshot.
