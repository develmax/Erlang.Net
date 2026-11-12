# Part 1465 — Check projection slot lifetime across branches

The projection pass builds instruction successors and predecessors from emitted BEAM labels. The backward walk requires initialization on all incoming paths to the projection. The forward walk follows direct calls, tests, selections and jumps. It preserves the copy if any path reads the temporary before overwriting it or discarding its frame. Frame trimming adjusts the tracked Y index. Unknown instructions and handler operations stop the proof.

In asn1ct:run_passes_1/2, two projections now write directly to x1, the register used by OTP. The negative control retains the projected value across a call because one branch uses it. OTP and both C# builds return the same results for both outcomes. The managed BEAM test executes the same live-slot cases.

- [Control source](beam_projection_branches.erl), [instructions and results](probe.txt), and [ASN.1 projection comparison](asn1ct-projections.txt)

```powershell
dotnet build tools/Validation/Erlang.BeamCorpus -m:1 -o artifacts/beam-backend/part1465-tool
$otp = Join-Path $env:TEMP 'erl-cohort-7mzi5e1n/view/artifacts/library376-official-compiler/installed'
$env:ERLANG_BEAM_CORPUS_SAVE_DIFFERENCES = '1'
dotnet artifacts/beam-backend/part1465-tool/Erlang.BeamCorpus.dll $otp artifacts/beam-backend/otp-corpus-part1465.json artifacts/beam-backend/part929-ordered-headers artifacts/beam-backend/part1058-source-overlay
```

All 1,366 sources were admitted and generated BEAM. The full comparison records 383 Equal and 983 Different, with zero status transitions. ELDAPv3 remains Equal after C# ASN.1 generation. OTP accepts both images of each differing pair. Three focused .NET cases passed against the same compiler binary as the corpus.

The pass changes 471 images and 1,254 function bodies. No functions gain or lose complete normalized instruction equality and frame sizes stay the same. The aggregate Code size gap falls by 6,499 bytes. beam_utils moves 18 bytes farther from the OTP Code size because its generated Code was already shorter.

The direct projection ranking drops from 2,268 functions across 606 images to 1,805 across 556. A move following a direct call remains the largest Y-slot continuation group at 319 images. A fresh frame scan finds 530 images where OTP tests for a nonempty list before frame allocation, and 527 where OTP tests for a tuple. These groups can overlap.

- [Full corpus](otp-corpus.json), [status changes](status-changes.txt), and [OTP loading](loader-and-code.txt)
- [Instruction delta](normalized-functions.txt), [frame and Code totals](summary.txt), [projection counts](projection-ranking.txt), and [remaining frame shapes](frame-ranking.txt)
- [Y-slot continuations](y-continuations.txt), [focused tests](focused.json), and [compiler hash](compiler-hash.txt)

The full .NET suite and canonical readiness validation remain pending.
