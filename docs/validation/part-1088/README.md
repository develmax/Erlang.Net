# Part 1088 — order prepared Y operands and literals as OTP does

Ordinary calls now use the OTP move order for arguments that are saved variables or literals. With no X dependencies, they reuse the existing independent move order. Computed arguments retain their existing evaluation order. Guard and function-value call paths retain their existing scheduling.

The OTP 29.1.1 probe checks mixed saved operands and constants with two and four arguments. Three tail functions match after normalizing entry labels and source file paths. Another probe verifies left-to-right side effects when evaluating two computed arguments.

- [Probe source](first_call_probe.erl)
- [Execution assertions](run.eval)
- [Instruction comparison](instruction-comparison.txt)
- [Previous emission](before.txt)
- [New emission](after.txt)
- [OTP emission](otp.txt)

The full corpus generated 1,363 BEAM files. There were 348 Equal, 1015 Different and 3 SourceRejected. ELDAPv3 is Equal. Compared with part 1087, 979 differing images changed. One Code length gap narrowed and one widened. OTP prepared all 1015 differing images for loading.

The largest remaining frame mismatch includes the ASN.1 compiler closure shown below. OTP reuses the Y slot of an argument at its last use with a swap. The C# emitter allocates an additional slot for the preceding call result.

- [Full corpus](otp-corpus.json)
- [Corpus delta](delta.json)
- [OTP loader](loader.txt)
- [Focused tests](focused.json)
- [Frame reuse example](asn1ct-entry.txt)
- [Opcode ranking](opcode-ranking.txt)

The instruction comparison ignores line instructions and label declarations while retaining operands. Across the 979 changed files, 27 function prefixes got longer and none got shorter. Ten function instruction sequences became equal by this metric, and none lost equality.

- [Function instruction delta](function-prefix.txt)
- [Comparison script](function-prefix.eval)
