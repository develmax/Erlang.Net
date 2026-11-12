# Part 1369 — Construct call arguments in their final registers

After the first computed call argument returns, the emitter keeps its result in X0 while it builds the remaining pure operands. It lowers records to tuples, writes constructed operands into their final argument registers and reserves the combined heap space once. A temporary X register becomes available again as soon as its value is consumed by a tuple or list. The ordinary evaluator also writes the last cons of a computed list into its final argument register.

The focused oracle checks returned terms, call order and exceptions. The record and list arguments in construct/2, the nonzero call result destination in offset/2, and the nested tuples and list in nested/2 match OTP's decoded instructions. The dynamic list test checks that the last cons writes X1 directly.

The Windows OTP 29.1.1 corpus generated all 1,366 images with 369 Equal, 997 Different and zero SourceRejected. ELDAPv3 remains Equal. OTP accepted all 997 differing images for loading. The Code size gap narrowed in 815 images and widened in ex_sizers by 12 bytes. The net reduction was 50,598 bytes. The normalized comparison covers all 816 changed images and found six gained function matches with no lost matches. Thirteen functions use fewer stack slots and all thirteen reach the OTP frame size.

In ct_netconfc:connect/2 the frame falls from three to one slot and three heap checks become one test_heap for 14 words. In dialyzer_dataflow:format_cerl/1 the list is written directly into X1. Both match the release instructions after normalization.

Nine earlier probes, the two selected compiler tests and the direct whole-module test passed. The full test suite for this build and canonical readiness remain pending.

- [Full corpus](otp-corpus.json)
- [Code size changes](code-delta.json)
- [Normalized functions](normalized-functions.txt)
- [Opcode ranking](opcode-ranking.txt)
- [Peak frame comparison](peak-slots.txt)
- [OTP loader check](loader.txt)
- [Release instructions](release-functions.txt)
- [Compiler test runner](focused.json)
- [Whole-module test](whole-module.txt)
- [Fixture source](argument_construction_probe.erl)
- [Focused check](check-argument-construction.ps1)
- [Focused output](probe.txt)
- [Fixture instructions](instructions.txt)
