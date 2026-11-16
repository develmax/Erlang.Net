# Part 1469 — Match list and tuple entry patterns before allocating the frame

The general emitter shares the first cons test and extraction across leading clauses that match a nonempty list. It can move an initial empty-list group after the nonempty clauses because their inputs are disjoint. It keeps original arguments intact across failure edges and keeps the order within each group.

The register matcher handles nested tuples and lists, literals, aliases with a single name per register, and equality for repeated bindings. Entry guards use the existing register guard emitter. Once a clause succeeds, the existing body lowering chooses a frame and saves only the inputs the body uses. Direct tail calls and immediate results use the existing register paths. Patterns and guards outside this scope use the existing fallback.

The control compiles two functions with nested patterns. OTP accepts the generated image. The reference and generated images return the same values for a successful guard, repeated-variable failure, guard failure, empty list and catch-all inputs. They report the same original argument when the strict match fails.

- [Managed regression](../../../tests/Erlang.Tests/Compiler/Beam/BeamModuleEmissionChecks.cs) and [OTP probe](probe.txt)
- [Release instruction prefixes](release-prefixes.txt) and [previous prefixes](../part-1468/list-entry-examples.txt)

```powershell
dotnet build tools/Validation/Erlang.BeamCorpus -m:1 -o artifacts/beam-backend/part1469-tool
$otp = Join-Path $env:TEMP 'erl-cohort-7mzi5e1n/view/artifacts/library376-official-compiler/installed'
$env:ERLANG_BEAM_CORPUS_SAVE_DIFFERENCES = '1'
dotnet artifacts/beam-backend/part1469-tool/Erlang.BeamCorpus.dll $otp artifacts/beam-backend/otp-corpus-part1469.json artifacts/beam-backend/part929-ordered-headers artifacts/beam-backend/part1058-source-overlay
```

The complete Windows OTP 29.1.1 corpus generates all 1,366 images and records 383 Equal, 983 Different, zero rejected sources and zero status transitions. ELDAPv3 remains Equal after C# ASN.1 generation. The OTP loader accepts both images of all 983 differing pairs. Three focused .NET tests passed using the compiler binary from the corpus run.

The rule changes 362 images. The first list test reaches OTP in 709 functions across 276 modules. Frame size moves closer to OTP in 1,119 functions and reaches the reference size in 143 of those. The aggregate Code size gap falls by 10,242 bytes. The frame group with a list test before allocation drops from 530 to 461 modules.

One body gains normalized instruction equality. Six lose it. Four direct bodies keep the original list and tuple registers longer than OTP. One has different failure labels and one has different closure argument ordering. Five of six representative release functions move the tests ahead of allocation. The representative with a bitstring pattern in a clause head still uses the existing fallback.

- [Full corpus](otp-corpus.json), [status changes](status-changes.txt), and [OTP loading](loader-and-code.txt)
- [Entry list tests](entry-delta.txt), [frame ranking](frame-ranking.txt), and [frame and Code totals](summary.txt)
- [Normalized instructions](normalized-functions.txt) and [six function differences](function-differences.txt)
- [Focused tests](focused.json) and [compiler hashes](compiler-hash.txt)

The full .NET suite was started on the same candidate. Canonical readiness needs its complete passing report. The comparison baseline remains the immutable Part 561 test snapshot. Publication remains pending.
