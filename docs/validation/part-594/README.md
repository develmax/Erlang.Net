# Part594 compact BEAM operands

Reference source is [OTP-29.1.1 beam_asm.erl](https://github.com/erlang/otp/blob/ad05823719d77c8faee87348ea39513d4e2f99c5/lib/compiler/src/beam_asm.erl). The oracle was the Windows OTP provider in the local Part376 admission receipt. Each row in [the test fixture](../../../tests/Erlang.Tests/Compiler/Beam/BeamCompactWriterChecks.cs) was captured with `binary:encode_hex(iolist_to_binary([beam_asm:encode(Tag, Value)]))` using the tag and integer from that row.

| Command | Result |
| --- | --- |
| `dotnet build tests/Erlang.Tests -m:1 --no-restore -v:q` | Passed with zero warnings and errors |
| `dotnet run --project tests/Erlang.Tests --no-build -- --case compiler/beam/compact-operands-match-otp --report artifacts/beam-compact-writer-report.json` | 1/1 tests passed, comparing all 22 captured vectors |

The [saved report](compact-operands.json) is a focused result. Module emission, packaged-image reproduction, whole-OTP corpus tests, full regression, and canonical readiness regeneration remain outstanding.
