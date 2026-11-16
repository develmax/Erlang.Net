# Part 1524 — Reuse registers and stack slots across tuple case call chains

When a terminal tuple arm executes a straight-line call chain, the compiler tracks the last use of saved inputs across calls. A projected field needed only as the next call's first argument can go directly into X0. A field needed across calls can reuse a dead input slot after the pattern checks. The atom alternative continues to use its own original inputs.

The planner reuses tuple projection and the existing straight-line register case planner. No dependency was added. The reference is OTP 29.1.1 at ad05823719d77c8faee87348ea39513d4e2f99c5. [Compiler implementation](../../../src/Compiler/Erlang.Compiler/Compilation/BeamModuleCompiler.TupleCaseCallContinuation.cs).

Both deterministic fixtures match OTP byte for byte. The first exercises a retained payload, exact numeric key checks, failed matches, and two calls. The second forwards the projected field into X0 and exercises the atom alternative. OTP comparison covers 89 observations. [Retained payload](../../../tests/Erlang.Tests/Resources/Beam/beam_tuple_case_call_chain.erl) · [Forwarded field](../../../tests/Erlang.Tests/Resources/Beam/beam_tuple_case_forward_chain.erl) · [Hashes](sha256.txt) · [Oracle results](fixture.txt) and [forwarding oracle](forward-fixture.txt).

## Commands

```powershell
dotnet build tools/Validation/Erlang.BeamCorpus -m:1 -o artifacts/beam-backend/part1524-forward-tool
dotnet build tests/Erlang.Tests -m:1 -o artifacts/beam-backend/part1524-tests
$otp = Join-Path $env:TEMP 'erl-cohort-7mzi5e1n/view/artifacts/library376-official-compiler/installed'
$env:ERLANG_BEAM_CORPUS_SAVE_DIFFERENCES = '1'
dotnet artifacts/beam-backend/part1524-forward-tool/Erlang.BeamCorpus.dll $otp artifacts/beam-backend/otp-corpus-part1524-forward.json artifacts/beam-backend/part929-ordered-headers artifacts/beam-backend/part1058-source-overlay
$tests = 'artifacts/beam-backend/part1524-tests/Erlang.Tests.dll'
dotnet $tests --case compiler/beam/whole-module-matches-otp --case compiler/beam/rebuild-installed-otp-release-images --case compiler/beam/call-result-case-keeps-register --report artifacts/beam-backend/part1524-forward-focused.json
dotnet $tests --block compiler --report artifacts/beam-backend/part1524-forward-compiler.json
```

## Results

All three focused checks pass. The release function erl_compile_server:get_path_arg/2 now matches normalized OTP instructions. Its entry uses two Y slots and projects the field into X0. The prior emission put the atom test first. [Focused tests](focused.json) · [Release instructions](erl_compile_server.txt).

All 1,366 release sources were accepted and generated. The complete corpus has 387 Equal and 979 Different, with no status transitions. ELDAPv3 still matches from the C# ASN.1 generator. One release image changed, erl_compile_server. Its Code chunk grew by six bytes even though get_path_arg/2 matches normalized instructions. OTP loads all 979 differing images. [Full corpus](otp-corpus.json) · [Normalized delta](normalized-functions.txt) · [Loader and Code](loader-and-code.txt).

## Remaining frame differences

The complete corpus contains 11,120 functions across 882 modules whose first normalized difference is a larger C# `allocate` with the same live input count. Among functions with unguarded variable-only heads, 101 functions in 79 modules have terminal remote-call cases with atom and tuple arms and nonzero OTP frames. Across heads and frame sizes, the arm audit finds 138 functions in 104 modules. These figures count a first instruction difference and do not imply that correcting it makes an entire file equal.

In the pinned reference, `lib/compiler/src/beam_ssa_pre_codegen.erl` places frames over SSA blocks, finds values live across clobbers, and reserves Y registers before instruction emission. Its final `beam_trim` pass uses instruction liveness to shorten a frame. The general C# emitter currently increments `nextSlot` as it assigns temporaries and computes the frame from the peak. A shared liveness plan for terminal case blocks is the next target. Use the atom and tuple group as the first corpus slice, including atom branches containing effects and tail calls. The first tuple-construction example is `inets:key1search/2`, where OTP constructs the result from X0 and keeps only the exact-match key in Y. [Instruction comparison](inets-key1search.txt). [First differences](first-difference-ranking.txt) · [Source shapes](frame-shapes-baseline.txt) · [Arm shapes](atom-tuple-arms-baseline.txt) · [Ranking script](case-frame-ranking.escript) · [Arm ranking script](atom-tuple-arm-ranking.escript).
