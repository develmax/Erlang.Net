# Part 1761

Track installed handlers and exceptional edges when allocating Y slots for a single try and one frame. Keep values live at the handler distinct from writes on protected paths. Model try_end and try_case as reading and clearing the tag. Allocate the tag group first so the final numbering places it highest.

The C# compiler accepted all 1,366 Windows OTP 29.1.1 sources and emitted 1,366 files. 398 images match byte for byte, including ELDAPv3 from the C# ASN.1 generator. The OTP loader accepts both files in all 968 differing pairs.

1468 stack slot-count comparisons at corresponding frame allocations across 449 changed images, with 294 new matches and 2 lost matches. Sum of absolute slot-count gaps decreases by 7982 across comparable allocations. The sum of absolute Code size differences decreased by 18447 bytes. Normalized disassembly gained 0 function matches and lost 0. The functions compile:encrypt_debug_info/3 and mnesia_monitor:patch_env/2 now use one slot fewer than OTP.

1541 changed comparable try frames, 313 new matches and 0 lost matches for {frame size,tag register}. 0 gains had matching frame sizes before the change. OTP places the tag in the highest slot in 1541 of 1541 changed comparable try frames.

Release compiler and test builds used SDK 10.0.404. All 11 targeted cases and seven BEAM harness cases passed. The regression fails with part 1760 and with a dedicated-slot variant. The OTP VM agreed on seven fixture outcomes, including failures before and after the handler is removed, retained values, exception reasons, stack entry identity and dictionary effects.

The compiler block passed 3722 of 3830 tests. The same 108 failed with part 1760. In the separate runtime selection, DETS passed and the port and dynamic apply cases reproduced preceding failures. The full runtime suite and readiness regeneration were outside this run.

~~~powershell
$env:ERLANG_BEAM_CORPUS_SAVE_DIFFERENCES = '1'
dotnet artifacts/beam-backend/part1761-try-tag-lifetime-tool/Erlang.BeamCorpus.dll $otp artifacts/beam-backend/otp-corpus-part1761-try-tag-lifetime-pinned.json artifacts/beam-backend/part929-ordered-headers artifacts/beam-backend/part1058-source-overlay
~~~

The next allocator group has a single catch and frame, with no trim, receive or debug instructions. 170 modules 386 functions {handlers,0,1,1,false,false,false}.

[Corpus](otp-corpus.json) · [Status changes](status-changes.txt) · [Loader and Code sizes](loader-and-code.txt) · [Frame differences](frame.txt) · [Two previously matching frames](lost-frames.txt) · [First differences](first.txt) · [Normalized instructions](normalized-functions.txt) · [Exception frames](exception-frames.txt) · [Catch handler entry shape](catch-targets.txt) · [Operand inventory](stack-operands-after.txt) · [Handler tag comparison](tag-delta.txt) · [Variant comparison](variants.txt) · [Fixture](beam_try_stack_slots.erl) · [Fixture frames](fixture-frames.txt) · [Fixture execution](behavior.txt) · [Focused tests](focused-final.txt) · [Direct BIF result](bif-behavior.txt) · [Previous compiler](negative.txt) · [Dedicated tag regression](negative-dedicated-tag.txt) · [Compiler block](compiler-block-summary.txt) · [Runtime checks](runtime-gates-pinned.txt)
