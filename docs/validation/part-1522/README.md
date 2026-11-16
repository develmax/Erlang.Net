# Part 1522 — Lower tuple case alternatives and exact key checks in registers

For pattern equality, a terminal tuple case retains the previously bound variables that participate in exact comparisons. The tuple and its projected fields remain in X registers. The emitter can return the unchanged selector in a wildcard arm or return an entire tuple bound by an alias. A disjoint atom arm can return a constant or issue a tail call. Tagged alternatives with the same arity can select different payload positions and extract those fields inside their respective arms. A failure can still report the original selector.

The compiler reuses the existing selector call-chain planner, tuple tests, and framed tail emission. The pinned OTP 29.1.1 reference is ad05823719d77c8faee87348ea39513d4e2f99c5. No dependency was added. [Compiler implementation](../../../src/Compiler/Erlang.Compiler/Compilation/BeamModuleCompiler.CallTupleCaseProjection.cs).

The deterministic fixture matches OTP with SHA-256 86D33B6BCED4CDC61525DA8E6BFA6512185DA3FAE4884A79D35BEA26F0145F49. The current image matches all 83 OTP observations, including exact numeric key mismatches and the original selector on case_clause. The previous image matches the same returns and exception reasons, with five stack entries reporting the alternate arm line where OTP reports the case line. [Exact fixture](../../../tests/Erlang.Tests/Resources/Beam/beam_tuple_case_fallbacks.erl) · [OTP fixture](../../../tests/Erlang.Tests/Resources/Beam/beam_tuple_case_fallbacks.beam) · [Oracle results](fixture.txt).

## Commands

```powershell
dotnet build tools/Validation/Erlang.BeamCorpus -m:1 -o artifacts/beam-backend/part1522-tool
dotnet build tests/Erlang.Tests -m:1 -o artifacts/beam-backend/part1522-tests
$otp = Join-Path $env:TEMP 'erl-cohort-7mzi5e1n/view/artifacts/library376-official-compiler/installed'
$env:ERLANG_BEAM_CORPUS_SAVE_DIFFERENCES = '1'
dotnet artifacts/beam-backend/part1522-tool/Erlang.BeamCorpus.dll $otp artifacts/beam-backend/otp-corpus-part1522-final.json artifacts/beam-backend/part929-ordered-headers artifacts/beam-backend/part1058-source-overlay
$tests = 'artifacts/beam-backend/part1522-tests/Erlang.Tests.dll'
dotnet $tests --case compiler/beam/atom-case-emits-local-tails --case compiler/beam/documentation-payload-matches-otp --case compiler/beam/rebuild-installed-otp-release-images --case compiler/beam/whole-module-matches-otp --case compiler/beam/call-result-bif-keeps-register --case compiler/beam/call-result-case-keeps-register --report artifacts/beam-backend/part1522-focused.json
dotnet $tests --block compiler --report artifacts/beam-backend/part1522-compiler.json
```

## Complete corpus

| Check | Result |
| --- | --- |
| Source admission and emission | 1,366 of 1,366. Zero source rejections and zero emission failures |
| Full image equality | 387 Equal and 979 Different. tftp_logger and ELDAPv3 remain Equal |
| Changed outputs | 28 images and 30 normalized function bodies |
| Instruction and frame equality | 14 functions reach normalized instruction equality. In 25 functions the maximum frame reaches OTP size, and one more moves closer |
| Code size | The aggregate gap to OTP falls by 285 bytes |
| OTP loader | Both images load in all 979 differing pairs |
| .NET checks | Six focused checks pass. The compiler block has the same 3,722 passes and 108 failures in 3,830 tests |

[Corpus](otp-corpus.json) · [Status transitions](status-changes.txt) · [Function delta](normalized-functions.txt) · [Frame delta](frame-delta.txt) · [Loader and Code](loader-and-code.txt) · [Focused checks](focused.json) · [Compiler block](compiler-test-status.txt).

The release comparison aligns random:seed/0, misc_supp:passq/2, and snmp_mini_mib:type/2. The first returns a whole tuple or calls a fallback. The second preserves an exact equality with the previously bound key. The third returns the original selector when the tuple does not match. [random](random-seed.txt) · [misc_supp](misc_supp-passq.txt) · [snmp_mini_mib](snmp_mini_mib-type.txt).

The selector audit now finds 1,093 functions with the source shape and a larger first allocation. The group with a simple selector and an unguarded tuple arm that binds another call result contains 102 functions in 80 images. The nearest candidate is xmerl_sax_simple_dom:find_and_remove_attribute/3. Its OTP code retains the needed field across keydelete/3 and trims the expired input slots on that branch. [Before](tuple-case-selector-before.txt) · [After](tuple-case-selector-after.txt) · [Scan](tuple-case-selector-shapes.escript) · [Nearest images](nearest.txt).

The full .NET suite and canonical readiness were not rerun.
