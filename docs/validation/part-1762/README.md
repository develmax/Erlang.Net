# Part 1762

Track one catch handler and one frame in the Y-slot allocator. Merge ordinary and exceptional entries through the label and line prefix of their common catch_end. Preserve the live Y values on both paths and clear the tag at catch_end.

The C# compiler accepted all 1,366 Windows OTP 29.1.1 sources and emitted 1,366 files. 398 images match byte for byte, including ELDAPv3 from the C# ASN.1 generator. The OTP loader accepts both files in all 968 differing pairs.

391 stack slot-count comparisons at corresponding frame allocations across 187 changed images, with 74 new matches and 0 lost matches. Sum of absolute slot-count gaps decreases by 1148 across comparable allocations. The sum of absolute Code size differences decreased by 3216 bytes. Normalized disassembly gained 0 function matches and lost 0.

398 changed comparable catch frames, 74 new matches and 0 lost matches for {frame size,tag register}. 0 gains had matching frame sizes before the change. OTP places the tag in the highest slot in 398 of 398 changed comparable catch frames.

Release compiler and test builds used SDK 10.0.404. All 11 targeted cases and seven BEAM harness cases passed. The regression fails with part 1761. The OTP VM agreed with the prior and current compiler on all seven outcomes, including throw, exit, error, a call after catch_end, retained values, stack entry identity and dictionary effects.

The compiler block passed 3721 of 3830 tests. The same 108 failed with part 1761. The original string Jaro test timed out during the broad run and passed in a separate run of the same binary. In the separate runtime selection, DETS passed and the port and dynamic apply cases reproduced preceding failures. The full runtime suite and readiness regeneration were outside this run.

~~~powershell
$env:ERLANG_BEAM_CORPUS_SAVE_DIFFERENCES = '1'
dotnet artifacts/beam-backend/part1762-catch-stack-slots-tool/Erlang.BeamCorpus.dll $otp artifacts/beam-backend/otp-corpus-part1762-catch-stack-slots-pinned.json artifacts/beam-backend/part929-ordered-headers artifacts/beam-backend/part1058-source-overlay
~~~

The next allocator group has one frame and one trim, with no handler, receive or debug instructions. 118 modules 171 functions {trim_shape,1,1,false,false,false}.

[Corpus](otp-corpus.json) · [Status changes](status-changes.txt) · [Loader and Code sizes](loader-and-code.txt) · [Frame differences](frame.txt) · [First differences](first.txt) · [Normalized instructions](normalized-functions.txt) · [Exception frames](exception-frames.txt) · [Catch handler entry shape](catch-targets.txt) · [Trim frame inventory](trim-frames.txt) · [Operand inventory](stack-operands-after.txt) · [Handler tag comparison](tag-delta.txt) · [Fixture](beam_catch_stack_slots.erl) · [Fixture frames](fixture-frames.txt) · [Fixture execution](behavior.txt) · [Focused tests](focused.txt) · [Previous compiler](negative.txt) · [Compiler block](compiler-block-summary.txt) · [Runtime checks](runtime-gates-pinned.txt) · [Isolated Jaro rerun](jaro-isolated.txt)
