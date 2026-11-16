# Part 1760

Reuse Y slots across binary extraction, match-position operations, compound matches and floating-point conversion. For compound matches, protect failure-path values against partial writes by earlier commands.

The C# compiler accepted all 1,366 Windows OTP 29.1.1 sources and emitted 1,366 files. The same 398 images match byte for byte, including ELDAPv3 from the C# ASN.1 generator. The OTP loader accepts both files in all 968 differing pairs.

544 stack slot-count comparisons at corresponding frame allocations across 177 changed images, with 10 new matches and 0 lost matches. Sum of absolute slot-count gaps decreases by 9415 across comparable allocations. The sum of absolute Code size differences decreased by 60979 bytes. Normalized disassembly gained 0 function matches and lost 0.

Release compiler and test builds used SDK 10.0.404. All 11 targeted cases and seven BEAM harness cases passed. The regression fails with part 1759. The OTP VM agreed on eight successful and failing scenarios and their dictionary effects. The fixed-pattern allocation shrank from five slots to four and matches OTP. The dynamic binary and signed integer patterns each saved two slots.

The compiler block passed 3722 of 3830 tests. The same 108 failed with part 1759. In the separate runtime selection, DETS passed and the port and dynamic apply cases reproduced preceding failures. The full runtime suite and readiness regeneration were outside this run.

~~~powershell
$env:ERLANG_BEAM_CORPUS_SAVE_DIFFERENCES = '1'
dotnet artifacts/beam-backend/part1760-bit-match-slots-tool/Erlang.BeamCorpus.dll $otp artifacts/beam-backend/otp-corpus-part1760-bit-match-slots-pinned.json artifacts/beam-backend/part929-ordered-headers artifacts/beam-backend/part1058-source-overlay
~~~

The next allocator group has a single try and frame, with no trim, receive or debug instructions. 373 modules 1151 functions {handlers,1,0,1,false,false,false}.

[Corpus](otp-corpus.json) · [Status changes](status-changes.txt) · [Loader and Code sizes](loader-and-code.txt) · [Frame differences](frame.txt) · [First differences](first.txt) · [Normalized instructions](normalized-functions.txt) · [Exception frames](exception-frames.txt) · [Operand inventory](stack-operands-after.txt) · [Fixture](beam_bit_match_slots.erl) · [Fixture frames](fixture-frames.txt) · [Fixture execution](behavior.txt) · [Focused tests](focused.txt) · [Previous compiler](negative.txt) · [Compiler block](compiler-block-summary.txt) · [Runtime checks](runtime-gates-pinned.txt)
