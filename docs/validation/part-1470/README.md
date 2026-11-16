# Part 1470 — Preserve specialized list entry lowering

The entry dispatcher gives the existing register-reusing list paths priority over the recursive list matcher. The existing two-clause emitter takes a deferred entry test. If it accepts the clause, the test precedes its extraction. If it declines, the structural matcher can still handle the function without an emitted prefix. The fallback and direct-call paths share the same list-test emitter.

The structural matcher derives closure capture order from the entry registers. Saved slots can follow the order in which the matcher binds variables. The lifted function receives original arguments before bindings extracted from patterns.

The targeted comparison checks 17 release modules and restores six normalized instruction sequences across five of them. Four use the existing nested-head and recursive-tail emission. One uses the existing two-clause result emitter and its distinct failure destinations. One uses the restored closure capture order.

- [Targeted function comparison](selected-functions.txt) and [OTP loading](selected-loader.txt)
- [Frame group breakdown](variable-frame-ranking.txt) and [release examples](variable-frame-examples.txt)

```powershell
dotnet build tools/Validation/Erlang.BeamCorpus -m:1 -o artifacts/beam-backend/part1470-tool-checked
$otp = Join-Path $env:TEMP 'erl-cohort-7mzi5e1n/view/artifacts/library376-official-compiler/installed'
$env:ERLANG_BEAM_CORPUS_SAVE_DIFFERENCES = '1'
dotnet artifacts/beam-backend/part1470-tool-checked/Erlang.BeamCorpus.dll $otp artifacts/beam-backend/otp-corpus-part1470-checked.json artifacts/beam-backend/part929-ordered-headers artifacts/beam-backend/part1058-source-overlay
```

An initial deferred callback ran during list extraction. Some result paths extract directly, so they skipped the callback and emitted an undefined fallback label. Other paths had already allocated a frame when the callback ran. The corrected emitter runs the callback before every accepted list result and before the frame. The regression covers direct copying, computed heads and tuple heads.

- [Initial compiler errors](initial-corpus-errors.txt), [fixed instructions and execution](cons-result-probe.txt), and [managed regression](../../../tests/Erlang.Tests/Compiler/Beam/BeamModuleEmissionChecks.cs)
- [Immediate result spills](call-result-ranking.txt)

The corrected Windows OTP 29.1.1 run generates all 1,366 images. It records 383 Equal and 983 Different with zero rejected sources, compiler errors and status transitions. ELDAPv3 remains Equal after C# ASN.1 generation. OTP accepts both images of all 983 differing pairs.

The change alters 64 images. Six functions regain normalized instruction equality and no others lose it. The aggregate Code size gap falls by 83 bytes. Of the 94 changed functions in the frame comparison, 13 move farther from the reference frame size and 81 keep their frame size distance.

The prior corpus contains 1,640 variable-head sequences with early frame operand differences across 527 modules. In 804 of them, the first expression assigns a direct call result. Only C# moves the first result immediately from x0 into a Y register in 629 functions across 292 modules. The pinned OTP pre-codegen pass identifies values live across X-register clobbers and defers saving call results so that nonoverlapping values can share a slot.

- [Full corpus](otp-corpus.json), [status transitions](status-changes.txt), and [OTP loading](loader-and-code.txt)
- [Normalized instruction comparison](normalized-functions.txt), [frame changes](frame-delta.txt), and [frame and Code totals](summary.txt)

All three focused .NET cases passed on the final compiler. The whole-module case timed out during the concurrent run and passed after the corpus finished. The permanent regression also passed directly. The Part 1469 full .NET run is still active and has recorded port failures including eacces. Canonical readiness and publication remain pending. The readiness comparison baseline stays at the immutable Part 561 snapshot.

- [Focused rerun](focused-retry.json), [first checked whole-module run](focused-whole.json), [direct regression](cons-result-managed.txt), and [compiler hashes](compiler-hash.txt)
