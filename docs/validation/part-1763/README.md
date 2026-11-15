# Part 1763

Analyze both sides of a single trim in one frame. Express Y operands in the original frame coordinates for liveness analysis. Pack slots used after the trim at the high end, then renumber operands for the remaining frame and update the size at exits.

The C# compiler accepted all 1,366 Windows OTP 29.1.1 sources and emitted 1,366 files. 398 images match byte for byte, including ELDAPv3 from the C# ASN.1 generator. The OTP loader accepts both files in all 968 differing pairs.

27 stack slot-count comparisons at corresponding frame allocations across 69 changed images, with 3 new matches and 0 lost matches. Sum of absolute slot-count gaps decreases by 41 across comparable allocations. The sum of absolute Code size differences decreased by 667 bytes. Normalized disassembly gained 0 function matches and lost 0.

Release compiler and test builds used SDK 10.0.404. All 11 targeted cases and seven BEAM harness cases passed. The regression fails with part 1762. The OTP VM gave the same results for seven checks using the pinned, prior and current images, including values read after trim, a dictionary effect and a function_clause.

The compiler block passed 3722 of 3830 tests. The same 108 failed with part 1762. The full runtime suite and readiness regeneration were outside this run.

~~~powershell
$env:ERLANG_BEAM_CORPUS_SAVE_DIFFERENCES = '1'
dotnet artifacts/beam-backend/part1763-single-trim-slots-tool/Erlang.BeamCorpus.dll $otp artifacts/beam-backend/otp-corpus-part1763-single-trim-slots-pinned.json artifacts/beam-backend/part929-ordered-headers artifacts/beam-backend/part1058-source-overlay
~~~

The preceding corpus comparison found 4245 helper identities across 498 modules in the largest missing list-comprehension helper group. Among parents with exactly one list comprehension, the largest source group has a variable pattern, variable source, a call as output and no filters. It contains 570 source functions in 213 modules.

230 functions in 151 modules move an unwritten Y slot between allocate and initial trim. 100 such functions in 71 modules have no frame in OTP.

[Corpus](otp-corpus.json) · [Status changes](status-changes.txt) · [Loader and Code sizes](loader-and-code.txt) · [Frame differences](frame.txt) · [First differences](first.txt) · [Normalized instructions](normalized-functions.txt) · [Exception frames](exception-frames.txt) · [Trim frame inventory](trim-frames.txt) · [Operand inventory](stack-operands-after.txt) · [Frame changes across trim](trim-delta.txt) · [Initial trim relocations](initial-trim-moves.txt) · [Missing function identities](function-identities-before.txt) · [Source comprehension shapes](lc-source-shapes-before.txt) · [Example from proplists](proplists-lc.txt) · [Fixture](beam_single_trim_slots.erl) · [Fixture frames](fixture-frames.txt) · [Fixture execution](behavior.txt) · [Focused tests](focused.txt) · [Previous compiler](negative.txt) · [Compiler block](compiler-block-summary.txt)
