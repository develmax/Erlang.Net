# Part 1464 — Use already initialized stack slots for projection elimination

The projection pass now handles a temporary Y slot through direct calls if it can find an earlier initialization in the same frame and block. It searches back to the allocation and stops at branches, frame changes and instructions that can introduce handlers. From the copy it follows straight-line instructions to a frame exit or an overwrite after a direct call. An intervening read or unknown instruction stops the proof.

The recursive control fixture keeps its original init_yregs. The tuple extraction now writes x0 directly before call_last. OTP, previous C# and current C# agree on the tested recursive and repeated-argument cases. The focused BEAM suite also covers uninitialized stack slots and both ordinary and tail append calls.

- [Control source](beam_projection_moves.erl) and [instructions and results](probe.txt)

```powershell
dotnet build tools/Validation/Erlang.BeamCorpus -m:1 -o artifacts/beam-backend/part1464-tool
$otp = Join-Path $env:TEMP 'erl-cohort-7mzi5e1n/view/artifacts/library376-official-compiler/installed'
$env:ERLANG_BEAM_CORPUS_SAVE_DIFFERENCES = '1'
dotnet artifacts/beam-backend/part1464-tool/Erlang.BeamCorpus.dll $otp artifacts/beam-backend/otp-corpus-part1464.json artifacts/beam-backend/part929-ordered-headers artifacts/beam-backend/part1058-source-overlay
```

All 1,366 sources were admitted and generated BEAM. The full comparison records 383 Equal and 983 Different with zero status transitions. ELDAPv3 remains Equal after C# ASN.1 generation. OTP accepts the reference and generated images in all 983 differing pairs. All three focused tests passed.

The pass changes 394 images and 1,187 function bodies. No functions gain or lose complete normalized instruction equality. Frame sizes stay the same. The aggregate Code size gap falls by 3,618 bytes. Two wx demos, ex_sizers and ex_webview, each move three bytes farther from the OTP Code size because their generated Code was already shorter.

The direct projection ranking drops from 2,560 functions across 656 images to 2,268 across 606. In asn1ct:merge_pass/1, the extraction now goes straight to x0, as in OTP. The generated frame still has three slots and OTP uses one. The remaining Y-slot candidate group most often resumes with a move after a direct call, in 375 images. Branch analysis remains open, including 222 images with an unconditional jump after the projection copy and before a direct call or another continuation.

- [Full corpus](otp-corpus.json), [status transitions](status-changes.txt), and [OTP loading](loader-and-code.txt)
- [Instruction comparison](normalized-functions.txt), [frame and Code totals](summary.txt), [projection ranking](projection-ranking.txt), and [ASN.1 sample](asn1ct-projections.txt)
- [Y-slot continuations before](y-continuations-before.txt) and [after](y-continuations-after.txt), [scanner](rank-y-projections.escript), [focused checks](focused.json), and [compiler hash](compiler-hash.txt)

The full .NET suite and canonical readiness validation remain pending.
