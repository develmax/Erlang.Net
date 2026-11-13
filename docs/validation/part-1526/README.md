# Part 1526 — Omit proven atom tests in tuple case alternatives

After a failed tuple test, the compiler can omit the exact comparison when a resolved call has exactly one possible non-tuple result. The shared result contract covers lists:keyfind/3, maps:find/2 and maps:take/2. The projection emitter still tests other atom alternatives. This uses the pinned OTP 29.1.1 specs and release instructions, with no new dependency.

The deterministic fixture exercises both the eliminated comparison and a comparison that must remain. OTP and C# produce identical bytes and agree on 80 values, errors and fixture stack observations. [Fixture](../../../tests/Erlang.Tests/Resources/Beam/beam_tuple_case_known_atom.erl) · [Hashes](sha256.txt) · [Oracle](fixture.txt).

## Commands

```powershell
dotnet build tools/Validation/Erlang.BeamCorpus -m:1 -o artifacts/beam-backend/part1526-tool
dotnet build tests/Erlang.Tests -m:1 -o artifacts/beam-backend/part1526-tests
$otp = Join-Path $env:TEMP 'erl-cohort-7mzi5e1n/view/artifacts/library376-official-compiler/installed'
$env:ERLANG_BEAM_CORPUS_SAVE_DIFFERENCES = '1'
dotnet artifacts/beam-backend/part1526-tool/Erlang.BeamCorpus.dll $otp artifacts/beam-backend/otp-corpus-part1526-final.json artifacts/beam-backend/part929-ordered-headers artifacts/beam-backend/part1058-source-overlay
```

## Interim results

The full corpus comparison is running. The first four changed images already show normalized instruction equality for asn1ct_imm:get_constraint/2, dbg_icmd:get_binding/2 and dbg_wx_trace_win:is_button/1.
