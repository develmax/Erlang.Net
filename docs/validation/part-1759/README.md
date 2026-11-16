# Part 1759

Reuse Y slots through map updates, map matches and binary construction or tail extraction. Preserve all source operands and prevent map-match destinations from taking a slot that is live along the failure edge. OTP validates that a failed match can partially write its destinations.

The C# compiler admitted all 1,366 Windows OTP 29.1.1 sources and emitted all 1,366 BEAM files. 398 Equal including ELDAPv3 from the C# ASN.1 generator and 968 Different. OTP's loader accepted both files in every differing pair. No status changes.

813 stack slot-count comparisons at corresponding frame allocations across 268 changed images, with 92 new matches and 1 lost matches. Sum of absolute slot-count gaps decreases by 3489 across comparable allocations. The sum of absolute Code size differences decreases by 14903 bytes. Normalized disassembly gains 0 matching functions and loses 0.

The fresh compiler and test builds used SDK 10.0.404. All 11 focused tests and all seven BEAM harness cases passed. The added instruction regression fails with part 1758. The pinned OTP VM agreed on all eight fixture results, dictionary effects, and badmap, badkey and badarg errors. The map lookup frame matches OTP at four slots with the same live X count. In erl_boot_server:start_args/2 the new allocator uses two slots where the release and part 1758 used three.

The compiler block passed 3722 of 3830 cases. The 108 failed cases also failed with part 1758. On .NET 10.0.11 DETS passes. The port and dynamic apply checks reproduce the preceding failures. The full runtime suite was outside this run and readiness evidence was not regenerated.

~~~powershell
$env:ERLANG_BEAM_CORPUS_SAVE_DIFFERENCES = '1'
dotnet artifacts/beam-backend/part1759-map-binary-slots-tool/Erlang.BeamCorpus.dll $otp artifacts/beam-backend/otp-corpus-part1759-map-binary-slots-pinned.json artifacts/beam-backend/part929-ordered-headers artifacts/beam-backend/part1058-source-overlay
~~~

The opcode inventory uses the part 1758 whitelist in both snapshots so the rows remain comparable. The next candidates include 41 modules 52 functions {unsupported_y,bs_match}, 34 modules 48 functions {unsupported_y,bs_get_binary2}, 23 modules 30 functions {unsupported_y,fconv}.

[Corpus](otp-corpus.json) · [Status changes](status-changes.txt) · [Loader and Code sizes](loader-and-code.txt) · [Normalized disassembly](normalized-functions.txt) · [Frame matches](frame.txt) · [Frame divergence in erl_boot_server](erl-boot-server-frames.txt) · [First differences](first.txt) · [Oversized frame structure](frame-structure.txt) · [Operand inventory before](stack-operands-before.txt) · [Operand inventory after](stack-operands-after.txt) · [Fixture](beam_map_binary_slots.erl) · [Fixture frame sizes](fixture-frames.txt) · [Fixture execution](behavior.txt) · [Focused tests](focused.txt) · [Compiler block](compiler-block-summary.txt) · [Previous compiler](negative.txt) · [Runtime checks](runtime-gates-pinned.txt)
