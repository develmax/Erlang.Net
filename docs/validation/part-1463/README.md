# Part 1463 — Emit tuple projections into their final registers

The C# compiler inspects the assembled BEAM instructions after resolving labels. It can replace a tuple projection and a later copy with a single projection into the copy destination. The instructions in between must leave the destination untouched. The temporary must have no further use through a supported call or return. The pass preserves the remaining bytes and reuses the managed BEAM instruction reader. Modules whose operand encoding the reader cannot decode retain their emitted instruction bytes.

The control fixture removes the temporary X-register copy before a recursive call. It keeps the copy when two arguments use the projected value. OTP accepts the reference, prior and current images. All three agree on the tested results and the process dictionary effect.

The first experiment also rewrote sequences whose temporary was overwritten during argument setup. That changed two byte-exact fixtures. OTP retains the temporary before apply_last in the dynamic case fixture and xmerl_xpath_lib. Requiring the supported call or return boundary restores both images byte for byte. A further test of the managed runtime exposed a missing frame initialization before call_ext_last. The pinned OTP validator also requires initialized Y slots at calls. The final rule keeps Y writes through calls and can eliminate a Y temporary before explicit deallocation followed by return.

- [Fixture](beam_projection_moves.erl), [instructions and execution](probe.txt)
- [Initial focused failures](initial-focused.json), [dynamic-case instructions](initial-probe-regression.txt), [xmerl instructions](initial-xmerl-regression.txt), and [checked xmerl result](xmerl-regression-checked.json)

```powershell
dotnet build tools/Validation/Erlang.BeamCorpus -m:1 -o artifacts/beam-backend/part1463-tool-final
$otp = Join-Path $env:TEMP 'erl-cohort-7mzi5e1n/view/artifacts/library376-official-compiler/installed'
$env:ERLANG_BEAM_CORPUS_SAVE_DIFFERENCES = '1'
dotnet artifacts/beam-backend/part1463-tool-final/Erlang.BeamCorpus.dll $otp artifacts/beam-backend/otp-corpus-part1463-final.json artifacts/beam-backend/part929-ordered-headers artifacts/beam-backend/part1058-source-overlay
```


All 1,366 sources were admitted and generated BEAM. The full Windows OTP 29.1.1 comparison records 383 Equal and 983 Different. ELDAPv3 stays Equal after C# ASN.1 generation. OTP accepts both the reference and generated image for each of the 983 differing pairs.

The rule changes 221 images and brings 42 functions to normalized instruction equality without losing prior function matches. Frame sizes stay the same in all 428 changed functions. The aggregate Code size gap falls by 1,749 bytes. The projection ranking drops from 2,909 functions across 676 images to 2,560 across 656. The largest remaining continuation group is a subsequent move in 401 images.

Three focused .NET tests passed. The corpus and focused builds used the same compiler DLL. Only the placement and wording of a source comment changed afterward. The full .NET suite and canonical readiness validation remain pending.

- [Full corpus](otp-corpus.json), [status transitions](status-changes.txt), and [OTP loading results](loader-and-code.txt)
- [Instruction comparison](normalized-functions.txt), [frame and Code summary](summary.txt), and [projection ranking](projection-ranking.txt)
- [Remaining continuations](projection-next-op.txt), [intermediate frame regression](checked-focused.json), [final focused checks](focused.json), and [compiler hashes](compiler-hash.txt)
