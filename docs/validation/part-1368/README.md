# Part 1368 — Keep first-call inputs in entry registers

The general emitter handles a terminal call whose first computed operand is an ordinary call on entry inputs. It calculates which inputs the remaining operands reference, saves those across the inner call and passes first-call-only inputs directly from the incoming registers. The existing argument emitter evaluates later operands in source order and the call planner emits the terminal call.

Four focused functions save one stack slot each. The fixture checks local and external calls, an earlier literal, a value referenced on both sides of an inner call, and exceptions that stop argument evaluation. Eight earlier probes, the two selected compiler runner cases and a direct invocation of the whole-module test passed.

The Windows OTP 29.1.1 corpus generated all 1,366 BEAM images with 369 Equal, 997 Different and zero SourceRejected. ELDAPv3 remains Equal. OTP accepted all 997 differing images for loading. Eight images changed and their Code size gaps narrowed by 76 bytes in total. Ten functions reduced peak stack use by one. dialyzer_dataflow:format_cerl/1 reaches the reference's one-slot frame and emits the external tail call. Its list is still constructed in X0 and moved to X1. The normalized comparison found no gained or lost matches across the eight changed images.

The source audit finds 33 differing modules with at least one input used only by the first computed call operand and 23 with only tuple, list and simple operands after that call. In ct_netconfc:connect/2 the frame drops from four to three slots. OTP uses one slot, preserves the first result in X0 and constructs the remaining arguments in their final registers under one heap test.

The complete test suite for the previous compiler build was still running when this checkpoint was verified. The present build has focused test evidence. Canonical readiness and publication remain pending.

- [Full corpus](otp-corpus.json)
- [Code size changes](code-delta.json)
- [Peak frame comparison](peak-slots.txt)
- [Normalized comparison](normalized-functions.txt)
- [OTP loader check](loader.txt)
- [Focused source](entry_operand_probe.erl)
- [Focused check](check-entry-operands.ps1)
- [Focused frame results](probe.txt)
- [Compiler test runner](focused.json)
- [Whole-module test](whole-module.txt)
- [ct_netconfc disassembly](ct-netconfc.txt)
- [dialyzer_dataflow disassembly](dialyzer-format-cerl.txt)
- [Source audit](entry-operand-audit.txt)
- [Source audit script](rank-entry-operands.eval)
