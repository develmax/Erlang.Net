# Part 1370 — Reuse a stack slot across wrapped call operands

The sibling call scheduler now plans nested calls inside tuple and list arguments. It preserves their evaluation order and uses the existing last-use slot logic to save each earlier result. The existing construction planner then builds the remaining operands from the saved results and the last result in X0 under one heap check.

The focused probe checks the lifetime of a saved input, an input still needed by the terminal call, and three calls nested across a tuple and a list. It checks returned terms, exception short-circuiting and call order. The reuse and retain fixtures match OTP's decoded instructions. All three use the OTP frame size. Nine earlier probes and the selected compiler checks passed.

The Windows OTP 29.1.1 corpus generated all 1,366 images with 369 Equal, 997 Different and zero SourceRejected. ELDAPv3 remains Equal. OTP accepted all 997 differing images for loading. Eleven images changed and their Code size gaps narrowed by 248 bytes in total. Seven functions gained normalized matches and twelve reached OTP's frame size after reducing stack use.

The frame audit finds a first exact-equality test versus allocation mismatch in 3,046 multi-clause functions across 622 modules. A nonempty-list test versus allocation occurs in 3,976 multi-clause functions across 614 modules. The groups can overlap by module. In AlgorithmInformation-2009:decode_disp/2, OTP dispatches in registers and tail-calls the decoder or exit/1 without allocating a stack frame. The generated function allocates two slots before its first test.

The full runtime suite has not run on this build. The prior part 1367 suite finished with 9,517 passes and 183 failures. It cannot support current readiness regeneration.

- [Full corpus](otp-corpus.json)
- [Code size changes](code-delta.json)
- [Normalized function comparison](normalized-functions.txt)
- [Peak frame comparison](peak-slots.txt)
- [OTP loader check](loader.txt)
- [Frame audit](frame-shapes.txt)
- [Frame audit script](rank-frame-shapes.escript)
- [Clause entry instructions](clause-entry.txt)
- [Fixture source](wrapped_operands_probe.erl)
- [Focused check](check-wrapped-operands.ps1)
- [Focused output](probe.txt)
- [Compiler test runner](focused.json)
- [Whole-module test](whole-module.txt)
