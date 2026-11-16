# Part 1742

Reuse branch-local Y slots when the frame is allocated after entry guards that fail to func_info. Guard failure still occurs with the original arguments and before any stack frame is allocated.

All 1,366 Windows OTP 29.1.1 sources were admitted and compiled. 398 images matched byte for byte, including ELDAPv3 from the C# ASN.1 generator. 968 differed and passed the OTP loader check. The change affects 551 images and decreases the total absolute Code size gap by 17136 bytes. Normalized instructions gain 0 function matches and lose 0. No status changes.

1416 frame size changes, 123 new OTP matches, 2 lost matches, 5187 fewer slots of absolute difference.

The build and all six targeted tests passed. The [fixture](../../../tests/Erlang.Tests/Resources/Beam/beam_guarded_branch_stack_slots.erl) now allocates one slot after the guard, as OTP does. The pinned VM verifies all three branches with a boxed value and the arguments of function_clause on guard failure. The full runtime suite was stopped after 415 passes and 24 failures on .NET 10.0.15. The DETS runtime-admission failure reproduced on the preceding compiler and the selected DETS case passed on the pinned .NET 10.0.11 runtime. Port and dynamic apply failures reproduced on both compiler versions under .NET 10.0.11. Readiness regeneration requires a passing full runtime suite and was not run.

[Corpus](otp-corpus.json) · [Status changes](status-changes.txt) · [Loader and Code](loader-and-code.txt) · [Functions](normalized-functions.txt) · [Tests](focused.txt) · [Frame sizes](delta.txt) · [Probe](probe.txt) · [Baseline frame shapes](baseline-frame-shapes.txt) · [Projection candidates](projection-copies-suspects.txt) · [Runtime attempt](runtime-attempt.txt)
