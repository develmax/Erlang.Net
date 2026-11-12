# Part 1458 — Fold atom equality guards into literal dispatch

The C# emitter now folds pure atom equality alternatives over a unary input into the existing literal table. This covers semicolon alternatives, boolean disjunction, literal clauses mixed with guards, and terminal if expressions with a true default. The first matching clause owns a key. A key that returns the unconditional default stays out of the table even when a later clause also names it.

For a terminal case, a guarded wildcard can use the outer input. The emitter keeps x0 for that guard and identity result, and uses the original case location for case_clause. Three fixtures compare normalized instructions and runtime results against OTP.

```powershell
dotnet build tools/Validation/Erlang.BeamCorpus -m:1 -o artifacts/beam-backend/part1458-tool-checked
$otp = Join-Path $env:TEMP 'erl-cohort-7mzi5e1n/view/artifacts/library376-official-compiler/installed'
$env:ERLANG_BEAM_CORPUS_SAVE_DIFFERENCES = '1'
dotnet artifacts/beam-backend/part1458-tool-checked/Erlang.BeamCorpus.dll $otp artifacts/beam-backend/otp-corpus-part1458-checked.json artifacts/beam-backend/part929-ordered-headers artifacts/beam-backend/part1058-source-overlay
```

The normalized release bodies match for SSL key_exchange_alg/1, TCP socket tag/1 and DNS encode_opcode/1. The instruction counts move from 91 to 29, 27 to 11 and 35 to 26 respectively. The generated DNS image round-trips standard and raw numeric opcodes in OTP.

The initial focused .NET run timed out on the whole-module case during concurrent work. The checked compiler and test build passed all three focused cases with the original harness deadline. The historical full .NET run for Part 1455 completed with 9,518 passed and 182 failed. Canonical readiness therefore remains pending.

- [Fixture instructions and execution](probe.txt), [focused .NET tests](focused.json), and [compiler hash](compiler-hash.txt)
- [SSL instructions](ssl-key-exchange.txt), [TCP instructions](socket-tag.txt), [DNS instructions](dns-opcode.txt), and [DNS execution](dns-opcode-execution.txt)
- [Historical full .NET result](prior-full-dotnet.txt)

The full corpus admitted all 1,366 sources and generated 1,366 images. The comparison records 383 Equal and 983 Different, with zero status transitions. ELDAPv3 remains Equal after C# ASN.1 generation. Both loaders accepted all differing images.

Twenty images changed. Twenty-four functions gained normalized instruction equality with zero losses, and 13 functions now have the OTP frame size. The Code chunk size gap decreased by 707 bytes in aggregate.

The refreshed scan finds 29 functions in 25 images whose entry atom table targets all begin with a tagged-tuple test. In httpc_manager:get_option/2 OTP selects among 15 keys before checking and reading the corresponding record field. C# tests the record first and then allocates a frame for linear key tests.

- [Full corpus](otp-corpus.json), [instruction changes](normalized-functions.txt), and [loaders and Code sizes](loader-and-code.txt)
- [Entry branch shapes](entry-branch-shapes.txt), [record projection instructions](record-option-dispatch.txt), and [remaining function differences](image-function-differences.txt)
